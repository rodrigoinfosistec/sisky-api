using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.DTOs;
using SiskyApi.Models;

namespace SiskyApi.Services;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly TenantContext _tenantContext;
    private readonly AuditService _auditService;

    public UserService(AppDbContext context, TenantContext tenantContext, AuditService auditService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditService = auditService;
    }

    public async Task<PaginatedResponseDto<UserResponseDto>> GetAll(int page, int perPage, string sortBy = "name", string sortDir = "asc", string? search = null)
    {
        var query = _context.Users.AsQueryable();

        if (_tenantContext.HasTenant)
        {
            query = query.Where(u =>
                _context.UserCompanies
                    .Include(uc => uc.Company)
                    .Any(uc => uc.UserId == u.Id && uc.Company.TenantId == _tenantContext.TenantId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Name.ToLower().Contains(search.ToLower()) ||
                                     u.Email.ToLower().Contains(search.ToLower()));
        }

        query = sortBy switch
        {
            "name" => sortDir == "desc" ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
            "email" => sortDir == "desc" ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "createdAt" => sortDir == "desc" ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => sortDir == "desc" ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id)
        };

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var users = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(user => new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<UserResponseDto>
        {
            Data = users,
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        };
    }

    public async Task<UserResponseDto?> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<object?> GetUserDetails(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        var companies = await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId)
            .Select(uc => new
            {
                uc.Company.Id,
                uc.Company.Name,
                uc.Company.PrimaryColor,
                uc.IsDefault
            })
            .ToListAsync();

        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => new
            {
                ur.CompanyId,
                ur.Role.Id,
                ur.Role.Name,
                ur.Role.IsSystem
            })
            .ToListAsync();

        var tenantRoles = await _context.Roles
            .Where(r => r.TenantId == _tenantContext.TenantId)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.IsSystem
            })
            .ToListAsync();

        var tenantCompanies = await _context.Companies
            .Where(c => c.TenantId == _tenantContext.TenantId && c.Active)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.PrimaryColor
            })
            .ToListAsync();

        return new
        {
            user.Id,
            user.Name,
            user.Email,
            user.AvatarUrl,
            user.Active,
            user.CreatedAt,
            Companies = companies,
            UserRoles = userRoles,
            TenantRoles = tenantRoles,
            TenantCompanies = tenantCompanies
        };
    }

    public async Task<UserResponseDto> Create(UserCreateDto dto)
    {
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            TenantId = _tenantContext.TenantId,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (_tenantContext.CompanyId.HasValue)
        {
            _context.UserCompanies.Add(new UserCompany
            {
                UserId = user.Id,
                CompanyId = _tenantContext.CompanyId.Value,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        await _auditService.LogAsync(
            AuditActions.Created,
            "User",
            user.Id,
            newValues: new { user.Name, user.Email }
        );

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponseDto?> Update(int id, UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        var oldValues = new { user.Name, user.Email };

        user.Name = dto.Name;
        user.Email = dto.Email;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            AuditActions.Updated,
            "User",
            user.Id,
            oldValues: oldValues,
            newValues: new { user.Name, user.Email }
        );

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> ChangePassword(int id, UserChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return false;
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password)) return false;
        if (dto.NewPassword != dto.NewPasswordConfirmation) return false;

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            AuditActions.PasswordChanged,
            "User",
            user.Id
        );

        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return false;

        await _auditService.LogAsync(
            AuditActions.Deleted,
            "User",
            user.Id,
            oldValues: new { user.Name, user.Email }
        );

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string?> UpdateAvatar(int id, Stream fileStream, string fileName, string contentType, StorageService storageService)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        if (!string.IsNullOrEmpty(user.AvatarUrl))
            await storageService.DeleteAsync(user.AvatarUrl);

        var url = await storageService.UploadAsync(fileStream, fileName, contentType);
        user.AvatarUrl = url;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            AuditActions.AvatarUpdated,
            "User",
            user.Id,
            newValues: new { AvatarUrl = url }
        );

        return url;
    }

    public async Task<List<object>> GetUserCompanies(int userId)
    {
        return await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId && uc.Company.Active)
            .Select(uc => (object)new
            {
                uc.Company.Id,
                uc.Company.Name,
                uc.Company.PrimaryColor,
                uc.IsDefault
            })
            .ToListAsync();
    }

    public async Task<bool> AddCompany(int userId, int companyId)
    {
        var exists = await _context.UserCompanies
            .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);
        if (exists) return false;

        _context.UserCompanies.Add(new UserCompany
        {
            UserId = userId,
            CompanyId = companyId,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.Created, "UserCompany", userId,
            newValues: new { CompanyId = companyId });
        return true;
    }

    public async Task<bool> RemoveCompany(int userId, int companyId)
    {
        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);
        if (userCompany is null) return false;
        if (userCompany.IsDefault) return false;

        _context.UserCompanies.Remove(userCompany);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.Deleted, "UserCompany", userId,
            oldValues: new { CompanyId = companyId });
        return true;
    }

    public async Task<bool> SetDefaultCompany(int userId, int companyId)
    {
        var userCompanies = await _context.UserCompanies
            .Where(uc => uc.UserId == userId)
            .ToListAsync();

        var target = userCompanies.FirstOrDefault(uc => uc.CompanyId == companyId);
        if (target is null) return false;

        foreach (var uc in userCompanies)
            uc.IsDefault = uc.CompanyId == companyId;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.Updated, "UserCompany", userId,
            newValues: new { DefaultCompanyId = companyId });
        return true;
    }

    public async Task<bool> AddRole(int userId, int companyId, int roleId)
    {
        var exists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.CompanyId == companyId && ur.RoleId == roleId);
        if (exists) return false;

        _context.UserRoles.Add(new UserRole
        {
            UserId = userId,
            CompanyId = companyId,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.Created, "UserRole", userId,
            newValues: new { CompanyId = companyId, RoleId = roleId });
        return true;
    }

    public async Task<bool> RemoveRole(int userId, int companyId, int roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.CompanyId == companyId && ur.RoleId == roleId);
        if (userRole is null) return false;

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.Deleted, "UserRole", userId,
            oldValues: new { CompanyId = companyId, RoleId = roleId });
        return true;
    }

    public async Task<(bool Success, string? Error, object? Data)> ToggleActive(int id, int currentUserId)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return (false, "Usuário não encontrado.", null);
        if (user.Id == currentUserId) return (false, "Você não pode inativar sua própria conta.", null);

        user.Active = !user.Active;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            user.Active ? AuditActions.Activated : AuditActions.Deactivated,
            "User",
            user.Id,
            newValues: new { user.Active }
        );

        return (true, null, new { user.Id, user.Active });
    }
}
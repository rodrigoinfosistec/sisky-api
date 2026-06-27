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
}
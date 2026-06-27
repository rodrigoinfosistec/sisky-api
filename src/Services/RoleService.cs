using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.Models;
using SiskyApi.DTOs;

namespace SiskyApi.Services;

public class RoleService
{
    private readonly AppDbContext _context;
    private readonly TenantContext _tenantContext;
    private readonly AuditService _auditService;

    public RoleService(AppDbContext context, TenantContext tenantContext, AuditService auditService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditService = auditService;
    }

    public async Task<PaginatedResponseDto<object>> GetAll(int page, int perPage, string sortBy = "name", string sortDir = "asc", string? search = null)
    {
        var query = _context.Roles
            .Where(r => r.TenantId == _tenantContext.TenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.ToLower().Contains(search.ToLower()));

        query = sortBy switch
        {
            "name" => sortDir == "desc" ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            "createdAt" => sortDir == "desc" ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
            _ => sortDir == "desc" ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id)
        };

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var roles = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(r => (object)new
            {
                r.Id,
                r.Name,
                r.IsSystem,
                r.CreatedAt,
                PermissionCount = r.RolePermissions.Count
            })
            .ToListAsync();

        return new PaginatedResponseDto<object>
        {
            Data = roles,
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        };
    }

    public async Task<object?> GetPermissions(int roleId)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                    .ThenInclude(p => p.Module)
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == _tenantContext.TenantId);

        if (role is null) return null;

        var tenantModuleIds = await _context.TenantModules
            .Where(tm => tm.TenantId == _tenantContext.TenantId && tm.Active)
            .Select(tm => tm.ModuleId)
            .ToListAsync();

        var allPermissions = await _context.Permissions
            .Include(p => p.Module)
            .Where(p => tenantModuleIds.Contains(p.ModuleId))
            .ToListAsync();

        var grouped = allPermissions
            .GroupBy(p => new { p.Module.Id, p.Module.Name, p.Module.Slug })
            .Select(g => new
            {
                ModuleId = g.Key.Id,
                ModuleName = g.Key.Name,
                ModuleSlug = g.Key.Slug,
                Permissions = g.Select(p => new
                {
                    p.Id,
                    p.Slug,
                    p.Description,
                    IsGranted = role.RolePermissions.Any(rp => rp.PermissionId == p.Id)
                }).ToList()
            })
            .ToList();

        return new
        {
            role.Id,
            role.Name,
            role.IsSystem,
            Modules = grouped
        };
    }

    public async Task<bool> AddPermission(int roleId, int permissionId)
    {
        var exists = await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        if (exists) return false;

        _context.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Created, "RolePermission", roleId, newValues: new { PermissionId = permissionId });
        return true;
    }

    public async Task<bool> RemovePermission(int roleId, int permissionId)
    {
        var rp = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        if (rp is null) return false;

        _context.RolePermissions.Remove(rp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Deleted, "RolePermission", roleId, oldValues: new { PermissionId = permissionId });
        return true;
    }

    public async Task<object?> Create(string name)
    {
        var exists = await _context.Roles
            .AnyAsync(r => r.TenantId == _tenantContext.TenantId && r.Name == name);
        if (exists) return null;

        var role = new Role
        {
            TenantId = _tenantContext.TenantId!.Value,
            Name = name,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Created, "Role", role.Id, newValues: new { role.Name });

        return new { role.Id, role.Name, role.IsSystem, role.CreatedAt, PermissionCount = 0 };
    }

    public async Task<object?> Update(int roleId, string name)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == _tenantContext.TenantId);
        if (role is null || role.IsSystem) return null;

        var oldName = role.Name;
        role.Name = name;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Updated, "Role", role.Id, oldValues: new { OldName = oldName }, newValues: new { role.Name });

        return new { role.Id, role.Name, role.IsSystem, role.CreatedAt };
    }

    public async Task<bool> Delete(int roleId)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == _tenantContext.TenantId);
        if (role is null || role.IsSystem) return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Deleted, "Role", roleId, oldValues: new { role.Name });
        return true;
    }
}
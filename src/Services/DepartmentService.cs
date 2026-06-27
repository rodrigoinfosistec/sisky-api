using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.DTOs;
using SiskyApi.Models;

namespace SiskyApi.Services;

public class DepartmentService
{
    private readonly AppDbContext _context;
    private readonly TenantContext _tenantContext;
    private readonly AuditService _auditService;

    public DepartmentService(AppDbContext context, TenantContext tenantContext, AuditService auditService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditService = auditService;
    }

    public async Task<PaginatedResponseDto<object>> GetAll(int page, int perPage, string sortBy = "name", string sortDir = "asc", string? search = null)
    {
        var query = _context.Departments
            .Where(d => d.TenantId == _tenantContext.TenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));

        query = sortBy switch
        {
            "name" => sortDir == "desc" ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
            "createdAt" => sortDir == "desc" ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
            _ => sortDir == "desc" ? query.OrderByDescending(d => d.Id) : query.OrderBy(d => d.Id)
        };

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var departments = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(d => (object)new
            {
                d.Id,
                d.Name,
                d.CreatedAt,
                UserCount = _context.Users.Count(u => u.DepartmentId == d.Id)
            })
            .ToListAsync();

        return new PaginatedResponseDto<object>
        {
            Data = departments,
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        };
    }

    public async Task<List<object>> GetAllSimple()
    {
        return await _context.Departments
            .Where(d => d.TenantId == _tenantContext.TenantId)
            .OrderBy(d => d.Name)
            .Select(d => (object)new { d.Id, d.Name })
            .ToListAsync();
    }

    public async Task<object?> Create(string name)
    {
        var exists = await _context.Departments
            .AnyAsync(d => d.TenantId == _tenantContext.TenantId && d.Name == name);
        if (exists) return null;

        var department = new Department
        {
            TenantId = _tenantContext.TenantId!.Value,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Created, "Department", department.Id, newValues: new { department.Name });

        return new { department.Id, department.Name, department.CreatedAt, UserCount = 0 };
    }

    public async Task<object?> Update(int id, string name)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == _tenantContext.TenantId);
        if (department is null) return null;

        var oldName = department.Name;
        department.Name = name;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Updated, "Department", department.Id, oldValues: new { Name = oldName }, newValues: new { department.Name });

        return new { department.Id, department.Name, department.CreatedAt };
    }

    public async Task<(bool Success, string? Error)> Delete(int id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == _tenantContext.TenantId);
        if (department is null) return (false, "Departamento não encontrado.");

        var hasUsers = await _context.Users.AnyAsync(u => u.DepartmentId == id);
        if (hasUsers) return (false, "Este departamento possui usuários associados. Remova-os antes de excluir.");

        await _auditService.LogAsync(AuditActions.Deleted, "Department", department.Id, oldValues: new { department.Name });

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
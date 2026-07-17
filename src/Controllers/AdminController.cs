using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Authorization;
using SiskyApi.Data;
using SiskyApi.DTOs;

namespace SiskyApi.Controllers;

[RequireSuperAdmin]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 15,
        [FromQuery] string? search = null)
    {
        var query = _context.Tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.ToLower().Contains(search.ToLower()) ||
                                     t.Subdomain.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var tenants = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Subdomain,
                t.Active,
                t.CreatedAt,
                UserCount = _context.Users.Count(u => u.TenantId == t.Id),
                CompanyCount = _context.Companies.Count(c => c.TenantId == t.Id)
            })
            .ToListAsync();

        return Ok(new PaginatedResponseDto<object>
        {
            Data = tenants.Cast<object>().ToList(),
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        });
    }

    [HttpGet("tenants/{id}")]
    public async Task<IActionResult> GetTenant(int id)
    {
        var tenant = await _context.Tenants
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Subdomain,
                t.Active,
                t.CreatedAt,
                UserCount = _context.Users.Count(u => u.TenantId == t.Id),
                Companies = _context.Companies
                    .Where(c => c.TenantId == t.Id)
                    .Select(c => new { c.Id, c.Name, c.Active })
                    .ToList(),
                Modules = _context.TenantModules
                    .Where(tm => tm.TenantId == t.Id)
                    .Select(tm => new { tm.Module.Id, tm.Module.Name, tm.Module.Slug, tm.Active })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (tenant is null) return NotFound();
        return Ok(tenant);
    }

    [HttpPatch("tenants/{id}/toggle-active")]
    public async Task<IActionResult> ToggleTenantActive(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant is null) return NotFound();

        tenant.Active = !tenant.Active;
        await _context.SaveChangesAsync();

        return Ok(new { tenant.Id, tenant.Active });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var totalTenants = await _context.Tenants.CountAsync();
        var activeTenants = await _context.Tenants.CountAsync(t => t.Active);
        var totalUsers = await _context.Users.CountAsync();
        var newTenantsThisMonth = await _context.Tenants
            .CountAsync(t => t.CreatedAt >= DateTime.UtcNow.AddMonths(-1));

        return Ok(new
        {
            totalTenants,
            activeTenants,
            totalUsers,
            newTenantsThisMonth
        });
    }
}
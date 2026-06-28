using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Authorization;
using SiskyApi.Data;
using SiskyApi.DTOs;
using SiskyApi.Services;

namespace SiskyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TenantContext _tenantContext;

    public AuditController(AppDbContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    [RequirePermission("audit.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20,
        [FromQuery] string? entity = null,
        [FromQuery] string? action = null,
        [FromQuery] string? search = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null)
    {
        var companyIdClaim = User.FindFirst("company_id")?.Value;
        int? companyId = int.TryParse(companyIdClaim, out var cid) ? cid : null;

        var query = _context.AuditLogs
            .Where(a => a.TenantId == _tenantContext.TenantId)
            .AsQueryable();

        if (companyId.HasValue)
            query = query.Where(a => a.CompanyId == companyId.Value);

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.Entity == entity);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.UserName.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
            query = query.Where(a => a.CreatedAt >= fromDate.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
            query = query.Where(a => a.CreatedAt <= toDate.ToUniversalTime().AddDays(1));

        query = query.OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var logs = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(a => new
            {
                a.Id,
                a.UserName,
                a.Action,
                a.Entity,
                a.EntityId,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new PaginatedResponseDto<object>
        {
            Data = logs.Cast<object>().ToList(),
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        });
    }
}
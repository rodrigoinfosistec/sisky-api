using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.Services;
using StackExchange.Redis;

namespace SiskyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TenantContext _tenantContext;
    private readonly IConnectionMultiplexer _redis;

    public DashboardController(AppDbContext context, TenantContext tenantContext, IConnectionMultiplexer redis)
    {
        _context = context;
        _tenantContext = tenantContext;
        _redis = redis;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var companyIdClaim = User.FindFirst("company_id")?.Value;
        int? companyId = int.TryParse(companyIdClaim, out var cid) ? cid : null;

        var usersQuery = _context.Users
            .Where(u => _context.UserCompanies
                .Any(uc => uc.UserId == u.Id && uc.CompanyId == companyId));

        var totalUsers = await usersQuery.CountAsync();

        var activeUsers = await usersQuery
            .Where(u => u.Active)
            .CountAsync();

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var newUsersThisMonth = await usersQuery
            .Where(u => u.CreatedAt >= startOfMonth)
            .CountAsync();

        // Sessões ativas no Redis
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var sessionKeys = server.Keys(pattern: "session:*");
        var activeSessions = sessionKeys.Count();

        return Ok(new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            NewUsersThisMonth = newUsersThisMonth,
            ActiveSessions = activeSessions
        });
    }
}
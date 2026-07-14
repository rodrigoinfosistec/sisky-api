using System.Security.Claims;
using System.Text.Json;
using SiskyApi.Data;
using SiskyApi.Constants;
using SiskyApi.Models;

namespace SiskyApi.Services;

public class AuditService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantContext _tenantContext;

    public AuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor, TenantContext tenantContext)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    public async Task LogAsync(string action, string entity, int? entityId, object? oldValues = null, object? newValues = null, int? tenantIdOverride = null, int? companyIdOverride = null, string? userNameOverride = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = userNameOverride ?? httpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? "Sistema";
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var companyIdClaim = httpContext?.User?.FindFirstValue("company_id");
        var tenantIdFromItems = httpContext?.Items["TenantId"] is int t ? t : (int?)null;

        var log = new AuditLog
        {
            TenantId = tenantIdOverride ?? tenantIdFromItems ?? _tenantContext.TenantId,
            CompanyId = companyIdOverride ?? (int.TryParse(companyIdClaim, out var companyId) ? companyId : null),
            UserId = int.TryParse(userId, out var uid) ? uid : entityId,
            UserName = userName,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}

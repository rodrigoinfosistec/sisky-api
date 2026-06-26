namespace SiskyApi.Services;

public class TenantContext
{
    public int? TenantId { get; private set; }
    public string? TenantName { get; private set; }
    public bool HasTenant => TenantId.HasValue;

    public void SetFromHttpContext(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.Items["TenantId"] is int tenantId)
        {
            TenantId = tenantId;
            TenantName = context.Items["TenantName"]?.ToString();
        }
    }
}
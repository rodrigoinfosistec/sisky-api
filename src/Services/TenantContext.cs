namespace SiskyApi.Services;

public class TenantContext
{
    public int? TenantId { get; private set; }
    public string? TenantName { get; private set; }
    public int? CompanyId { get; private set; }
    public bool HasTenant => TenantId.HasValue;

    public void SetFromHttpContext(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.Items["TenantId"] is int tenantId)
        {
            TenantId = tenantId;
            TenantName = context.Items["TenantName"]?.ToString();
        }

        // Pega o company_id do JWT
        var companyIdClaim = context?.User?.FindFirst("company_id")?.Value;
        if (int.TryParse(companyIdClaim, out var companyId))
            CompanyId = companyId;
    }
}
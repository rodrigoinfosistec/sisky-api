namespace SiskyApi.Services;

public class TenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? TenantId
    {
        get
        {
            if (_httpContextAccessor.HttpContext?.Items["TenantId"] is int tenantId)
                return tenantId;
            return null;
        }
    }

    public string? TenantName
    {
        get => _httpContextAccessor.HttpContext?.Items["TenantName"]?.ToString();
    }

    public int? CompanyId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("company_id")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }

    public bool HasTenant => TenantId.HasValue;
}
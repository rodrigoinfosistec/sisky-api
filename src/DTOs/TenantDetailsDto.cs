namespace SiskyApi.DTOs;

public class TenantDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public List<TenantDetailsCompanyDto> Companies { get; set; } = new();
    public List<TenantDetailsModuleDto> Modules { get; set; } = new();
}

public class TenantDetailsCompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
}

public class TenantDetailsModuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool Active { get; set; }
}
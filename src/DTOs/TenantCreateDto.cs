namespace SiskyApi.DTOs;

public class TenantCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
}
namespace SiskyApi.DTOs;

public class TenantResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int CompanyCount { get; set; }
}
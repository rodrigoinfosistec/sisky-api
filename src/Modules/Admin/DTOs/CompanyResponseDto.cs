namespace SiskyApi.Modules.Admin.DTOs;

public class CompanyResponseDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}
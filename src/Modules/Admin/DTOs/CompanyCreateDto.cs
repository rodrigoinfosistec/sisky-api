namespace SiskyApi.Modules.Admin.DTOs;

public class CompanyCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; }
}
namespace SiskyApi.Models;

public class CompanyModule
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
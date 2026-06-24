namespace SiskyApi.Models;

public class Module
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
    public ICollection<CompanyModule> CompanyModules { get; set; } = new List<CompanyModule>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
namespace SiskyApi.Models;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
    public ICollection<Company> Companies { get; set; } = new List<Company>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
namespace SiskyApi.Models;

public class TenantModule
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
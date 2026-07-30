namespace SiskyApi.Shared.Models;

public class IoTDevice
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "dht22", "hc_sr04", etc
    public string ApiKeyHash { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<IoTReading> Readings { get; set; } = new List<IoTReading>();
}
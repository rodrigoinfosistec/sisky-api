namespace SiskyApi.Shared.Models;

public class IoTReading
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public IoTDevice Device { get; set; } = null!;
    public int TenantId { get; set; }
    public string Type { get; set; } = string.Empty; // "dht22", "hc_sr04"
    public string Data { get; set; } = "{}"; // JSONB
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
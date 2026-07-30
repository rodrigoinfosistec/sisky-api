namespace SiskyApi.Modules.IoT.DTOs;

public class IoTDeviceResponseDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ApiKey { get; set; } // só retorna na criação
}
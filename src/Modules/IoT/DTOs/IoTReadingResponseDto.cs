namespace SiskyApi.Modules.IoT.DTOs;

public class IoTReadingResponseDto
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}
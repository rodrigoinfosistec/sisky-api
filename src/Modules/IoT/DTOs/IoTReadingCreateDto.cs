namespace SiskyApi.Modules.IoT.DTOs;

public class IoTReadingCreateDto
{
    public string Type { get; set; } = string.Empty;
    public object Data { get; set; } = new();
}
namespace SiskyApi.Modules.Notifications.DTOs;

public class NotificationResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; }
}
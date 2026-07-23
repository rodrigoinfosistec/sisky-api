namespace SiskyApi.Models;

public class TicketMessage
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsAdminReply { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
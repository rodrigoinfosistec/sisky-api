namespace SiskyApi.Models;

public class Ticket
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string UserName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public string Priority { get; set; } = "medium";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
}
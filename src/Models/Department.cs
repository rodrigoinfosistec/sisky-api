namespace SiskyApi.Models;

public class Department
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
}
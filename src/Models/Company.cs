namespace SiskyApi.Models;

public class Company
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CompanyModule> CompanyModules { get; set; } = new List<CompanyModule>();
    public ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
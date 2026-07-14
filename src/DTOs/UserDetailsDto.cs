namespace SiskyApi.DTOs;

public class UserDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<UserDetailsCompanyDto> Companies { get; set; } = new();
    public List<UserDetailsRoleDto> UserRoles { get; set; } = new();
    public List<UserDetailsTenantRoleDto> TenantRoles { get; set; } = new();
    public List<UserDetailsTenantCompanyDto> TenantCompanies { get; set; } = new();
}

public class UserDetailsCompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; }
    public bool IsDefault { get; set; }
}

public class UserDetailsRoleDto
{
    public int CompanyId { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}

public class UserDetailsTenantRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}

public class UserDetailsTenantCompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; }
}
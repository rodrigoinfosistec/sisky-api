namespace SiskyApi.DTOs;

public class RoleDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public List<RoleDetailsModuleDto> Modules { get; set; } = new();
}

public class RoleDetailsModuleDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleSlug { get; set; } = string.Empty;
    public List<RoleDetailsPermissionDto> Permissions { get; set; } = new();
}

public class RoleDetailsPermissionDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
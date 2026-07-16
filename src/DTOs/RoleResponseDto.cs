namespace SiskyApi.DTOs;

public class RoleResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PermissionCount { get; set; }
}
namespace SiskyApi.DTOs;

public class UserChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string NewPasswordConfirmation { get; set; } = string.Empty;
}
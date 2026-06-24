namespace SiskyApi.DTOs;

public class ResetPasswordDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string NewPasswordConfirmation { get; set; } = string.Empty;
}
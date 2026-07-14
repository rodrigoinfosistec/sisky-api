namespace SiskyApi.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
}
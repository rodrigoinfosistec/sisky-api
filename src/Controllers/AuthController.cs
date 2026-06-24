using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.DTOs;
using SiskyApi.Services;

namespace SiskyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IValidator<LoginDto> _loginValidator;

    public AuthController(AuthService authService, IValidator<LoginDto> loginValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var validation = await _loginValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.Login(dto, ipAddress, userAgent);
        if (result is null) return Unauthorized("E-mail ou senha incorretos.");

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var token = await _authService.Refresh(refreshToken);
        if (token is null) return Unauthorized("Refresh token inválido ou expirado.");

        return Ok(new { token });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.Logout(token, userId);
        return NoContent();
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessions = await _authService.GetSessions(userId);
        return Ok(sessions);
    }

    [Authorize]
    [HttpDelete("sessions/{tokenSuffix}")]
    public async Task<IActionResult> RevokeSession(string tokenSuffix)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.RevokeSession(userId, tokenSuffix);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("sessions")]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.RevokeAllSessions(userId, token);
        return NoContent();
    }
}
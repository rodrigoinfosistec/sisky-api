using SiskyApi.Services;

namespace SiskyApi.Middlewares;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, AuthService authService)
    {
        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        if (!string.IsNullOrEmpty(token) && await authService.IsTokenBlacklisted(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Token inválido.");
            return;
        }

        await _next(context);
    }
}
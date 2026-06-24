using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using SiskyApi.Data;
using SiskyApi.DTOs;

namespace SiskyApi.Services;

public class SessionInfo
{
    public string Token { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IDatabase _redis;

    public AuthService(AppDbContext context, IConfiguration configuration, IConnectionMultiplexer redis)
    {
        _context = context;
        _configuration = configuration;
        _redis = redis.GetDatabase();
    }

    public async Task<object?> Login(LoginDto dto, string ipAddress, string userAgent)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password)) return null;

        var token = GenerateJwtToken(user.Id, user.Email, user.Name);
        var expiresAt = DateTime.UtcNow.AddHours(double.Parse(_configuration["Jwt:ExpiresInHours"]!));

        var session = new SessionInfo
        {
            Token = token,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        await _redis.StringSetAsync(
            $"session:{user.Id}:{token[^10..]}",
            JsonSerializer.Serialize(session),
            expiresAt - DateTime.UtcNow);

        if (dto.RememberMe)
        {
            var refreshToken = Guid.NewGuid().ToString();
            await _redis.StringSetAsync(
                $"refresh_token:{refreshToken}",
                user.Id.ToString(),
                TimeSpan.FromDays(30));

            return new { token, refreshToken };
        }

        return new { token };
    }

    public async Task<string?> Refresh(string refreshToken)
    {
        var userId = await _redis.StringGetAsync($"refresh_token:{refreshToken}");
        if (userId.IsNullOrEmpty) return null;

        var user = await _context.Users.FindAsync(int.Parse(userId!));
        if (user is null) return null;

        await _redis.KeyDeleteAsync($"refresh_token:{refreshToken}");

        var newRefreshToken = Guid.NewGuid().ToString();
        await _redis.StringSetAsync(
            $"refresh_token:{newRefreshToken}",
            user.Id.ToString(),
            TimeSpan.FromDays(30));

        var newToken = GenerateJwtToken(user.Id, user.Email, user.Name);
        return newToken;
    }

    public async Task Logout(string token, int userId)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var expiration = jwt.ValidTo - DateTime.UtcNow;

        await _redis.StringSetAsync($"blacklist:{token}", "true", expiration);
        await _redis.KeyDeleteAsync($"session:{userId}:{token[^10..]}");
    }

    public async Task<bool> IsTokenBlacklisted(string token)
    {
        return await _redis.KeyExistsAsync($"blacklist:{token}");
    }

    public async Task<List<SessionInfo>> GetSessions(int userId)
    {
        var pattern = $"session:{userId}:*";
        var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern);

        var sessions = new List<SessionInfo>();
        foreach (var key in keys)
        {
            var value = await _redis.StringGetAsync(key);
            if (!value.IsNullOrEmpty)
            {
                var session = JsonSerializer.Deserialize<SessionInfo>(value!.ToString());
                if (session != null) sessions.Add(session);
            }
        }

        return sessions.OrderByDescending(s => s.CreatedAt).ToList();
    }

    public async Task RevokeSession(int userId, string tokenSuffix)
    {
        var key = $"session:{userId}:{tokenSuffix}";
        var value = await _redis.StringGetAsync(key);
        if (!value.IsNullOrEmpty)
        {
            var session = JsonSerializer.Deserialize<SessionInfo>(value!.ToString());
            if (session != null)
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(session.Token);
                var expiration = jwt.ValidTo - DateTime.UtcNow;
                if (expiration > TimeSpan.Zero)
                    await _redis.StringSetAsync($"blacklist:{session.Token}", "true", expiration);
            }
            await _redis.KeyDeleteAsync(key);
        }
    }

    public async Task RevokeAllSessions(int userId, string currentToken)
    {
        var pattern = $"session:{userId}:*";
        var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern).ToList();

        foreach (var key in keys)
        {
            var value = await _redis.StringGetAsync(key);
            if (!value.IsNullOrEmpty)
            {
                var session = JsonSerializer.Deserialize<SessionInfo>(value!.ToString());
                if (session != null && session.Token != currentToken)
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(session.Token);
                    var expiration = jwt.ValidTo - DateTime.UtcNow;
                    if (expiration > TimeSpan.Zero)
                        await _redis.StringSetAsync($"blacklist:{session.Token}", "true", expiration);
                    await _redis.KeyDeleteAsync(key);
                }
            }
        }
    }

    private string GenerateJwtToken(int id, string email, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name)
        };

        var expiration = DateTime.UtcNow.AddHours(
            double.Parse(_configuration["Jwt:ExpiresInHours"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
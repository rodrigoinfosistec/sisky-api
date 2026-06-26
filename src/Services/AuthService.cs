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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.Active);
        if (user is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password)) return null;

        var (companyId, tenantId, roles, permissions) = await GetUserCompanyContext(user.Id);

        Console.WriteLine($"[DEBUG LOGIN] userId={user.Id} companyId={companyId} tenantId={tenantId} roles={roles.Count} permissions={permissions.Count}");

        var token = GenerateJwtToken(user.Id, user.Email, user.Name, tenantId, companyId, roles, permissions);
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

    private async Task<(int? companyId, int? tenantId, List<string> roles, List<string> permissions)> GetUserCompanyContext(int userId, int? companyId = null)
    {
        var userCompany = companyId.HasValue
            ? await _context.UserCompanies
                .Include(uc => uc.Company)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == companyId)
            : await _context.UserCompanies
                .Include(uc => uc.Company)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.IsDefault)
              ?? await _context.UserCompanies
                .Include(uc => uc.Company)
                .FirstOrDefaultAsync(uc => uc.UserId == userId);

        if (userCompany is null)
            return (null, null, new List<string>(), new List<string>());

        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Where(ur => ur.UserId == userId && ur.CompanyId == userCompany.CompanyId)
            .ToListAsync();

        var roles = userRoles.Select(ur => ur.Role.Name).Distinct().ToList();
        var permissions = userRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Slug)
            .Distinct()
            .ToList();

        return (userCompany.CompanyId, userCompany.Company.TenantId, roles, permissions);
    }

    private string GenerateJwtToken(int id, string email, string name, int? tenantId = null, int? companyId = null, List<string>? roles = null, List<string>? permissions = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, id.ToString()),
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Name, name),
    };

        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));

        if (companyId.HasValue)
            claims.Add(new Claim("company_id", companyId.Value.ToString()));

        if (roles != null)
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

        if (permissions != null)
            foreach (var permission in permissions)
                claims.Add(new Claim("permission", permission));

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

    public async Task<bool> ForgotPassword(string email, string frontendUrl, EmailService emailService)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return true; // não revela se o email existe

        var token = Guid.NewGuid().ToString();
        await _redis.StringSetAsync(
            $"password_reset:{token}",
            user.Id.ToString(),
            TimeSpan.FromHours(1));

        var resetLink = $"{frontendUrl}/reset-password?token={token}";
        await emailService.SendPasswordResetAsync(user.Email, user.Name, resetLink);

        return true;
    }

    public async Task<bool> ResetPassword(ResetPasswordDto dto)
    {
        var userId = await _redis.StringGetAsync($"password_reset:{dto.Token}");
        if (userId.IsNullOrEmpty) return false;

        var user = await _context.Users.FindAsync(int.Parse(userId!));
        if (user is null) return false;

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        await _redis.KeyDeleteAsync($"password_reset:{dto.Token}");

        return true;
    }

    public async Task<string?> SwitchCompany(int userId, int companyId)
    {
        var userCompany = await _context.UserCompanies
            .Include(uc => uc.Company)
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);

        if (userCompany is null) return null;

        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        var (cId, tenantId, roles, permissions) = await GetUserCompanyContext(userId, companyId);

        return GenerateJwtToken(user.Id, user.Email, user.Name, tenantId, cId, roles, permissions);
    }
}
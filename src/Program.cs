using SiskyApi.Data.Seeders;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using SiskyApi.Authorization;
using SiskyApi.Data;
using SiskyApi.Services;
using Scalar.AspNetCore;
using Resend;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<RoleService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis")!;
    ConfigurationOptions options;

    if (redisConnection.StartsWith("redis://") || redisConnection.StartsWith("rediss://"))
    {
        options = ConfigurationOptions.Parse(new Uri(redisConnection).ToString());
        var uri = new Uri(redisConnection);
        options = new ConfigurationOptions
        {
            EndPoints = { { uri.Host, uri.Port } },
            Password = uri.UserInfo.Split(':').LastOrDefault(),
            Ssl = redisConnection.StartsWith("rediss://"),
            AbortOnConnectFail = false
        };
    }
    else
    {
        options = ConfigurationOptions.Parse(redisConnection);
        options.AbortOnConnectFail = false;
    }

    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddSingleton<IResend>(_ =>
    ResendClient.Create(new ResendClientOptions
    {
        ApiToken = builder.Configuration["Mail:ApiKey"]!
    }));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionsConfig.All)
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});

builder.Services.AddMemoryCache();

builder.Services.AddScoped<TenantContext>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Sisky API";
        options.Theme = ScalarTheme.DeepSpace;
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<SiskyApi.Middlewares.DynamicCorsMiddleware>();
app.UseMiddleware<SiskyApi.Middlewares.TokenBlacklistMiddleware>();
app.UseMiddleware<SiskyApi.Middlewares.TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db, app.Environment, app.Configuration);
}

app.Run();
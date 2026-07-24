using SiskyApi.Data.Seeders;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using SiskyApi.Authorization;
using SiskyApi.Data;
using SiskyApi.HealthChecks;
using SiskyApi.Services;
using Scalar.AspNetCore;
using Resend;
using Amazon.S3;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"];
    o.TracesSampleRate = 0.1;
    o.Debug = builder.Environment.IsDevelopment();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<SettingsService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis")!;
    ConfigurationOptions options;

    if (redisConnection.StartsWith("redis://") || redisConnection.StartsWith("rediss://"))
    {
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

builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var accountId = builder.Configuration["Storage:AccountId"]!;
    var accessKeyId = builder.Configuration["Storage:AccessKeyId"]!;
    var secretAccessKey = builder.Configuration["Storage:SecretAccessKey"]!;

    var config = new AmazonS3Config
    {
        ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
        ForcePathStyle = true
    };

    return new AmazonS3Client(accessKeyId, secretAccessKey, config);
});

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddCheck<RedisHealthCheck>("redis");

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
builder.Services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionsConfig.All)
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }

    options.AddPolicy(RequireSuperAdminAttribute.PolicyName, policy =>
        policy.Requirements.Add(new SuperAdminRequirement()));
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

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(app.Configuration) }
});

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                error = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db, app.Environment, app.Configuration);
}

app.Run();
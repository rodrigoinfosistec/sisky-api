using SiskyApi.Data.Seeders;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using SiskyApi.Data;
using SiskyApi.Services;
using SiskyApi.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();

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

builder.Services.AddValidatorsFromAssemblyContaining<UserCreateValidator>();

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://sisky-web.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseMiddleware<SiskyApi.Middlewares.TokenBlacklistMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db, app.Environment);
}

app.Run();
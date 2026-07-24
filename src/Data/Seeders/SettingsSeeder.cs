using Microsoft.EntityFrameworkCore;
using SiskyApi.Models;

namespace SiskyApi.Data.Seeders;

public static class SettingsSeeder
{
    public static async Task SeedAsync(AppDbContext context, IConfiguration configuration)
    {
        var defaults = new Dictionary<string, string>
        {
            { "support_email", configuration["Admin:SupportEmail"] ?? "suporte@sisky.com.br" },
            { "system_name", "Sisky" },
        };

        foreach (var (key, value) in defaults)
        {
            var exists = await context.Settings.AnyAsync(s => s.Key == key);
            if (!exists)
            {
                context.Settings.Add(new Setting
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
namespace SiskyApi.Data.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
    {
        await PermissionSeeder.SeedAsync(context);
        await UserSeeder.SeedAsync(context, env, configuration);
        await TenantSeeder.SeedAsync(context, env, configuration);
    }
}
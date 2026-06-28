namespace SiskyApi.Data.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, IWebHostEnvironment env)
    {
        await PermissionSeeder.SeedAsync(context);
        await UserSeeder.SeedAsync(context, env);
        await TenantSeeder.SeedAsync(context, env);
    }
}
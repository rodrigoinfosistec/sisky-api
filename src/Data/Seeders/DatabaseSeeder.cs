namespace SiskyApi.Data.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, IWebHostEnvironment env)
    {
        await UserSeeder.SeedAsync(context, env);
    }
}
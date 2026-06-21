using SiskyApi.Models;

namespace SiskyApi.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (context.Users.Any()) return;

        var users = new List<User>
        {
            new User
            {
                Name = "Rodrigo",
                Email = "rodrigo@sisky.com.br",
                Password = BCrypt.Net.BCrypt.HashPassword("password"),
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }
}
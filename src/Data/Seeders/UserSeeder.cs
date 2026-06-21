using Bogus;
using SiskyApi.Models;

namespace SiskyApi.Data.Seeders;

public static class UserSeeder
{
    public static async Task SeedAsync(AppDbContext context, IWebHostEnvironment env)
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

        if (env.IsDevelopment())
        {
            var faker = new Faker<User>("pt_BR")
                .RuleFor(u => u.Name, f => f.Name.FullName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
                .RuleFor(u => u.Password, f => BCrypt.Net.BCrypt.HashPassword("password"))
                .RuleFor(u => u.CreatedAt, f => f.Date.Past(1).ToUniversalTime());

            users.AddRange(faker.Generate(49));
        }

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }
}
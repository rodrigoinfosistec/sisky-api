using Bogus;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Models;

namespace SiskyApi.Data.Seeders;

public static class UserSeeder
{
    public static async Task SeedAsync(AppDbContext context, IWebHostEnvironment env)
    {
        var exists = await context.Users
            .AnyAsync(u => u.Email == "rodrigo.infosistec@gmail.com");

        if (!exists)
        {
            var admin = new User
            {
                Name = "Administrador",
                Email = "rodrigo.infosistec@gmail.com",
                Password = BCrypt.Net.BCrypt.HashPassword("password"),
                Active = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }

        if (env.IsDevelopment())
        {
            var faker = new Faker<User>("pt_BR")
                .RuleFor(u => u.Name, f => f.Name.FullName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
                .RuleFor(u => u.Password, f => BCrypt.Net.BCrypt.HashPassword("password"))
                .RuleFor(u => u.Active, f => true)
                .RuleFor(u => u.CreatedAt, f => f.Date.Past(1).ToUniversalTime());

            var fakeUsers = faker.Generate(49);
            foreach (var user in fakeUsers)
            {
                var emailExists = await context.Users.AnyAsync(u => u.Email == user.Email);
                if (!emailExists)
                {
                    context.Users.Add(user);
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
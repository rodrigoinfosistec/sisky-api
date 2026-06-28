using Microsoft.EntityFrameworkCore;
using SiskyApi.Authorization;
using SiskyApi.Models;

namespace SiskyApi.Data.Seeders;

public static class PermissionSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        foreach (var moduleConfig in PermissionsConfig.Modules)
        {
            // Cria o módulo se não existir
            var module = await context.Modules
                .FirstOrDefaultAsync(m => m.Slug == moduleConfig.Slug);

            if (module is null)
            {
                module = new Module
                {
                    Name = moduleConfig.Name,
                    Slug = moduleConfig.Slug,
                    Description = moduleConfig.Description,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Modules.Add(module);
                await context.SaveChangesAsync();
            }

            // Cria as permissões do módulo que não existem
            foreach (var action in moduleConfig.Actions)
            {
                var slug = $"{moduleConfig.Slug}.{action}";
                var exists = await context.Permissions
                    .AnyAsync(p => p.Slug == slug);

                if (!exists)
                {
                    context.Permissions.Add(new Permission
                    {
                        ModuleId = module.Id,
                        Slug = slug,
                        Description = PermissionsConfig.DescriptionFor(slug),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
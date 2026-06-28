using SiskyApi.Authorization;
using SiskyApi.Models;

namespace SiskyApi.Data.Seeders;

public static class TenantSeeder
{
    public static async Task SeedAsync(AppDbContext context, IWebHostEnvironment env)
    {
        if (context.Tenants.Any()) return;

        // Módulos do sistema
        var modules = new List<Module>
        {
            new Module { Name = "Usuários", Slug = "users", Description = "Gestão de usuários e permissões" },
            new Module { Name = "Financeiro", Slug = "financeiro", Description = "Gestão financeira" },
            new Module { Name = "RH", Slug = "rh", Description = "Recursos humanos" },
            new Module { Name = "CRM", Slug = "crm", Description = "Gestão de clientes" },
        };
        await context.Modules.AddRangeAsync(modules);
        await context.SaveChangesAsync();

        // Permissões baseadas no PermissionsConfig
        var permissions = new List<Permission>();
        foreach (var slug in PermissionsConfig.All)
        {
            var parts = slug.Split('.');
            var moduleSlug = parts[0];
            var action = parts[1];
            var module = modules.FirstOrDefault(m => m.Slug == moduleSlug);
            if (module is null) continue;

            var description = action switch
            {
                "view" => $"Visualizar {module.Name}",
                "create" => $"Criar em {module.Name}",
                "edit" => $"Editar em {module.Name}",
                "delete" => $"Excluir em {module.Name}",
                _ => slug
            };

            permissions.Add(new Permission
            {
                ModuleId = module.Id,
                Slug = slug,
                Description = description
            });
        }
        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();

        // Tenant de exemplo
        var tenant = new Tenant
        {
            Name = "Grupo Draxel",
            Subdomain = "draxel",
            Active = true
        };
        await context.Tenants.AddAsync(tenant);
        await context.SaveChangesAsync();

        // Módulos do tenant
        await context.TenantModules.AddRangeAsync(modules.Select(m => new TenantModule
        {
            TenantId = tenant.Id,
            ModuleId = m.Id,
            Active = true
        }));
        await context.SaveChangesAsync();

        // Roles do sistema
        var superAdminRole = new Role { TenantId = tenant.Id, Name = "Super Admin", IsSystem = true };
        var adminTenantRole = new Role { TenantId = tenant.Id, Name = "Admin Tenant", IsSystem = true };
        await context.Roles.AddRangeAsync(superAdminRole, adminTenantRole);
        await context.SaveChangesAsync();

        // Super Admin tem todas as permissões
        await context.RolePermissions.AddRangeAsync(permissions.Select(p => new RolePermission
        {
            RoleId = superAdminRole.Id,
            PermissionId = p.Id
        }));
        await context.SaveChangesAsync();

        // Empresas do tenant
        var company1 = new Company { TenantId = tenant.Id, Name = "Draxel São Paulo", PrimaryColor = "#111111", Active = true };
        var company2 = new Company { TenantId = tenant.Id, Name = "Draxel Rio", PrimaryColor = "#1a56db", Active = true };
        await context.Companies.AddRangeAsync(company1, company2);
        await context.SaveChangesAsync();

        // Módulos das empresas
        await context.CompanyModules.AddRangeAsync(modules.Select(m => new CompanyModule
        {
            CompanyId = company1.Id,
            ModuleId = m.Id,
            Active = true
        }));
        await context.CompanyModules.AddRangeAsync(modules.Take(2).Select(m => new CompanyModule
        {
            CompanyId = company2.Id,
            ModuleId = m.Id,
            Active = true
        }));
        await context.SaveChangesAsync();

        // Associa o usuário Rodrigo às duas empresas
        var rodrigo = context.Users.FirstOrDefault(u => u.Email == "rodrigo.infosistec@gmail.com");
        if (rodrigo != null)
        {
            await context.UserCompanies.AddRangeAsync(
                new UserCompany { UserId = rodrigo.Id, CompanyId = company1.Id, IsDefault = true },
                new UserCompany { UserId = rodrigo.Id, CompanyId = company2.Id, IsDefault = false }
            );
            await context.UserRoles.AddRangeAsync(
                new UserRole { UserId = rodrigo.Id, CompanyId = company1.Id, RoleId = superAdminRole.Id },
                new UserRole { UserId = rodrigo.Id, CompanyId = company2.Id, RoleId = superAdminRole.Id }
            );
            await context.SaveChangesAsync();
        }
    }
}
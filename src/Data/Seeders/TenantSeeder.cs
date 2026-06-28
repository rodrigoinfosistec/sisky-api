using Microsoft.EntityFrameworkCore;
using SiskyApi.Models;

namespace SiskyApi.Data.Seeders;

public static class TenantSeeder
{
    public static async Task SeedAsync(AppDbContext context, IWebHostEnvironment env)
    {
        var modules = await context.Modules.ToListAsync();
        var permissions = await context.Permissions.ToListAsync();

        // Tenant
        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == "default");

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Name = "Tenant Default",
                Subdomain = "default",
                Active = true
            };
            await context.Tenants.AddAsync(tenant);
            await context.SaveChangesAsync();
        }

        // Atualiza o TenantId do administrador
        var admin = await context.Users
            .FirstOrDefaultAsync(u => u.Email == "rodrigo.infosistec@gmail.com");

        if (admin != null && admin.TenantId is null)
        {
            admin.TenantId = tenant.Id;
            await context.SaveChangesAsync();
        }

        // Módulos do tenant
        foreach (var module in modules)
        {
            var exists = await context.TenantModules
                .AnyAsync(tm => tm.TenantId == tenant.Id && tm.ModuleId == module.Id);
            if (!exists)
            {
                context.TenantModules.Add(new TenantModule
                {
                    TenantId = tenant.Id,
                    ModuleId = module.Id,
                    Active = true
                });
            }
        }
        await context.SaveChangesAsync();

        // Roles do sistema
        var superAdminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == "Super Admin");

        if (superAdminRole is null)
        {
            superAdminRole = new Role { TenantId = tenant.Id, Name = "Super Admin", IsSystem = true };
            context.Roles.Add(superAdminRole);
            await context.SaveChangesAsync();
        }

        var adminTenantRole = await context.Roles
            .FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == "Admin Tenant");

        if (adminTenantRole is null)
        {
            adminTenantRole = new Role { TenantId = tenant.Id, Name = "Admin Tenant", IsSystem = true };
            context.Roles.Add(adminTenantRole);
            await context.SaveChangesAsync();
        }

        // Super Admin tem todas as permissões
        foreach (var permission in permissions)
        {
            var exists = await context.RolePermissions
                .AnyAsync(rp => rp.RoleId == superAdminRole.Id && rp.PermissionId == permission.Id);
            if (!exists)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = superAdminRole.Id,
                    PermissionId = permission.Id
                });
            }
        }
        await context.SaveChangesAsync();

        // Empresas
        var company1 = await context.Companies
            .FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Name == "Empresa Principal");

        if (company1 is null)
        {
            company1 = new Company { TenantId = tenant.Id, Name = "Empresa Principal", PrimaryColor = "#111111", Active = true };
            context.Companies.Add(company1);
            await context.SaveChangesAsync();
        }

        var company2 = await context.Companies
            .FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Name == "Empresa Secundária");

        if (company2 is null)
        {
            company2 = new Company { TenantId = tenant.Id, Name = "Empresa Secundária", PrimaryColor = "#1a56db", Active = true };
            context.Companies.Add(company2);
            await context.SaveChangesAsync();
        }

        // Módulos das empresas
        foreach (var module in modules)
        {
            var exists = await context.CompanyModules
                .AnyAsync(cm => cm.CompanyId == company1.Id && cm.ModuleId == module.Id);
            if (!exists)
            {
                context.CompanyModules.Add(new CompanyModule
                {
                    CompanyId = company1.Id,
                    ModuleId = module.Id,
                    Active = true
                });
            }
        }

        foreach (var module in modules.Take(2))
        {
            var exists = await context.CompanyModules
                .AnyAsync(cm => cm.CompanyId == company2.Id && cm.ModuleId == module.Id);
            if (!exists)
            {
                context.CompanyModules.Add(new CompanyModule
                {
                    CompanyId = company2.Id,
                    ModuleId = module.Id,
                    Active = true
                });
            }
        }
        await context.SaveChangesAsync();

        if (admin != null)
        {
            var uc1 = await context.UserCompanies
                .AnyAsync(uc => uc.UserId == admin.Id && uc.CompanyId == company1.Id);
            if (!uc1)
            {
                context.UserCompanies.Add(new UserCompany
                {
                    UserId = admin.Id,
                    CompanyId = company1.Id,
                    IsDefault = true
                });
            }

            var uc2 = await context.UserCompanies
                .AnyAsync(uc => uc.UserId == admin.Id && uc.CompanyId == company2.Id);
            if (!uc2)
            {
                context.UserCompanies.Add(new UserCompany
                {
                    UserId = admin.Id,
                    CompanyId = company2.Id,
                    IsDefault = false
                });
            }

            var ur1 = await context.UserRoles
                .AnyAsync(ur => ur.UserId == admin.Id && ur.CompanyId == company1.Id && ur.RoleId == superAdminRole.Id);
            if (!ur1)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = admin.Id,
                    CompanyId = company1.Id,
                    RoleId = superAdminRole.Id
                });
            }

            var ur2 = await context.UserRoles
                .AnyAsync(ur => ur.UserId == admin.Id && ur.CompanyId == company2.Id && ur.RoleId == superAdminRole.Id);
            if (!ur2)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = admin.Id,
                    CompanyId = company2.Id,
                    RoleId = superAdminRole.Id
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
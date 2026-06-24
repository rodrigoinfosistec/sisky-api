using Microsoft.EntityFrameworkCore;
using SiskyApi.Models;

namespace SiskyApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<TenantModule> TenantModules { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<CompanyModule> CompanyModules { get; set; }
    public DbSet<UserCompany> UserCompanies { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TenantModule — chave composta
        modelBuilder.Entity<TenantModule>()
            .HasKey(tm => new { tm.TenantId, tm.ModuleId });

        // CompanyModule — chave composta
        modelBuilder.Entity<CompanyModule>()
            .HasKey(cm => new { cm.CompanyId, cm.ModuleId });

        // UserCompany — chave composta
        modelBuilder.Entity<UserCompany>()
            .HasKey(uc => new { uc.UserId, uc.CompanyId });

        // RolePermission — chave composta
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // UserRole — chave composta
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.CompanyId, ur.RoleId });

        // Tenant subdomain único
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Subdomain)
            .IsUnique();

        // Module slug único
        modelBuilder.Entity<Module>()
            .HasIndex(m => m.Slug)
            .IsUnique();

        // Permission slug único por módulo
        modelBuilder.Entity<Permission>()
            .HasIndex(p => new { p.ModuleId, p.Slug })
            .IsUnique();
    }
}
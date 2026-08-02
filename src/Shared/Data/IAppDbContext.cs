using Microsoft.EntityFrameworkCore;
using SiskyApi.Shared.Models;

namespace SiskyApi.Shared.Data;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<Company> Companies { get; }
    DbSet<Module> Modules { get; }
    DbSet<TenantModule> TenantModules { get; }
    DbSet<CompanyModule> CompanyModules { get; }
    DbSet<UserCompany> UserCompanies { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketMessage> TicketMessages { get; }
    DbSet<Setting> Settings { get; }
    DbSet<IoTDevice> IoTDevices { get; }
    DbSet<IoTReading> IoTReadings { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
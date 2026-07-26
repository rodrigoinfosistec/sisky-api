using Hangfire;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Shared.Data;
using SiskyApi.Shared.Models;
using SiskyApi.Shared.Common;
using SiskyApi.Shared.Constants;
using SiskyApi.Shared;
using SiskyApi.Modules.Admin.DTOs;
using SiskyApi.Modules.Audit.DTOs;
using SiskyApi.Modules.Tickets.DTOs;

namespace SiskyApi.Modules.Admin;

public class AdminService
{
    private readonly AppDbContext _context;
    private readonly SettingsService _settingsService;

    public AdminService(AppDbContext context, SettingsService settingsService)
    {
        _context = context;
        _settingsService = settingsService;
    }

    public async Task<object> GetDashboard()
    {
        var totalTenants = await _context.Tenants.CountAsync();
        var activeTenants = await _context.Tenants.CountAsync(t => t.Active);
        var totalUsers = await _context.Users.CountAsync();
        var newTenantsThisMonth = await _context.Tenants
            .CountAsync(t => t.CreatedAt >= DateTime.UtcNow.AddMonths(-1));

        return new { totalTenants, activeTenants, totalUsers, newTenantsThisMonth };
    }

    public async Task<PaginatedResponseDto<TenantResponseDto>> GetTenants(int page, int perPage, string? search)
    {
        var query = _context.Tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.ToLower().Contains(search.ToLower()) ||
                                     t.Subdomain.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var tenants = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(t => new TenantResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Active = t.Active,
                CreatedAt = t.CreatedAt,
                UserCount = _context.Users.Count(u => u.TenantId == t.Id),
                CompanyCount = _context.Companies.Count(c => c.TenantId == t.Id)
            })
            .ToListAsync();

        return new PaginatedResponseDto<TenantResponseDto>
        {
            Data = tenants,
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        };
    }

    public async Task<TenantDetailsDto?> GetTenant(int id)
    {
        return await _context.Tenants
            .Where(t => t.Id == id)
            .Select(t => new TenantDetailsDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Active = t.Active,
                CreatedAt = t.CreatedAt,
                UserCount = _context.Users.Count(u => u.TenantId == t.Id),
                Companies = _context.Companies
                    .Where(c => c.TenantId == t.Id)
                    .Select(c => new TenantDetailsCompanyDto { Id = c.Id, Name = c.Name, Active = c.Active })
                    .ToList(),
                Modules = _context.TenantModules
                    .Where(tm => tm.TenantId == t.Id)
                    .Select(tm => new TenantDetailsModuleDto
                    {
                        Id = tm.Module.Id,
                        Name = tm.Module.Name,
                        Slug = tm.Module.Slug,
                        IsCore = tm.Module.IsCore,
                        Active = tm.Active
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TenantResponseDto> Create(TenantCreateDto dto)
    {
        var tenant = new Tenant
        {
            Name = dto.Name,
            Subdomain = dto.Subdomain.ToLower(),
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var modules = await _context.Modules.Where(m => m.Active).ToListAsync();
        foreach (var module in modules)
        {
            _context.TenantModules.Add(new TenantModule
            {
                TenantId = tenant.Id,
                ModuleId = module.Id,
                Active = true
            });
        }
        await _context.SaveChangesAsync();

        return new TenantResponseDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Active = tenant.Active,
            CreatedAt = tenant.CreatedAt,
            UserCount = 0,
            CompanyCount = 0
        };
    }

    public async Task<TenantResponseDto?> Update(int id, TenantUpdateDto dto)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant is null) return null;

        tenant.Name = dto.Name;
        await _context.SaveChangesAsync();

        return new TenantResponseDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Active = tenant.Active,
            CreatedAt = tenant.CreatedAt,
            UserCount = await _context.Users.CountAsync(u => u.TenantId == tenant.Id),
            CompanyCount = await _context.Companies.CountAsync(c => c.TenantId == tenant.Id)
        };
    }

    public async Task<(bool Success, string? Error)> Delete(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant is null) return (false, "Tenant não encontrado.");

        var hasUsers = await _context.Users.AnyAsync(u => u.TenantId == id);
        if (hasUsers)
            return (false, "Este tenant possui usuários associados. Remova-os antes de excluir.");

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, bool? Active)> ToggleActive(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant is null) return (false, null);

        tenant.Active = !tenant.Active;
        await _context.SaveChangesAsync();

        return (true, tenant.Active);
    }

    public async Task<PaginatedResponseDto<AuditLogResponseDto>> GetAuditLogs(
        int page, int perPage, int? tenantId, string? search,
        string? action, string? entity, DateTime? from, DateTime? to)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(a => a.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.UserName.ToLower().Contains(search.ToLower()));
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.Entity == entity);
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value.ToUniversalTime().AddDays(1));

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(a => new AuditLogResponseDto
            {
                Id = a.Id,
                TenantId = a.TenantId,
                CompanyId = a.CompanyId,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                Entity = a.Entity,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<AuditLogResponseDto>
        {
            Data = logs,
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        };
    }

    public async Task<PaginatedResponseDto<TicketResponseDto>> GetTickets(
        int page, int perPage, int? tenantId,
        string? status, string? priority, string? search)
    {
        var query = _context.Tickets.AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.ToLower().Contains(search.ToLower()) ||
                                     t.UserName.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var tickets = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(t => new TicketResponseDto
            {
                Id = t.Id,
                TenantId = t.TenantId,
                TenantName = t.TenantName,
                CompanyId = t.CompanyId,
                CompanyName = t.CompanyName,
                UserId = t.UserId,
                UserName = t.UserName,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                MessageCount = t.Messages.Count,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<TicketResponseDto>
        {
            Data = tickets,
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        };
    }

    public async Task<TicketDetailsDto?> GetTicket(int id)
    {
        return await _context.Tickets
            .Where(t => t.Id == id)
            .Select(t => new TicketDetailsDto
            {
                Id = t.Id,
                TenantId = t.TenantId,
                TenantName = t.TenantName,
                CompanyId = t.CompanyId,
                CompanyName = t.CompanyName,
                UserId = t.UserId,
                UserName = t.UserName,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Messages = t.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new TicketMessageDto
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        UserName = m.UserName,
                        Message = m.Message,
                        IsAdminReply = m.IsAdminReply,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TicketMessageDto?> AddAdminMessage(int ticketId, TicketMessageCreateDto dto, int adminUserId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Tenant)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket is null) return null;

        var admin = await _context.Users.FindAsync(adminUserId);

        var message = new TicketMessage
        {
            TicketId = ticketId,
            UserId = adminUserId,
            UserName = admin!.Name,
            Message = dto.Message,
            IsAdminReply = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketMessages.Add(message);

        ticket.UpdatedAt = DateTime.UtcNow;
        if (ticket.Status == TicketStatus.Open)
            ticket.Status = TicketStatus.InProgress;

        await _context.SaveChangesAsync();

        BackgroundJob.Enqueue<EmailService>(x =>
            x.SendTicketReplyToTenantAsync(
                ticket.User.Email, ticket.UserName,
                ticket.Id, ticket.Title,
                dto.Message, ticket.Tenant.Subdomain));

        return new TicketMessageDto
        {
            Id = message.Id,
            UserId = message.UserId,
            UserName = message.UserName,
            Message = message.Message,
            IsAdminReply = message.IsAdminReply,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<(bool Success, string? Error)> UpdateTicketStatus(int ticketId, string status)
    {
        if (!TicketStatus.All.Contains(status))
            return (false, "Status inválido.");

        var ticket = await _context.Tickets
            .Include(t => t.Tenant)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket is null) return (false, "Ticket não encontrado.");

        var oldStatus = ticket.Status;
        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        BackgroundJob.Enqueue<EmailService>(x =>
            x.SendTicketStatusChangedAsync(
                ticket.User.Email, ticket.UserName,
                ticket.Id, ticket.Title,
                oldStatus, status,
                ticket.Tenant.Subdomain));

        return (true, null);
    }

    public async Task<Dictionary<string, string>> GetSettings()
    {
        return await _settingsService.GetAll();
    }

    public async Task UpdateSettings(Dictionary<string, string> values)
    {
        await _settingsService.SetMany(values);
    }

    public async Task<(bool Success, string? Error, bool? Active)> ToggleTenantModule(int tenantId, int moduleId)
    {
        var module = await _context.Modules.FindAsync(moduleId);
        if (module is null) return (false, "Módulo não encontrado.", null);

        if (module.IsCore)
            return (false, "Módulos core não podem ser desativados.", null);

        var tenantModule = await _context.TenantModules
            .FirstOrDefaultAsync(tm => tm.TenantId == tenantId && tm.ModuleId == moduleId);

        if (tenantModule is null) return (false, "Módulo não associado ao tenant.", null);

        tenantModule.Active = !tenantModule.Active;
        await _context.SaveChangesAsync();

        return (true, null, tenantModule.Active);
    }

    public async Task<List<CompanyResponseDto>> GetCompanies(int tenantId)
    {
        return await _context.Companies
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyResponseDto
            {
                Id = c.Id,
                TenantId = c.TenantId,
                Name = c.Name,
                PrimaryColor = c.PrimaryColor,
                Active = c.Active,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CompanyResponseDto> CreateCompany(int tenantId, CompanyCreateDto dto)
    {
        var company = new Company
        {
            TenantId = tenantId,
            Name = dto.Name,
            PrimaryColor = dto.PrimaryColor ?? "#111111",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        // Associa os módulos ativos do tenant à nova empresa
        var modules = await _context.TenantModules
            .Where(tm => tm.TenantId == tenantId && tm.Active)
            .Select(tm => tm.ModuleId)
            .ToListAsync();

        foreach (var moduleId in modules)
        {
            _context.CompanyModules.Add(new CompanyModule
            {
                CompanyId = company.Id,
                ModuleId = moduleId,
                Active = true
            });
        }
        await _context.SaveChangesAsync();

        // Associa automaticamente todos os Super Admins do tenant à nova empresa
        var superAdminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == "Super Admin");

        if (superAdminRole != null)
        {
            var superAdminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == superAdminRole.Id)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in superAdminUserIds)
            {
                var alreadyInCompany = await _context.UserCompanies
                    .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == company.Id);

                if (!alreadyInCompany)
                {
                    _context.UserCompanies.Add(new UserCompany
                    {
                        UserId = userId,
                        CompanyId = company.Id,
                        IsDefault = false
                    });
                }

                var alreadyHasRole = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == userId &&
                                    ur.CompanyId == company.Id &&
                                    ur.RoleId == superAdminRole.Id);

                if (!alreadyHasRole)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = userId,
                        CompanyId = company.Id,
                        RoleId = superAdminRole.Id
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        return new CompanyResponseDto
        {
            Id = company.Id,
            TenantId = company.TenantId,
            Name = company.Name,
            PrimaryColor = company.PrimaryColor,
            Active = company.Active,
            CreatedAt = company.CreatedAt
        };
    }
    public async Task<CompanyResponseDto?> UpdateCompany(int tenantId, int companyId, CompanyUpdateDto dto)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && c.TenantId == tenantId);
        if (company is null) return null;

        company.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.PrimaryColor))
            company.PrimaryColor = dto.PrimaryColor;

        await _context.SaveChangesAsync();

        return new CompanyResponseDto
        {
            Id = company.Id,
            TenantId = company.TenantId,
            Name = company.Name,
            PrimaryColor = company.PrimaryColor,
            Active = company.Active,
            CreatedAt = company.CreatedAt
        };
    }

    public async Task<(bool Success, string? Error)> DeleteCompany(int tenantId, int companyId)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && c.TenantId == tenantId);
        if (company is null) return (false, "Empresa não encontrada.");

        var hasUsers = await _context.UserCompanies.AnyAsync(uc => uc.CompanyId == companyId);
        if (hasUsers)
            return (false, "Esta empresa possui usuários associados. Desative-a em vez de excluir.");

        var hasAuditLogs = await _context.AuditLogs.AnyAsync(a => a.CompanyId == companyId);
        if (hasAuditLogs)
            return (false, "Esta empresa possui logs de auditoria. Desative-a em vez de excluir.");

        var hasTickets = await _context.Tickets.AnyAsync(t => t.CompanyId == companyId);
        if (hasTickets)
            return (false, "Esta empresa possui tickets. Desative-a em vez de excluir.");

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, bool? Active)> ToggleCompanyActive(int tenantId, int companyId)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && c.TenantId == tenantId);
        if (company is null) return (false, null);

        company.Active = !company.Active;
        await _context.SaveChangesAsync();

        return (true, company.Active);
    }
}
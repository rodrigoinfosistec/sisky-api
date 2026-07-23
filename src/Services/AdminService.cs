using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.DTOs;
using SiskyApi.Models;
using SiskyApi.Constants;

namespace SiskyApi.Services;

public class AdminService
{
    private readonly AppDbContext _context;

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetDashboard()
    {
        var totalTenants = await _context.Tenants.CountAsync();
        var activeTenants = await _context.Tenants.CountAsync(t => t.Active);
        var totalUsers = await _context.Users.CountAsync();
        var newTenantsThisMonth = await _context.Tenants
            .CountAsync(t => t.CreatedAt >= DateTime.UtcNow.AddMonths(-1));

        return new
        {
            totalTenants,
            activeTenants,
            totalUsers,
            newTenantsThisMonth
        };
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
                    .Select(c => new TenantDetailsCompanyDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Active = c.Active
                    })
                    .ToList(),
                Modules = _context.TenantModules
                    .Where(tm => tm.TenantId == t.Id)
                    .Select(tm => new TenantDetailsModuleDto
                    {
                        Id = tm.Module.Id,
                        Name = tm.Module.Name,
                        Slug = tm.Module.Slug,
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
        int page,
        int perPage,
        int? tenantId,
        string? search,
        string? action,
        string? entity,
        DateTime? from,
        DateTime? to)
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
    int page,
    int perPage,
    int? tenantId,
    string? status,
    string? priority,
    string? search)
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
        var ticket = await _context.Tickets.FindAsync(ticketId);
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

        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket is null) return (false, "Ticket não encontrado.");

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
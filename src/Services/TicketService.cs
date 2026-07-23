using Microsoft.EntityFrameworkCore;
using SiskyApi.Constants;
using SiskyApi.Data;
using SiskyApi.DTOs;
using SiskyApi.Models;

namespace SiskyApi.Services;

public class TicketService
{
    private readonly AppDbContext _context;
    private readonly TenantContext _tenantContext;

    public TicketService(AppDbContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PaginatedResponseDto<TicketResponseDto>> GetAll(
        int page,
        int perPage,
        string? status,
        string? priority,
        string? search)
    {
        var query = _context.Tickets
            .Where(t => t.TenantId == _tenantContext.TenantId &&
                        t.CompanyId == _tenantContext.CompanyId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
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

    public async Task<TicketDetailsDto?> GetById(int id)
    {
        return await _context.Tickets
            .Where(t => t.Id == id &&
                        t.TenantId == _tenantContext.TenantId &&
                        t.CompanyId == _tenantContext.CompanyId)
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

    public async Task<TicketResponseDto> Create(TicketCreateDto dto, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        var company = await _context.Companies.FindAsync(_tenantContext.CompanyId);
        var tenant = await _context.Tenants.FindAsync(_tenantContext.TenantId);

        var ticket = new Ticket
        {
            TenantId = _tenantContext.TenantId!.Value,
            TenantName = tenant!.Name,
            CompanyId = _tenantContext.CompanyId!.Value,
            CompanyName = company!.Name,
            UserId = userId,
            UserName = user!.Name,
            Title = dto.Title,
            Description = dto.Description,
            Status = TicketStatus.Open,
            Priority = dto.Priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        return new TicketResponseDto
        {
            Id = ticket.Id,
            TenantId = ticket.TenantId,
            TenantName = ticket.TenantName,
            CompanyId = ticket.CompanyId,
            CompanyName = ticket.CompanyName,
            UserId = ticket.UserId,
            UserName = ticket.UserName,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            MessageCount = 0,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };
    }

    public async Task<TicketMessageDto?> AddMessage(int ticketId, TicketMessageCreateDto dto, int userId, bool isAdminReply = false)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId &&
                                      t.TenantId == _tenantContext.TenantId &&
                                      t.CompanyId == _tenantContext.CompanyId);

        if (ticket is null) return null;

        var user = await _context.Users.FindAsync(userId);

        var message = new TicketMessage
        {
            TicketId = ticketId,
            UserId = userId,
            UserName = user!.Name,
            Message = dto.Message,
            IsAdminReply = isAdminReply,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketMessages.Add(message);

        ticket.UpdatedAt = DateTime.UtcNow;
        if (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
            ticket.Status = TicketStatus.Open;

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

    public async Task<(bool Success, string? Error)> UpdateStatus(int ticketId, string status)
    {
        if (!TicketStatus.All.Contains(status))
            return (false, "Status inválido.");

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId &&
                                      t.TenantId == _tenantContext.TenantId);

        if (ticket is null) return (false, "Ticket não encontrado.");

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
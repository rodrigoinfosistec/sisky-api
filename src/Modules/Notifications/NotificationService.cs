using Microsoft.EntityFrameworkCore;
using SiskyApi.Modules.Notifications.DTOs;
using SiskyApi.Shared.Data;
using SiskyApi.Shared.Models;

namespace SiskyApi.Modules.Notifications;

public class NotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationResponseDto>> GetByUser(int userId, int tenantId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Link = n.Link,
                Read = n.Read,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<int> GetUnreadCount(int userId, int tenantId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && n.TenantId == tenantId && !n.Read);
    }

    public async Task<bool> MarkAsRead(int id, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null) return false;

        notification.Read = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsUnread(int id, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null) return false;

        notification.Read = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsRead(int userId, int tenantId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.TenantId == tenantId && !n.Read)
            .ToListAsync();

        foreach (var n in notifications)
            n.Read = true;

        await _context.SaveChangesAsync();
    }

    public async Task Create(int userId, int tenantId, string title, string message, string? link = null)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            TenantId = tenantId,
            Title = title,
            Message = message,
            Link = link,
            Read = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}
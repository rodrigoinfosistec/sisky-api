using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.Shared;

namespace SiskyApi.Modules.Notifications;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly NotificationService _notificationService;
    private readonly TenantContext _tenantContext;

    public NotificationController(NotificationService notificationService, TenantContext tenantContext)
    {
        _notificationService = notificationService;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null) return BadRequest("Tenant não identificado.");

        var notifications = await _notificationService.GetByUser(userId, tenantId.Value);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null) return BadRequest("Tenant não identificado.");

        var count = await _notificationService.GetUnreadCount(userId, tenantId.Value);
        return Ok(new { count });
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _notificationService.MarkAsRead(id, userId);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpPatch("{id}/unread")]
    public async Task<IActionResult> MarkAsUnread(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _notificationService.MarkAsUnread(id, userId);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null) return BadRequest("Tenant não identificado.");

        await _notificationService.MarkAllAsRead(userId, tenantId.Value);
        return Ok();
    }
}
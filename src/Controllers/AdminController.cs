using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using SiskyApi.Authorization;
using SiskyApi.DTOs;
using SiskyApi.Services;
using System.Security.Claims;

namespace SiskyApi.Controllers;

[RequireSuperAdmin]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly IValidator<TenantCreateDto> _createValidator;
    private readonly IValidator<TenantUpdateDto> _updateValidator;

    public AdminController(
        AdminService adminService,
        IValidator<TenantCreateDto> createValidator,
        IValidator<TenantUpdateDto> updateValidator)
    {
        _adminService = adminService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var metrics = await _adminService.GetDashboard();
        return Ok(metrics);
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 15,
        [FromQuery] string? search = null)
    {
        var result = await _adminService.GetTenants(page, perPage, search);
        return Ok(result);
    }

    [HttpGet("tenants/{id}")]
    public async Task<IActionResult> GetTenant(int id)
    {
        var tenant = await _adminService.GetTenant(id);
        if (tenant is null) return NotFound();
        return Ok(tenant);
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] TenantCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var tenant = await _adminService.Create(dto);
        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
    }

    [HttpPut("tenants/{id}")]
    public async Task<IActionResult> UpdateTenant(int id, [FromBody] TenantUpdateDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var tenant = await _adminService.Update(id, dto);
        if (tenant is null) return NotFound();
        return Ok(tenant);
    }

    [HttpDelete("tenants/{id}")]
    public async Task<IActionResult> DeleteTenant(int id)
    {
        var (success, error) = await _adminService.Delete(id);
        if (!success) return BadRequest(error);
        return NoContent();
    }

    [HttpPatch("tenants/{id}/toggle-active")]
    public async Task<IActionResult> ToggleTenantActive(int id)
    {
        var (success, active) = await _adminService.ToggleActive(id);
        if (!success) return NotFound();
        return Ok(new { id, active });
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs(
    [FromQuery] int page = 1,
    [FromQuery] int perPage = 20,
    [FromQuery] int? tenantId = null,
    [FromQuery] string? search = null,
    [FromQuery] string? action = null,
    [FromQuery] string? entity = null,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null)
    {
        var result = await _adminService.GetAuditLogs(page, perPage, tenantId, search, action, entity, from, to);
        return Ok(result);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(
    [FromQuery] int page = 1,
    [FromQuery] int perPage = 15,
    [FromQuery] int? tenantId = null,
    [FromQuery] string? status = null,
    [FromQuery] string? priority = null,
    [FromQuery] string? search = null)
    {
        var result = await _adminService.GetTickets(page, perPage, tenantId, status, priority, search);
        return Ok(result);
    }

    [HttpGet("tickets/{id}")]
    public async Task<IActionResult> GetTicket(int id)
    {
        var ticket = await _adminService.GetTicket(id);
        if (ticket is null) return NotFound();
        return Ok(ticket);
    }

    [HttpPost("tickets/{id}/messages")]
    public async Task<IActionResult> AddAdminMessage(int id, [FromBody] TicketMessageCreateDto dto)
    {
        var adminUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var message = await _adminService.AddAdminMessage(id, dto, adminUserId);
        if (message is null) return NotFound();
        return Ok(message);
    }

    [HttpPatch("tickets/{id}/status")]
    public async Task<IActionResult> UpdateTicketStatus(int id, [FromBody] string status)
    {
        var (success, error) = await _adminService.UpdateTicketStatus(id, status);
        if (!success) return BadRequest(error);
        return Ok();
    }
}
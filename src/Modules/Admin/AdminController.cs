using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using SiskyApi.Shared.Authorization;
using SiskyApi.Modules.Admin.DTOs;
using SiskyApi.Modules.Tickets.DTOs;
using SiskyApi.Modules.IoT.DTOs;

namespace SiskyApi.Modules.Admin;

[RequireSuperAdmin]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly IValidator<TenantCreateDto> _createValidator;
    private readonly IValidator<TenantUpdateDto> _updateValidator;
    private readonly IValidator<CompanyCreateDto> _companyCreateValidator;
    private readonly IValidator<CompanyUpdateDto> _companyUpdateValidator;
    private readonly IValidator<IoTDeviceCreateDto> _deviceCreateValidator;

    public AdminController(
    AdminService adminService,
    IValidator<TenantCreateDto> createValidator,
    IValidator<TenantUpdateDto> updateValidator,
    IValidator<CompanyCreateDto> companyCreateValidator,
    IValidator<CompanyUpdateDto> companyUpdateValidator,
    IValidator<IoTDeviceCreateDto> deviceCreateValidator)
    {
        _adminService = adminService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _companyCreateValidator = companyCreateValidator;
        _companyUpdateValidator = companyUpdateValidator;
        _deviceCreateValidator = deviceCreateValidator;
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

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _adminService.GetSettings();
        return Ok(settings);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, string> values)
    {
        await _adminService.UpdateSettings(values);
        return Ok();
    }

    [HttpPatch("tenants/{tenantId}/modules/{moduleId}/toggle")]
    public async Task<IActionResult> ToggleTenantModule(int tenantId, int moduleId)
    {
        var (success, error, active) = await _adminService.ToggleTenantModule(tenantId, moduleId);
        if (!success) return BadRequest(error);
        return Ok(new { tenantId, moduleId, active });
    }

    [HttpGet("tenants/{tenantId}/companies")]
    public async Task<IActionResult> GetCompanies(int tenantId)
    {
        var companies = await _adminService.GetCompanies(tenantId);
        return Ok(companies);
    }

    [HttpPost("tenants/{tenantId}/companies")]
    public async Task<IActionResult> CreateCompany(int tenantId, [FromBody] CompanyCreateDto dto)
    {
        var validation = await _companyCreateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var company = await _adminService.CreateCompany(tenantId, dto);
        return Created($"/api/admin/tenants/{tenantId}/companies/{company.Id}", company);
    }

    [HttpPut("tenants/{tenantId}/companies/{companyId}")]
    public async Task<IActionResult> UpdateCompany(int tenantId, int companyId, [FromBody] CompanyUpdateDto dto)
    {
        var validation = await _companyUpdateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var company = await _adminService.UpdateCompany(tenantId, companyId, dto);
        if (company is null) return NotFound();
        return Ok(company);
    }

    [HttpDelete("tenants/{tenantId}/companies/{companyId}")]
    public async Task<IActionResult> DeleteCompany(int tenantId, int companyId)
    {
        var (success, error) = await _adminService.DeleteCompany(tenantId, companyId);
        if (!success) return BadRequest(error);
        return NoContent();
    }

    [HttpPatch("tenants/{tenantId}/companies/{companyId}/toggle-active")]
    public async Task<IActionResult> ToggleCompanyActive(int tenantId, int companyId)
    {
        var (success, active) = await _adminService.ToggleCompanyActive(tenantId, companyId);
        if (!success) return NotFound();
        return Ok(new { tenantId, companyId, active });
    }

    [HttpPost("settings/logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        var url = await _adminService.UploadLogo(file);
        if (url is null) return BadRequest("Erro ao fazer upload do logo.");
        return Ok(new { url });
    }

    [HttpPost("settings/favicon")]
    public async Task<IActionResult> UploadFavicon(IFormFile file)
    {
        var url = await _adminService.UploadFavicon(file);
        if (url is null) return BadRequest("Erro ao fazer upload do favicon.");
        return Ok(new { url });
    }

    // IoT Devices
    [HttpGet("tenants/{tenantId}/devices")]
    public async Task<IActionResult> GetIoTDevices(int tenantId)
    {
        var devices = await _adminService.GetIoTDevices(tenantId);
        return Ok(devices);
    }

    [HttpPost("tenants/{tenantId}/devices")]
    public async Task<IActionResult> CreateIoTDevice(int tenantId, [FromBody] IoTDeviceCreateDto dto)
    {
        var validation = await _deviceCreateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var device = await _adminService.CreateIoTDevice(tenantId, dto);
        return Created($"/api/admin/tenants/{tenantId}/devices/{device.Id}", device);
    }

    [HttpPatch("tenants/{tenantId}/devices/{deviceId}/toggle")]
    public async Task<IActionResult> ToggleIoTDevice(int tenantId, int deviceId)
    {
        var (success, active) = await _adminService.ToggleIoTDevice(tenantId, deviceId);
        if (!success) return NotFound();
        return Ok(new { tenantId, deviceId, active });
    }

    [HttpDelete("tenants/{tenantId}/devices/{deviceId}")]
    public async Task<IActionResult> DeleteIoTDevice(int tenantId, int deviceId)
    {
        var (success, error) = await _adminService.DeleteIoTDevice(tenantId, deviceId);
        if (!success) return BadRequest(error);
        return NoContent();
    }

    [HttpPost("tenants/{tenantId}/devices/{deviceId}/seed")]
    public async Task<IActionResult> SeedIoTReadings(int tenantId, int deviceId)
    {
        await _adminService.SeedIoTReadings(tenantId, deviceId);
        return Ok(new { message = "Dados mockados gerados com sucesso." });
    }

    [HttpDelete("tenants/{tenantId}/devices/{deviceId}/readings")]
    public async Task<IActionResult> ClearIoTReadings(int tenantId, int deviceId)
    {
        await _adminService.ClearIoTReadings(tenantId, deviceId);
        return Ok(new { message = "Leituras removidas com sucesso." });
    }
}
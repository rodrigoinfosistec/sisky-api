using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.Modules.IoT.DTOs;
using SiskyApi.Shared;
using SiskyApi.Shared.Authorization;

namespace SiskyApi.Modules.IoT;

[ApiController]
[Route("api/iot")]
public class IoTController : ControllerBase
{
    private readonly IoTService _iotService;
    private readonly TenantContext _tenantContext;

    public IoTController(IoTService iotService, TenantContext tenantContext)
    {
        _iotService = iotService;
        _tenantContext = tenantContext;
    }

    // Leituras — tenant visualiza seus dados
    [Authorize]
    [RequirePermission("iot.view")]
    [HttpGet("readings")]
    public async Task<IActionResult> GetReadings(
    [FromQuery] int? tenantId = null,
    [FromQuery] int? deviceId = null,
    [FromQuery] string? type = null,
    [FromQuery] int hours = 24)
    {
        // Super Admin passa tenantId como query param
        // Usuário comum usa o TenantId do contexto (middleware ou JWT)
        var resolvedTenantId = tenantId
            ?? _tenantContext.TenantId
            ?? HttpContext.Items["TenantId"] as int?;

        if (resolvedTenantId is null)
            return BadRequest("Tenant não identificado.");

        var readings = await _iotService.GetReadings(resolvedTenantId.Value, deviceId, type, hours);
        return Ok(readings);
    }

    // Ingestão de dados — autenticação por API Key (ESP32)
    [HttpPost("readings")]
    public async Task<IActionResult> CreateReading([FromBody] IoTReadingCreateDto dto)
    {
        var apiKey = Request.Headers["X-Api-Key"].ToString();

        if (string.IsNullOrEmpty(apiKey))
            return Unauthorized("API Key não encontrada no header.");

        var device = await _iotService.ValidateApiKey(apiKey);

        if (device is null)
            return Unauthorized($"API Key inválida. Key recebida: {apiKey[..10]}...");

        var reading = await _iotService.CreateReading(device.Id, device.TenantId, dto);
        if (reading is null)
            return BadRequest("Erro ao registrar leitura.");

        return Created("", reading);
    }
}
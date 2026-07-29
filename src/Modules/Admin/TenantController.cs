using Microsoft.AspNetCore.Mvc;
using SiskyApi.Shared;

namespace SiskyApi.Modules.Admin;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly SettingsService _settingsService;

    public TenantController(IConfiguration configuration, SettingsService settingsService)
    {
        _configuration = configuration;
        _settingsService = settingsService;
    }

    [HttpGet("resolve")]
    public IActionResult Resolve()
    {
        var tenantId = HttpContext.Items["TenantId"];
        var tenantName = HttpContext.Items["TenantName"];
        var frontendUrl = _configuration["App:FrontendUrl"];

        if (tenantId is null)
            return NotFound(new
            {
                error = "Tenant não encontrado ou inativo.",
                redirectTo = frontendUrl
            });

        return Ok(new { tenantId, tenantName });
    }

    [HttpGet("config")]
    public async Task<IActionResult> Config()
    {
        var settings = await _settingsService.GetAll();

        return Ok(new
        {
            systemName = settings.GetValueOrDefault("system_name", "Sisky"),
            logoUrl = settings.GetValueOrDefault("logo_url", ""),
            faviconUrl = settings.GetValueOrDefault("favicon_url", ""),
        });
    }
}
using Microsoft.AspNetCore.Mvc;

namespace SiskyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public TenantController(IConfiguration configuration)
    {
        _configuration = configuration;
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
}
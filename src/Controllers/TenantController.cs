using Microsoft.AspNetCore.Mvc;

namespace SiskyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    [HttpGet("resolve")]
    public IActionResult Resolve()
    {
        var tenantId = HttpContext.Items["TenantId"];
        var tenantName = HttpContext.Items["TenantName"];

        if (tenantId is null)
            return NotFound(new
            {
                error = "Tenant não encontrado ou inativo.",
                redirectTo = "https://sisky.com.br"
            });

        return Ok(new
        {
            tenantId,
            tenantName
        });
    }
}
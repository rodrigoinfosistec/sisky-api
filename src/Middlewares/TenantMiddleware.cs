using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;

namespace SiskyApi.Middlewares;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _frontendUrl;

    public TenantMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _frontendUrl = configuration["App:FrontendUrl"]!;
    }

    public async Task Invoke(HttpContext context, AppDbContext db)
    {
        var subdomain = context.Request.Headers["X-Tenant-Subdomain"].ToString();

        if (!string.IsNullOrEmpty(subdomain))
        {
            var tenant = await db.Tenants
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.Active);

            if (tenant is null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Tenant não encontrado ou inativo.",
                    redirectTo = _frontendUrl
                });
                return;
            }

            context.Items["TenantId"] = tenant.Id;
            context.Items["TenantName"] = tenant.Name;
        }

        await _next(context);
    }
}
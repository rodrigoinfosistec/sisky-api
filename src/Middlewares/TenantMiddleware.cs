using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;

namespace SiskyApi.Middlewares;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
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
                    redirectTo = "https://sisky.com.br"
                });
                return;
            }

            context.Items["TenantId"] = tenant.Id;
            context.Items["TenantName"] = tenant.Name;
        }

        await _next(context);
    }
}
using Microsoft.EntityFrameworkCore;
using SiskyApi.Shared.Data;

namespace SiskyApi.Shared.Middlewares;

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
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;
        var tenantId = context.Items["TenantId"] as int?;

        // POST /api/iot/readings usa API Key — não precisa de tenant no contexto
        if (path == "/api/iot/readings" && method == "POST")
        {
            await _next(context);
            return;
        }

        if (tenantId.HasValue)
        {
            // resto do código...
        }

        await _next(context);
    }
}
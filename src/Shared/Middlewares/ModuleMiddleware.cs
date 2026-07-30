using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SiskyApi.Shared.Data;

namespace SiskyApi.Shared.Middlewares;

public class ModuleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    private static readonly Dictionary<string, string> RouteModuleMap = new()
    {
        { "/api/user", "users" },
        { "/api/role", "users" },
        { "/api/audit", "audit" },
        { "/api/financeiro", "financeiro" },
        { "/api/rh", "rh" },
        { "/api/crm", "crm" },
        { "/api/iot", "iot" },
    };

    public ModuleMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task Invoke(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var tenantId = context.Items["TenantId"] as int?;

        if (tenantId.HasValue)
        {
            var matchedModule = RouteModuleMap
                .FirstOrDefault(kvp => path.StartsWith(kvp.Key));

            if (matchedModule.Key != null)
            {
                var slug = matchedModule.Value;
                var cacheKey = $"module:{tenantId}:{slug}";

                if (!_cache.TryGetValue(cacheKey, out bool isActive))
                {
                    isActive = await db.TenantModules
                        .Include(tm => tm.Module)
                        .AnyAsync(tm => tm.TenantId == tenantId &&
                                        tm.Module.Slug == slug &&
                                        tm.Active);

                    _cache.Set(cacheKey, isActive, TimeSpan.FromMinutes(5));
                }

                if (!isActive)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Módulo não disponível para este tenant."
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
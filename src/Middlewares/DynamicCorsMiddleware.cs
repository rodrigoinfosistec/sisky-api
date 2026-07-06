using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SiskyApi.Data;

namespace SiskyApi.Middlewares;

public class DynamicCorsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly string _domain;

    public DynamicCorsMiddleware(RequestDelegate next, IMemoryCache cache, IConfiguration configuration)
    {
        _next = next;
        _cache = cache;
        _domain = configuration["App:Domain"]!;
    }

    public async Task Invoke(HttpContext context, AppDbContext db)
    {
        var origin = context.Request.Headers["Origin"].ToString();

        if (!string.IsNullOrEmpty(origin))
        {
            var allowed = await IsOriginAllowed(origin, db);

            if (allowed)
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                context.Response.Headers["Access-Control-Allow-Headers"] = "*";
                context.Response.Headers["Access-Control-Allow-Methods"] = "*";

                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    return;
                }
            }
        }

        await _next(context);
    }

    private async Task<bool> IsOriginAllowed(string origin, AppDbContext db)
    {
        try
        {
            var uri = new Uri(origin);

            if (uri.Host == "localhost") return true;
            if (uri.Host == $"www.{_domain}") return true;
            if (uri.Host == _domain) return true;

            if (uri.Host.EndsWith($".{_domain}"))
            {
                var subdomain = uri.Host.Replace($".{_domain}", "");
                var cacheKey = $"cors:{subdomain}";

                if (_cache.TryGetValue(cacheKey, out bool cached))
                    return cached;

                var allowed = await db.Tenants
                    .AnyAsync(t => t.Subdomain == subdomain && t.Active);

                _cache.Set(cacheKey, allowed, TimeSpan.FromMinutes(5));

                return allowed;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
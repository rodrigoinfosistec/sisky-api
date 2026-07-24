using System.Text;
using Hangfire.Dashboard;

namespace SiskyApi.Authorization;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _user;
    private readonly string _password;

    public HangfireAuthorizationFilter(IConfiguration configuration)
    {
        _user = configuration["Hangfire:DashboardUser"]!;
        _password = configuration["Hangfire:DashboardPassword"]!;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (authHeader.StartsWith("Basic "))
        {
            var credentials = Encoding.UTF8.GetString(
                Convert.FromBase64String(authHeader["Basic ".Length..]));
            var parts = credentials.Split(':', 2);
            if (parts.Length == 2 && parts[0] == _user && parts[1] == _password)
                return true;
        }

        httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = 401;
        return false;
    }
}
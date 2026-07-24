using Hangfire.Dashboard;

namespace SiskyApi.Authorization;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var isSuperAdmin = httpContext.User.FindFirst("is_super_admin")?.Value;
        return isSuperAdmin == "true";
    }
}
using Microsoft.AspNetCore.Authorization;

namespace SiskyApi.Authorization;

public class SuperAdminHandler : AuthorizationHandler<SuperAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SuperAdminRequirement requirement)
    {
        var isSuperAdmin = context.User.FindFirst("is_super_admin")?.Value;

        if (isSuperAdmin == "true")
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
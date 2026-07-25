using Microsoft.AspNetCore.Authorization;

namespace SiskyApi.Shared.Authorization;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = permission;
    }
}
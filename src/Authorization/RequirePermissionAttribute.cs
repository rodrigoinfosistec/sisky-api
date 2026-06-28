using Microsoft.AspNetCore.Authorization;

namespace SiskyApi.Authorization;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = permission;
    }
}
using Microsoft.AspNetCore.Authorization;

namespace SiskyApi.Authorization;

public class RequireSuperAdminAttribute : AuthorizeAttribute
{
    public const string PolicyName = "SuperAdmin";

    public RequireSuperAdminAttribute()
    {
        Policy = PolicyName;
    }
}
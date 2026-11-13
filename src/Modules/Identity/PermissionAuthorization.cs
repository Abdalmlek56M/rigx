using Microsoft.AspNetCore.Authorization;

namespace HardwareStore.Modules.Identity.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string ClaimType = "permission";

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (PermissionCatalog.All.Contains(requirement.Permission) &&
            context.User.Identities.Any(identity => identity.IsAuthenticated &&
                identity.Claims.Any(c => c.Type == ClaimType && string.Equals(c.Value, requirement.Permission, StringComparison.Ordinal))))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

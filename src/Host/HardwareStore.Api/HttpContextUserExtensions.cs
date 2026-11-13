using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;

/// <summary>Reads the signed-in user from the request. The single place endpoints get the caller's id and permissions.</summary>
internal static class HttpContextUserExtensions
{
    /// <summary>The signed-in account id; false when the request is anonymous or the id claim is missing/invalid.</summary>
    public static bool TryGetUserId(this HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId != Guid.Empty;

    /// <summary>True when the caller's session carries the permission (used to mask sensitive fields, not to authorize).</summary>
    public static bool HasPermission(this HttpContext context, string permission) =>
        context.User.Claims.Any(claim =>
            claim.Type == PermissionAuthorizationHandler.ClaimType &&
            string.Equals(claim.Value, permission, StringComparison.Ordinal));
}

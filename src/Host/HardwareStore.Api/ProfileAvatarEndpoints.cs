using HardwareStore.Common.Domain;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static class ProfileAvatarEndpoints
{
    public static IEndpointRouteBuilder MapProfileAvatarEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/profile/avatar", UploadAsync).RequireAuthorization();
        endpoints.MapDelete("/api/v1/identity/profile/avatar", DeleteAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<Ok<AvatarUploadResponse>> UploadAsync(
        HttpContext context,
        IdentityProfileService profiles,
        ICatalogMediaStorage media,
        CatalogMediaOptions options,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        var upload = await CatalogEndpoints.ReadSingleImageAsync(context, options, cancellationToken);
        var storageKey = await media.SaveAssetImageAsync("avatars", accountId, Guid.CreateVersion7(), upload, cancellationToken);
        await DeleteOldFileAsync(media, await profiles.ReplaceAvatarAsync(accountId, storageKey, cancellationToken));
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(new AvatarUploadResponse(storageKey));
    }

    private static async Task<NoContent> DeleteAsync(
        HttpContext context,
        IdentityProfileService profiles,
        ICatalogMediaStorage media,
        CancellationToken cancellationToken)
    {
        await DeleteOldFileAsync(media, await profiles.ReplaceAvatarAsync(context.RequireUserId(), null, cancellationToken));
        return TypedResults.NoContent();
    }

    /// <summary>The account already points at the new avatar (or none), so a file that cannot be deleted is left behind rather than failing the request.</summary>
    private static async Task DeleteOldFileAsync(ICatalogMediaStorage media, string? previousKey)
    {
        if (string.IsNullOrWhiteSpace(previousKey)) return;
        try
        {
            await media.DeleteStorageKeyAsync(previousKey, CancellationToken.None);
        }
        catch
        {
            // Best effort: the account is already consistent.
        }
    }
}

public sealed record AvatarUploadResponse(string AvatarStorageKey);

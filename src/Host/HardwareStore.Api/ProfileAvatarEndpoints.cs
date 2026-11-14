using HardwareStore.Common.Domain;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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
        IDbContextFactory<IdentityDbContext> identityFactory,
        ICatalogMediaStorage media,
        CatalogMediaOptions options,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        var upload = await CatalogEndpoints.ReadSingleImageAsync(context, options, cancellationToken);
        var storageKey = await media.SaveAssetImageAsync("avatars", accountId, Guid.CreateVersion7(), upload, cancellationToken);

        string? previousKey;
        await using (var db = await identityFactory.CreateDbContextAsync(cancellationToken))
        {
            var account = await db.UserAccounts.SingleOrDefaultAsync(
                item => item.Id == accountId && item.IsActive, cancellationToken);
            if (account is null)
                throw new UnauthenticatedException();
            previousKey = account.AvatarStorageKey;
            account.AvatarStorageKey = storageKey;
            await db.SaveChangesAsync(cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            try
            {
                await media.DeleteStorageKeyAsync(previousKey, CancellationToken.None);
            }
            catch
            {
                // Old avatar cleanup is best-effort; the new avatar is already stored.
            }
        }
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(new AvatarUploadResponse(storageKey));
    }

    private static async Task<NoContent> DeleteAsync(
        HttpContext context,
        IDbContextFactory<IdentityDbContext> identityFactory,
        ICatalogMediaStorage media,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        string? previousKey;
        await using (var db = await identityFactory.CreateDbContextAsync(cancellationToken))
        {
            var account = await db.UserAccounts.SingleOrDefaultAsync(
                item => item.Id == accountId && item.IsActive, cancellationToken);
            if (account is null)
                throw new UnauthenticatedException();
            previousKey = account.AvatarStorageKey;
            account.AvatarStorageKey = null;
            await db.SaveChangesAsync(cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            try
            {
                await media.DeleteStorageKeyAsync(previousKey, CancellationToken.None);
            }
            catch
            {
                // Keep the account consistent even if file cleanup fails.
            }
        }
        return TypedResults.NoContent();
    }
}

public sealed record AvatarUploadResponse(string AvatarStorageKey);

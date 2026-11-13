using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public static class ProfileAvatarEndpoints
{
    public static IEndpointRouteBuilder MapProfileAvatarEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/profile/avatar", UploadAsync).RequireAuthorization();
        endpoints.MapDelete("/api/v1/identity/profile/avatar", DeleteAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> UploadAsync(
        HttpContext context,
        IDbContextFactory<IdentityDbContext> identityFactory,
        ICatalogMediaStorage media,
        CatalogMediaOptions options,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        if (!context.Request.HasFormContentType)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["form"] = ["A multipart form is required."]
            });
        context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()!
            .MaxRequestBodySize = options.MaxUploadBytes + 64 * 1024;
        if (context.Request.ContentLength > options.MaxUploadBytes + 64 * 1024)
            return Results.Problem(statusCode: 413, title: "The uploaded image is too large.");
        try
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var image = form.Files.GetFile("image");
            if (image is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["image"] = ["An image file is required."]
                });
            var storageKey = await media.SaveAssetImageAsync(
                "avatars",
                accountId,
                Guid.CreateVersion7(),
                new CatalogImageUpload(image.OpenReadStream(), image.Length, image.FileName, image.ContentType),
                cancellationToken);

            string? previousKey;
            await using (var db = await identityFactory.CreateDbContextAsync(cancellationToken))
            {
                var account = await db.UserAccounts.SingleOrDefaultAsync(
                    item => item.Id == accountId && item.IsActive, cancellationToken);
                if (account is null)
                    return Results.Unauthorized();
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
            return Results.Ok(new { avatarStorageKey = storageKey });
        }
        catch (CatalogValidationException exception)
        {
            return Results.ValidationProblem(exception.Errors);
        }
    }

    private static async Task<IResult> DeleteAsync(
        HttpContext context,
        IDbContextFactory<IdentityDbContext> identityFactory,
        ICatalogMediaStorage media,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        string? previousKey;
        await using (var db = await identityFactory.CreateDbContextAsync(cancellationToken))
        {
            var account = await db.UserAccounts.SingleOrDefaultAsync(
                item => item.Id == accountId && item.IsActive, cancellationToken);
            if (account is null)
                return Results.Unauthorized();
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
        return Results.NoContent();
    }
}

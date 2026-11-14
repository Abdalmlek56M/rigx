using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static class StorefrontEndpoints
{
    public static IEndpointRouteBuilder MapStorefrontEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/storefront/settings", GetPublicSettingsAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/storefront/checkout-quote", QuoteCheckoutAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/storefront/shipping-rates", GetShippingRatesAsync).AllowAnonymous();

        var settings = endpoints.MapGroup("/api/v1/admin/storefront/settings");
        settings.MapGet("", GetAdminSettingsAsync).RequirePermission(SettingsPermission);
        MapSection<StoreInfoSettings>(settings);
        MapSection<ContactLinkSettings>(settings);
        MapSection<ShippingSettings>(settings);
        MapSection<PaymentSettings>(settings);
        MapSection<WatermarkSettings>(settings);
        MapSection<StoreControlSettings>(settings);
        MapSection<WhatsAppTemplateSettings>(settings);
        MapSection<ReviewSettings>(settings);
        MapSection<LanguageSettings>(settings);
        MapSection<InvoiceSettings>(settings);
        MapSection<LeadSourceSettings>(settings);
        MapSection<SalesSettings>(settings);
        MapSection<WarrantyOptionSettings>(settings);
        MapSection<BackupSettings>(settings);
        settings.MapPut("/logos/{kind}", SetLogoAsync).RequirePermission(SettingsPermission);
        settings.MapDelete("/logos/{kind}", RemoveLogoAsync).RequirePermission(SettingsPermission);
        settings.MapPost("/watermark/reapply", ReapplyWatermark).RequirePermission(SettingsPermission);

        var wishlist = endpoints.MapGroup("/api/v1/wishlist").RequireAuthorization();
        wishlist.MapGet("", ListWishlistAsync);
        wishlist.MapPost("/{productId:guid}", AddWishlistAsync);
        wishlist.MapDelete("/{productId:guid}", RemoveWishlistAsync);
        wishlist.MapDelete("", ClearWishlistAsync);
        wishlist.MapPost("/merge", MergeWishlistAsync);
        return endpoints;
    }

    private const string SettingsPermission = "System.StoreSettings";

    private static void MapSection<T>(RouteGroupBuilder group) where T : class, IStoreSettingsSection<T>
    {
        // Passed as a plain Delegate so the compile-time endpoint generators skip the open generic handler
        // (they cannot emit code for T); the runtime binder handles each closed section type.
        Delegate handler = UpdateSectionAsync<T>;
        group.MapPut("/" + T.Key, handler).RequirePermission(SettingsPermission);
    }

    private static async Task<IResult> ListWishlistAsync(
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await wishlist.ListAsync(accountId, cancellationToken));
    }

    private static async Task<IResult> AddWishlistAsync(
        Guid productId,
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await wishlist.AddAsync(accountId, productId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveWishlistAsync(
        Guid productId,
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await wishlist.RemoveAsync(accountId, productId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ClearWishlistAsync(
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await wishlist.ClearAsync(accountId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MergeWishlistAsync(
        WishlistMergeRequest request,
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await wishlist.MergeAsync(
            accountId,
            request.ProductIds ?? [],
            cancellationToken);
        return Results.Ok(await wishlist.ListAsync(accountId, cancellationToken));
    }

    private static async Task<IResult> GetPublicSettingsAsync(
        HttpContext context,
        StoreSettingsService settings,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await settings.GetPublicAsync(cancellationToken));
    }

    private static async Task<IResult> QuoteCheckoutAsync(
        StoreSettingsService settings,
        CancellationToken cancellationToken,
        string? governorate = null,
        decimal subtotal = 0,
        bool pickup = false) =>
        Results.Ok(await settings.QuoteCheckoutAsync(governorate, subtotal, pickup, cancellationToken));

    private static async Task<IResult> GetShippingRatesAsync(StoreSettingsService settings, CancellationToken cancellationToken) =>
        Results.Ok(await settings.GetShippingRatesAsync(cancellationToken));

    private static async Task<IResult> GetAdminSettingsAsync(
        HttpContext context,
        StoreSettingsService settings,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await settings.GetAdminAsync(cancellationToken));
    }

    /// <summary>The logo is stored under a new key every time; the previous file is deleted only after the row points at the new one.</summary>
    private static async Task<IResult> SetLogoAsync(string kind, HttpContext context, StoreSettingsService settings,
        ICatalogMediaStorage storage, CatalogMediaOptions options, CancellationToken cancellationToken)
    {
        StoreLogoKind.Require(kind);
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        var (upload, problem) = await CatalogEndpoints.ReadSingleImageAsync(context, options, cancellationToken);
        if (upload is null) return problem!;
        var key = await storage.SaveSettingsLogoAsync(kind, upload, cancellationToken);
        StoreLogoChange change;
        try { change = await settings.SetLogoAsync(kind, key, actorId, cancellationToken); }
        catch { await storage.DeleteStorageKeyAsync(key, CancellationToken.None); throw; }
        return await FinishLogoChangeAsync(change, storage, cancellationToken);
    }

    private static async Task<IResult> RemoveLogoAsync(string kind, HttpContext context, StoreSettingsService settings,
        ICatalogMediaStorage storage, CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return await FinishLogoChangeAsync(await settings.SetLogoAsync(kind, null, actorId, cancellationToken), storage, cancellationToken);
    }

    private static async Task<IResult> FinishLogoChangeAsync(StoreLogoChange change, ICatalogMediaStorage storage,
        CancellationToken cancellationToken)
    {
        if (change.PreviousKey is not null)
            await storage.DeleteStorageKeyAsync(change.PreviousKey, cancellationToken);
        return Results.Ok(change.Settings);
    }

    private static IResult ReapplyWatermark(WatermarkReapplyJob job)
    {
        job.Signal();
        return Results.Accepted();
    }

    private static async Task<IResult> UpdateSectionAsync<T>(
        StoreSettingsSectionUpdate<T> request,
        HttpContext context,
        StoreSettingsService settings,
        CancellationToken cancellationToken)
        where T : class, IStoreSettingsSection<T>
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await settings.UpdateSectionAsync(request.Value, request.RowVersion, actorId, cancellationToken));
    }
}

public sealed record WishlistMergeRequest(IReadOnlyList<Guid>? ProductIds);

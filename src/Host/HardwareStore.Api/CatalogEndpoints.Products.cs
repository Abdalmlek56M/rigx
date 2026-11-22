using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>Admin products: edit, publish, photos and the media audit.</summary>
public static partial class CatalogEndpoints
{
    private static void MapProductAdmin(RouteGroupBuilder admin)
    {
        admin.MapGet("/products", GetAdminProductsAsync)
            .RequirePermission("Catalog.View");
        admin.MapGet("/products/{id:guid}", GetAdminProductAsync)
            .RequirePermission("Catalog.View");
        admin.MapPost("/products", CreateProductAsync)
            .RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/products/{id:guid}", UpdateProductAsync)
            .RequirePermission("Catalog.ManageProducts");
        admin.MapPatch("/products/{id:guid}/status", UpdateProductStatusAsync)
            .RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/products/{id:guid}/images", UploadImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapPut("/products/{id:guid}/images/{imageId:guid}", UpdateImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapDelete("/products/{id:guid}/images/{imageId:guid}", DeleteImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapGet("/media-audit", GetMediaAuditAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapPost("/media-audit/cleanup", CleanupOrphanMediaAsync)
            .RequirePermission("Catalog.ManageMedia");
    }

    private static Task<IReadOnlyList<ProductSummaryView>> GetAdminProductsAsync(
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetProductsAsync(cancellationToken);

    private static async Task<Ok<ProductDetailView>> GetAdminProductAsync(
        Guid id,
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.GetProductAsync(id, cancellationToken));

    private static async Task<Created<ProductDetailView>> CreateProductAsync(
        ProductInput input,
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Created(
            (string?)null,
            await catalog.SaveProductAsync(null, input, cancellationToken));

    private static async Task<Ok<ProductDetailView>> UpdateProductAsync(
        Guid id,
        ProductInput input,
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.SaveProductAsync(id, input, cancellationToken));

    private static async Task<Ok<ProductDetailView>> UpdateProductStatusAsync(
        Guid id,
        ProductStatusInput input,
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.SetProductStatusAsync(
            id, input.Status, input.RowVersion, cancellationToken));

    private static async Task<Created<ProductImageView>> UploadImageAsync(
        Guid id,
        ProductImageService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var (form, upload) = await ReadImageFormAsync(context, options, cancellationToken);
        var altText = form["altText"].ToString();
        var isPrimary = bool.TryParse(form["isPrimary"].ToString(), out var parsedPrimary) && parsedPrimary;
        return TypedResults.Created((string?)null, await catalog.AddImageAsync(id, altText, isPrimary, upload, cancellationToken));
    }

    private static async Task<Ok<ProductImageView>> UpdateImageAsync(
        Guid id,
        Guid imageId,
        ImageMetadataInput input,
        ProductImageService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.UpdateImageAsync(
            id, imageId, input.AltText, input.SortOrder, input.IsPrimary, cancellationToken));

    private static async Task<NoContent> DeleteImageAsync(
        Guid id,
        Guid imageId,
        ProductImageService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteImageAsync(id, imageId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<CatalogMediaAuditView> GetMediaAuditAsync(
        CatalogMediaMaintenanceService maintenance,
        CancellationToken cancellationToken) =>
        maintenance.AuditAsync(cancellationToken);

    private static async Task<Ok<CatalogMediaCleanupView>> CleanupOrphanMediaAsync(
        CatalogMediaMaintenanceService maintenance,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await maintenance.CleanupOrphansAsync(cancellationToken));
}

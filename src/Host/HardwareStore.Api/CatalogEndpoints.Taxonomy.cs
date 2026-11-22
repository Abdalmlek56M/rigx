using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>Admin categories and brands: edit, hand order, specs, pictures and logos.</summary>
public static partial class CatalogEndpoints
{
    private static void MapTaxonomyAdmin(RouteGroupBuilder admin)
    {
        admin.MapGet("/categories", GetAdminCategoriesAsync)
            .RequirePermission("Catalog.View");
        admin.MapGet("/brands", GetAdminBrandsAsync)
            .RequirePermission("Catalog.View");
        admin.MapPost("/categories", CreateCategoryAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPut("/categories/{id:guid}", UpdateCategoryAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPut("/categories/{id:guid}/specs", SetCategorySpecsAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPost("/categories/{id:guid}/image", UploadCategoryImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapDelete("/categories/{id:guid}/image", DeleteCategoryImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapPut("/categories/order", ReorderCategoriesAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPut("/brands/order", ReorderBrandsAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPost("/brands", CreateBrandAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPut("/brands/{id:guid}", UpdateBrandAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPost("/brands/{id:guid}/logo", UploadBrandLogoAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapDelete("/brands/{id:guid}/logo", DeleteBrandLogoAsync)
            .RequirePermission("Catalog.ManageMedia");
    }

    private static Task<IReadOnlyList<CategoryView>> GetAdminCategoriesAsync(
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetCategoriesAsync(cancellationToken);

    private static Task<IReadOnlyList<BrandView>> GetAdminBrandsAsync(
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetBrandsAsync(cancellationToken);

    private static async Task<Created<CategoryView>> CreateCategoryAsync(
        CategoryInput input,
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Created(
            (string?)null,
            await catalog.SaveCategoryAsync(null, input, cancellationToken));

    private static async Task<Ok<CategoryView>> UpdateCategoryAsync(
        Guid id,
        CategoryInput input,
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.SaveCategoryAsync(id, input, cancellationToken));

    private static async Task<Ok<CategoryView>> SetCategorySpecsAsync(
        Guid id,
        CategorySpecsInput input,
        CategorySpecService specs,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await specs.SetSpecsAsync(id, input.Specs, input.RowVersion, cancellationToken));

    private static async Task<NoContent> ReorderCategoriesAsync(
        CategoryOrderInput input,
        TaxonomyOrderService order,
        CancellationToken cancellationToken)
    {
        await order.ReorderCategoriesAsync(input.ParentId, input.Ids, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ReorderBrandsAsync(
        BrandOrderInput input,
        TaxonomyOrderService order,
        CancellationToken cancellationToken)
    {
        await order.ReorderBrandsAsync(input.Ids, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Created<BrandView>> CreateBrandAsync(
        BrandInput input,
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Created(
            (string?)null,
            await catalog.SaveBrandAsync(null, input, cancellationToken));

    private static async Task<Ok<BrandView>> UpdateBrandAsync(
        Guid id,
        BrandInput input,
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.SaveBrandAsync(id, input, cancellationToken));

    private static async Task<Ok<CategoryView>> UploadCategoryImageAsync(
        Guid id,
        TaxonomyImageService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var upload = await ReadSingleImageAsync(context, options, cancellationToken);
        return TypedResults.Ok(await catalog.SetCategoryImageAsync(id, upload, cancellationToken));
    }

    private static async Task<Ok<CategoryView>> DeleteCategoryImageAsync(
        Guid id,
        TaxonomyImageService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.DeleteCategoryImageAsync(id, cancellationToken));

    private static async Task<Ok<BrandView>> UploadBrandLogoAsync(
        Guid id,
        TaxonomyImageService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var upload = await ReadSingleImageAsync(context, options, cancellationToken);
        return TypedResults.Ok(await catalog.SetBrandLogoAsync(id, upload, cancellationToken));
    }

    private static async Task<Ok<BrandView>> DeleteBrandLogoAsync(
        Guid id,
        TaxonomyImageService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.DeleteBrandLogoAsync(id, cancellationToken));
}

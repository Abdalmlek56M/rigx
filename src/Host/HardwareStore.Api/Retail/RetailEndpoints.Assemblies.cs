using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private const string ViewCosts = "Inventory.ViewCosts";

    private static async Task<IResult> ListAssembliesAsync(HttpContext context, AssemblyService assemblies, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await assemblies.ListAssembliesAsync(context.HasPermission(ViewCosts), ct));
    }

    private static async Task<IResult> GetAssemblyAsync(Guid id, HttpContext context, AssemblyService assemblies, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await assemblies.GetAsync(id, context.HasPermission(ViewCosts), ct));
    }

    private static async Task<IResult> ListPublishedAssembliesAsync(HttpContext context, AssemblyService assemblies, CancellationToken ct,
        string? usage = null, decimal? minPrice = null, decimal? maxPrice = null)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await assemblies.ListPublishedAssembliesAsync(new AssemblyStoreQuery(usage, minPrice, maxPrice), ct));
    }

    private static async Task<IResult> CreateAssemblyAsync(AssemblyCreateRequest request, HttpContext context, AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.CreateAssemblyAsync(request, context.HasPermission(ViewCosts), ct));

    /// <summary>The build warranty is chosen from the store settings list (Storefront), so the label is resolved here.</summary>
    private static async Task<IResult> UpdateAssemblyAsync(Guid id, AssemblyEditRequest request, HttpContext context,
        AssemblyService assemblies, StoreSettingsService settings, CancellationToken ct)
    {
        var (label, months) = WarrantyChoice.Resolve(await WarrantyChoice.LoadOptionsAsync(settings, ct), request.WarrantyLabel);
        var update = new AssemblyUpdateRequest(request.Name, request.PublicTitleAr, request.PublicTitleEn, request.SalePrice,
            request.RowVersion, request.Usage, request.SellMode, label, months, request.Copies ?? 1, request.SuitableForAr, request.SuitableForEn);
        return Results.Ok(await assemblies.UpdateAssemblyAsync(id, update, context.HasPermission(ViewCosts), ct));
    }

    private static async Task<IResult> SetAllocationsAsync(Guid id, AssemblyAllocationRequest request, HttpContext context,
        AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.SetAssemblyAllocationsAsync(id, request, context.HasPermission(ViewCosts), ct));

    private static async Task<IResult> PublishAssemblyAsync(Guid id, RetailVersionRequest request, HttpContext context,
        AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.SetAssemblyPublishedAsync(id, true, request.RowVersion, context.HasPermission(ViewCosts), ct));

    private static async Task<IResult> UnpublishAssemblyAsync(Guid id, RetailVersionRequest request, HttpContext context,
        AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.SetAssemblyPublishedAsync(id, false, request.RowVersion, context.HasPermission(ViewCosts), ct));

    private static async Task<IResult> DisassembleAsync(Guid id, RetailVersionRequest request, HttpContext context,
        AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.DisassembleAsync(id, request.RowVersion, context.HasPermission(ViewCosts), ct));

    private static async Task<IResult> AddAssemblyPartAsync(Guid id, AssemblyAddPartRequest request, HttpContext context,
        AssemblyPartService parts, CancellationToken ct) =>
        Results.Ok(await parts.AddPartAsync(id, request, context.HasPermission(ViewCosts), ct));

    /// <summary>A part that is not in stock: warranty from the settings list and specs checked against the category schema (Catalog).</summary>
    private static async Task<IResult> AddNewAssemblyPartAsync(Guid id, AssemblyNewPartBody request, HttpContext context,
        AssemblyPartService parts, StoreSettingsService settings, CategorySpecService categorySpecs, CancellationToken ct)
    {
        var (label, months) = WarrantyChoice.Resolve(await WarrantyChoice.LoadOptionsAsync(settings, ct), request.WarrantyLabel);
        var specs = await categorySpecs.CheckValuesAsync(request.CategoryId, request.Specs ?? [], ct);
        var part = new ReceiptLine(request.ModelName ?? string.Empty, request.Condition ?? string.Empty, 1, request.AcquisitionCost,
            request.Price, request.CategoryId, request.ProductId, request.WithBoardExtra, request.AloneExtra, request.SerialNumber,
            request.PartnerPercentOverride, label, months, request.HasBox, request.IsHidden, request.InternalNote, specs, request.PartnerId);
        return Results.Ok(await parts.AddNewPartAsync(id, new AssemblyNewPartRequest(part, request.RowVersion), context.HasPermission(ViewCosts), ct));
    }

    private static async Task<IResult> UpdateAssemblyPartAsync(Guid id, Guid unitId, AssemblyPartBody request, HttpContext context,
        AssemblyPartService parts, StoreSettingsService settings, CancellationToken ct)
    {
        var (label, months) = WarrantyChoice.Resolve(await WarrantyChoice.LoadOptionsAsync(settings, ct), request.WarrantyLabel);
        var update = new AssemblyPartUpdateRequest(request.PartPrice, label, months, request.PartnerPercentOverride, request.PartnerId, request.RowVersion);
        return Results.Ok(await parts.UpdatePartAsync(id, unitId, update, context.HasPermission(ViewCosts), ct));
    }

    private static async Task<IResult> RemoveAssemblyPartAsync(Guid id, Guid unitId, AssemblyRemovePartRequest request, HttpContext context,
        AssemblyPartService parts, CancellationToken ct) =>
        Results.Ok(await parts.RemovePartAsync(id, unitId, request, context.HasPermission(ViewCosts), ct));

    private static async Task<IResult> ReplaceAssemblyPartAsync(Guid id, Guid unitId, AssemblyReplacePartRequest request, HttpContext context,
        AssemblyPartService parts, CancellationToken ct) =>
        Results.Ok(await parts.ReplacePartAsync(id, unitId, request, context.HasPermission(ViewCosts), ct));

    private static async Task<IResult> AddAssemblyPhotoAsync(Guid id, HttpContext context, AssemblyService assemblies,
        ICatalogMediaStorage storage, CatalogMediaOptions options, MediaWatermarkSource watermarks, CancellationToken ct)
    {
        await assemblies.GetAsync(id, false, ct);
        var (upload, problem) = await CatalogEndpoints.ReadSingleImageAsync(context, options, ct);
        if (upload is null) return problem!;
        var stored = await storage.SaveImageAsync(id, Guid.CreateVersion7(), upload, await watermarks.ForPhotosAsync(ct), ct);
        return Results.Ok(await assemblies.AddPhotoAsync(id, new StockItemPhoto(stored.StorageKey, stored.ThumbnailStorageKey), ct));
    }

    private static async Task<IResult> RemoveAssemblyPhotoAsync(Guid id, string storageKey, AssemblyService assemblies,
        ICatalogMediaStorage storage, CancellationToken ct)
    {
        var photo = await assemblies.RemovePhotoAsync(id, storageKey, ct);
        await storage.DeleteAsync(photo.StorageKey, ct);
        return Results.NoContent();
    }
}

/// <summary>Build card edit; <paramref name="WarrantyLabel"/> is one of the settings warranty options (empty = per part).</summary>
public sealed record AssemblyEditRequest(string? Name, string? PublicTitleAr, string? PublicTitleEn, decimal SalePrice, long RowVersion,
    string? Usage = null, string? SellMode = null, string? WarrantyLabel = null, int? Copies = null, string? SuitableForAr = null,
    string? SuitableForEn = null);
/// <summary>Per-part edit; an empty <paramref name="WarrantyLabel"/> means "use the build's warranty".</summary>
public sealed record AssemblyPartBody(decimal PartPrice, string? WarrantyLabel, decimal? PartnerPercentOverride, Guid? PartnerId, long RowVersion);
public sealed record AssemblyNewPartBody(string? ModelName, string? Condition, decimal AcquisitionCost, decimal Price, long RowVersion,
    Guid? CategoryId = null, Guid? ProductId = null, decimal? WithBoardExtra = null, decimal? AloneExtra = null, string? SerialNumber = null,
    decimal? PartnerPercentOverride = null, string? WarrantyLabel = null, bool HasBox = false, bool IsHidden = false,
    string? InternalNote = null, Dictionary<string, string>? Specs = null, Guid? PartnerId = null);

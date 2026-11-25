using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    private const int MaxCatalogSuggestionCandidates = 50;
    private const int MaxSpecFilters = 10;
    private const string SpecFilterPrefix = "spec.";

    private static async Task<Ok<PagedResult<StockItemAdminView>>> ListStockItemsAsync(HttpContext context, StockItemAdminService items, CancellationToken ct,
        string? search = null, Guid? categoryId = null, string? condition = null, bool? hidden = null, bool? inStock = null,
        int page = 1, int pageSize = 50)
    {
        context.Response.Headers.CacheControl = "no-store";
        var query = new StockItemQuery(search, categoryId, condition, hidden, inStock, page, pageSize);
        return TypedResults.Ok(await items.ListAsync(query, context.HasPermission("Inventory.ViewCosts"),
            context.HasPermission("Inventory.ViewSerials"), ct));
    }

    private static async Task<Ok<StockItemAdminView>> GetStockItemAsync(Guid id, HttpContext context, StockItemAdminService items, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await items.GetAsync(id, context.HasPermission("Inventory.ViewCosts"),
            context.HasPermission("Inventory.ViewSerials"), ct));
    }

    /// <summary>Warranty is chosen from the store settings list (Storefront), so the label is resolved here.</summary>
    private static async Task<Ok<StockItemAdminView>> UpdateStockItemAsync(Guid id, StockItemUpdateRequest request, HttpContext context,
        StockItemAdminService items, StoreSettingsService settings, CategorySpecService categorySpecs, CancellationToken ct)
    {
        var (label, months) = WarrantyChoice.Resolve(await WarrantyChoice.LoadOptionsAsync(settings, ct), request.WarrantyLabel);
        var update = new StockItemUpdate(request.ModelName, request.CategoryId, request.BrandId, request.ProductId, request.Price,
            request.WithBoardExtra, request.AloneExtra, label, months, request.HasBox, request.IsHidden, request.InternalNote,
            await categorySpecs.CheckValuesAsync(request.CategoryId, request.Specs ?? [], ct), request.RowVersion, request.PriceOptions, request.ExcludedFromStockValue,
            request.Description, request.DescriptionEn);
        return TypedResults.Ok(await items.UpdateAsync(id, update, context.HasPermission("Inventory.ViewCosts"),
            context.HasPermission("Inventory.ViewSerials"), ct));
    }

    private static async Task<Ok<StockVisibilityResult>> SetStockVisibilityAsync(StockVisibilityRequest request, StockItemAdminService items, CancellationToken ct) =>
        TypedResults.Ok(new StockVisibilityResult(await items.SetVisibilityAsync(request, ct)));

    private static async Task<Ok<IReadOnlyList<StockItemPhoto>>> AddStockItemPhotoAsync(Guid id, HttpContext context, StockItemAdminService items,
        ICatalogMediaStorage storage, CatalogMediaOptions options, MediaWatermarkSource watermarks, CancellationToken ct)
    {
        await items.GetAsync(id, false, false, ct);
        var upload = await CatalogEndpoints.ReadSingleImageAsync(context, options, ct);
        var stored = await storage.SaveImageAsync(id, Guid.CreateVersion7(), upload, await watermarks.ForPhotosAsync(ct), ct);
        return TypedResults.Ok(await items.AddPhotoAsync(id, new StockItemPhoto(stored.StorageKey, stored.ThumbnailStorageKey), ct));
    }

    private static async Task<Ok<IReadOnlyList<StockItemPhoto>>> ReorderStockItemPhotosAsync(Guid id, PhotoOrderRequest request, StockItemAdminService items, CancellationToken ct) =>
        TypedResults.Ok(await items.ReorderPhotosAsync(id, request.StorageKeys, ct));

    private static async Task<NoContent> RemoveStockItemPhotoAsync(Guid id, string storageKey, StockItemAdminService items,
        ICatalogMediaStorage storage, CancellationToken ct)
    {
        var photo = await items.RemovePhotoAsync(id, storageKey, ct);
        await storage.DeleteAsync(photo.StorageKey, ct);
        return TypedResults.NoContent();
    }

    /// <summary>Model names from stock plus catalog products, one entry per spelling-independent model key.</summary>
    private static async Task<Ok<List<ModelNameSuggestion>>> SuggestModelsAsync(StockItemAdminService items, ProductManagementService products,
        CancellationToken ct, string? q = null, Guid? categoryId = null)
    {
        var suggestions = (await items.SuggestModelsAsync(q, categoryId, ct)).ToList();
        var key = ModelName.Key(q ?? string.Empty);
        if (key.Length >= 2)
        {
            var matches = await products.FindNamesAsync(q ?? string.Empty, MaxCatalogSuggestionCandidates, ct);
            var known = suggestions.Select(suggestion => ModelName.Key(suggestion.ModelName)).ToHashSet();
            foreach (var product in matches.Where(match => ModelName.Key(match.Name).Contains(key) && known.Add(ModelName.Key(match.Name))))
                suggestions.Add(new ModelNameSuggestion(product.Name, product.CategoryId, product.Id, 0));
        }
        return TypedResults.Ok(suggestions);
    }

    private static async Task<Ok<StoreItemPage>> ListStoreItemsAsync(HttpContext context, StoreItemService items, TaxonomyService taxonomy, CancellationToken ct,
        string? search = null, Guid? categoryId = null, Guid? brandId = null, string? condition = null, bool? inStock = null,
        decimal? minPrice = null, decimal? maxPrice = null, string? sort = null, int page = 1, int pageSize = 24)
    {
        var categoryIds = categoryId is Guid category ? await taxonomy.GetCategoryWithDescendantIdsAsync(category, ct) : null;
        return TypedResults.Ok(await items.ListAsync(
            new StoreItemQuery(search, categoryIds, condition, inStock, minPrice, maxPrice, sort, page, pageSize,
                SpecFilters(context.Request.Query), brandId), ct));
    }

    /// <summary>Reads <c>spec.&lt;key&gt;=value</c> query parameters (valid keys only, at most <see cref="MaxSpecFilters"/>).</summary>
    private static Dictionary<string, string> SpecFilters(IQueryCollection query)
    {
        var filters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, values) in query)
        {
            if (filters.Count == MaxSpecFilters) break;
            if (!name.StartsWith(SpecFilterPrefix, StringComparison.Ordinal)) continue;
            var key = name[SpecFilterPrefix.Length..];
            var value = values.ToString().Trim();
            if (value.Length > 0 && CategorySpecSchema.IsValidKey(key)) filters[key] = value;
        }
        return filters;
    }

    private static async Task<Ok<StoreItemDetail>> GetStoreItemAsync(string key, StoreItemService items, CancellationToken ct) =>
        TypedResults.Ok(await items.GetAsync(key, ct));
}

public sealed record StockItemUpdateRequest(string? ModelName, Guid? CategoryId, Guid? BrandId, Guid? ProductId, decimal Price, decimal? WithBoardExtra,
    decimal? AloneExtra, string? WarrantyLabel, bool HasBox, bool IsHidden, string? InternalNote,
    Dictionary<string, string>? Specs, long RowVersion, ItemPriceOptions? PriceOptions = null,
    bool ExcludedFromStockValue = false, string? Description = null, string? DescriptionEn = null);
public sealed record StockVisibilityResult(int Updated);

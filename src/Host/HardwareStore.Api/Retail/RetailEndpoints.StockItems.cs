using HardwareStore.Common.Domain;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public static partial class RetailEndpoints
{
    private const int MaxCatalogSuggestionCandidates = 50;
    private const int MaxSpecFilters = 10;
    private const string SpecFilterPrefix = "spec.";

    private static async Task<IResult> ListStockItemsAsync(HttpContext context, StockItemAdminService items, CancellationToken ct,
        string? search = null, Guid? categoryId = null, string? condition = null, bool? hidden = null, bool? inStock = null,
        int page = 1, int pageSize = 50)
    {
        context.Response.Headers.CacheControl = "no-store";
        var query = new StockItemQuery(search, categoryId, condition, hidden, inStock, page, pageSize);
        return Results.Ok(await items.ListAsync(query, context.HasPermission("Inventory.ViewCosts"),
            context.HasPermission("Inventory.ViewSerials"), ct));
    }

    private static async Task<IResult> GetStockItemAsync(Guid id, HttpContext context, StockItemAdminService items, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await items.GetAsync(id, context.HasPermission("Inventory.ViewCosts"),
            context.HasPermission("Inventory.ViewSerials"), ct));
    }

    /// <summary>Warranty is chosen from the store settings list (Storefront), so the label is resolved here.</summary>
    private static async Task<IResult> UpdateStockItemAsync(Guid id, StockItemUpdateRequest request, HttpContext context,
        StockItemAdminService items, StoreSettingsService settings, CategorySpecService categorySpecs, CancellationToken ct)
    {
        var (label, months) = WarrantyChoice.Resolve(await WarrantyChoice.LoadOptionsAsync(settings, ct), request.WarrantyLabel);
        var update = new StockItemUpdate(request.ModelName, request.CategoryId, request.ProductId, request.Price,
            request.WithBoardExtra, request.AloneExtra, label, months, request.HasBox, request.IsHidden, request.InternalNote,
            await categorySpecs.CheckValuesAsync(request.CategoryId, request.Specs ?? [], ct), request.RowVersion);
        return Results.Ok(await items.UpdateAsync(id, update, context.HasPermission("Inventory.ViewCosts"),
            context.HasPermission("Inventory.ViewSerials"), ct));
    }

    private static async Task<IResult> SetStockVisibilityAsync(StockVisibilityRequest request, StockItemAdminService items, CancellationToken ct) =>
        Results.Ok(new StockVisibilityResult(await items.SetVisibilityAsync(request, ct)));

    private static async Task<IResult> AddStockItemPhotoAsync(Guid id, HttpContext context, StockItemAdminService items,
        ICatalogMediaStorage storage, CatalogMediaOptions options, MediaWatermarkSource watermarks, CancellationToken ct)
    {
        await items.GetAsync(id, false, false, ct);
        var (upload, problem) = await CatalogEndpoints.ReadSingleImageAsync(context, options, ct);
        if (upload is null) return problem!;
        var stored = await storage.SaveImageAsync(id, Guid.CreateVersion7(), upload, await watermarks.ForPhotosAsync(ct), ct);
        return Results.Ok(await items.AddPhotoAsync(id, new StockItemPhoto(stored.StorageKey, stored.ThumbnailStorageKey), ct));
    }

    private static async Task<IResult> RemoveStockItemPhotoAsync(Guid id, string storageKey, StockItemAdminService items,
        ICatalogMediaStorage storage, CancellationToken ct)
    {
        var photo = await items.RemovePhotoAsync(id, storageKey, ct);
        await storage.DeleteAsync(photo.StorageKey, ct);
        return Results.NoContent();
    }

    /// <summary>Model names from stock plus catalog products, one entry per spelling-independent model key.</summary>
    private static async Task<IResult> SuggestModelsAsync(StockItemAdminService items, IDbContextFactory<CatalogDbContext> catalogFactory,
        CancellationToken ct, string? q = null, Guid? categoryId = null)
    {
        var suggestions = (await items.SuggestModelsAsync(q, categoryId, ct)).ToList();
        var key = ModelName.Key(q ?? string.Empty);
        if (key.Length >= 2)
        {
            var pattern = "%" + (q ?? string.Empty).Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            await using var catalog = await catalogFactory.CreateDbContextAsync(ct);
            var products = await catalog.Products.AsNoTracking().Where(product => EF.Functions.ILike(product.Name, pattern))
                .OrderBy(product => product.Name).Take(MaxCatalogSuggestionCandidates)
                .Select(product => new { product.Id, product.Name, product.CategoryId }).ToListAsync(ct);
            var known = suggestions.Select(suggestion => ModelName.Key(suggestion.ModelName)).ToHashSet();
            foreach (var product in products.Where(product => ModelName.Key(product.Name).Contains(key) && known.Add(ModelName.Key(product.Name))))
                suggestions.Add(new ModelNameSuggestion(product.Name, product.CategoryId, product.Id, 0));
        }
        return Results.Ok(suggestions);
    }

    private static async Task<IResult> ListStoreItemsAsync(HttpContext context, StoreItemService items, TaxonomyService taxonomy, CancellationToken ct,
        string? search = null, Guid? categoryId = null, string? condition = null, bool? inStock = null, decimal? minPrice = null,
        decimal? maxPrice = null, string? sort = null, int page = 1, int pageSize = 24)
    {
        var categoryIds = categoryId is Guid category ? await taxonomy.GetCategoryWithDescendantIdsAsync(category, ct) : null;
        return Results.Ok(await items.ListAsync(
            new StoreItemQuery(search, categoryIds, condition, inStock, minPrice, maxPrice, sort, page, pageSize,
                SpecFilters(context.Request.Query)), ct));
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

    private static async Task<IResult> GetStoreItemAsync(string code, StoreItemService items, CancellationToken ct) =>
        Results.Ok(await items.GetAsync(code, ct));
}

public sealed record StockItemUpdateRequest(string? ModelName, Guid? CategoryId, Guid? ProductId, decimal Price, decimal? WithBoardExtra,
    decimal? AloneExtra, string? WarrantyLabel, bool HasBox, bool IsHidden, string? InternalNote,
    Dictionary<string, string>? Specs, long RowVersion);
public sealed record StockVisibilityResult(int Updated);

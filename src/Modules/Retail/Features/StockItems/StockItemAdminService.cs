using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Back-office stock items: search and paging, editing, hiding, photos and model-name suggestions.</summary>
public sealed class StockItemAdminService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    private const int MaxVisibilityIds = 200;
    private const int MinSuggestionKeyLength = 2;
    private const int MaxSuggestions = 10;
    private const int SuggestionCandidates = 1000;

    public async Task<PagedResult<StockItemAdminView>> ListAsync(StockItemQuery query, bool includeCosts, bool includeSerials, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = db.StockItems.AsNoTracking().Matching(query.Search, includeSerials);
        if (!string.IsNullOrWhiteSpace(query.Condition))
        {
            var condition = StockCondition.Parse(query.Condition);
            items = items.Where(item => item.Condition == condition);
        }
        if (query.CategoryId is Guid categoryId) items = items.Where(item => item.CategoryId == categoryId);
        if (query.Hidden is bool hidden) items = items.Where(item => item.IsHidden == hidden);
        if (query.InStock is bool inStock)
            items = items.Where(item => item.Units.Any(unit => unit.Status == UnitStatus.Available) == inStock);
        var (page, pageSize) = PageRequest.Of(query.Page, query.PageSize);
        var total = await items.CountAsync(cancellationToken);
        var rows = await items.Include(item => item.Units).OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).AsSplitQuery().ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var builds = await StockItemMapping.BuildsOfAsync(db, rows, cancellationToken);
        return new PagedResult<StockItemAdminView>(rows.Select(item => StockItemMapping.ToAdminView(item, now, includeCosts, includeSerials, builds)).ToList(),
            page, pageSize, total);
    }

    public async Task<StockItemAdminView> GetAsync(Guid id, bool includeCosts, bool includeSerials, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.StockItems.AsNoTracking().Include(row => row.Units)
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw ItemNotFound();
        return StockItemMapping.ToAdminView(item, timeProvider.GetUtcNow(), includeCosts, includeSerials, await StockItemMapping.BuildsOfAsync(db, [item], cancellationToken));
    }

    public async Task<StockItemAdminView> UpdateAsync(Guid id, StockItemUpdate request, bool includeCosts = false,
        bool includeSerials = false, CancellationToken cancellationToken = default)
    {
        var modelName = StockItemRules.CleanModelName(request.ModelName);
        StockItemRules.ValidatePrices(request.Price, request.WithBoardExtra, request.AloneExtra);
        StockItemRules.ValidateWarranty(request.WarrantyLabel, request.WarrantyMonths);
        StockItemRules.ValidatePriceOptions(request.PriceOptions);
        var specs = StockItemRules.CleanSpecs(request.Specs);
        var note = StockItemRules.CleanNote(request.InternalNote);
        var description = StockItemRules.CleanDescription(request.Description, "description");
        var descriptionEn = StockItemRules.CleanDescription(request.DescriptionEn, "descriptionEn");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.StockItems.Include(row => row.Units)
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw ItemNotFound();
        if (item.RowVersion != request.RowVersion) throw ChangedByAnotherUser();
        item.ModelName = modelName;
        item.ModelKey = ModelName.Key(modelName);
        await StockItemSlugs.AssignAsync(db, item, cancellationToken);
        item.CategoryId = request.CategoryId;
        item.BrandId = request.BrandId;
        item.ProductId = request.ProductId;
        item.Price = request.Price;
        item.WithBoardExtra = request.WithBoardExtra;
        item.AloneExtra = request.AloneExtra;
        item.OffersAlone = request.PriceOptions?.Alone;
        item.OffersWithBoard = request.PriceOptions?.WithBoard;
        item.OffersInBuild = request.PriceOptions?.InBuild;
        item.WarrantyLabel = string.IsNullOrWhiteSpace(request.WarrantyLabel) ? null : request.WarrantyLabel.Trim();
        item.WarrantyMonths = item.WarrantyLabel is null ? null : request.WarrantyMonths;
        item.HasBox = request.HasBox;
        item.ExcludedFromStockValue = request.ExcludedFromStockValue;
        item.IsHidden = request.IsHidden;
        item.InternalNote = note;
        item.Specs = specs;
        item.Description = description;
        item.DescriptionEn = descriptionEn;
        foreach (var unit in item.Units.Where(unit => unit.ProductId != request.ProductId))
        {
            unit.ProductId = request.ProductId;
            unit.RowVersion++;
        }
        Touch(item);
        await SaveAsync(db, cancellationToken);
        return StockItemMapping.ToAdminView(item, timeProvider.GetUtcNow(), includeCosts, includeSerials, await StockItemMapping.BuildsOfAsync(db, [item], cancellationToken));
    }

    public async Task<int> SetVisibilityAsync(StockVisibilityRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.Ids.Distinct().ToList();
        if (ids.Count is < 1 or > MaxVisibilityIds)
            throw new RetailException(400, $"Choose between 1 and {MaxVisibilityIds} items.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await db.StockItems.Where(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);
        if (items.Count != ids.Count) throw new RetailException(404, "Some items were not found.");
        foreach (var item in items)
        {
            item.IsHidden = request.Hidden;
            Touch(item);
        }
        await SaveAsync(db, cancellationToken);
        return items.Count;
    }

    public async Task<IReadOnlyList<StockItemPhoto>> AddPhotoAsync(Guid id, StockItemPhoto photo, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.StockItems.SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw ItemNotFound();
        if (item.Photos.Count >= StockItemRules.MaxPhotos)
            throw new RetailException(409, $"An item can have at most {StockItemRules.MaxPhotos} photos.");
        item.Photos = [.. item.Photos, photo];
        Touch(item);
        await SaveAsync(db, cancellationToken);
        return item.Photos;
    }

    /// <summary>The item's photos, or none when there is no such item (a copy source that is gone copies nothing).</summary>
    public async Task<IReadOnlyList<StockItemPhoto>> FindPhotosAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var photos = await db.StockItems.AsNoTracking().Where(item => item.Id == id).Select(item => item.Photos).SingleOrDefaultAsync(cancellationToken);
        return photos ?? [];
    }

    public async Task<IReadOnlyList<OwnedPhoto>> ListPhotosAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.StockItems.AsNoTracking().Where(item => item.PhotoCount > 0)
            .Select(item => new { item.Id, item.Photos }).ToListAsync(cancellationToken);
        return rows.SelectMany(row => row.Photos.Select(photo => new OwnedPhoto(row.Id, photo))).ToList();
    }

    /// <summary>Swaps one photo for a re-rendered one in place; false when the old photo is no longer on the item (removed meanwhile).</summary>
    public async Task<bool> ReplacePhotoAsync(Guid id, string oldStorageKey, StockItemPhoto replacement, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.StockItems.SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        var index = item?.Photos.FindIndex(entry => entry.StorageKey == oldStorageKey) ?? -1;
        if (item is null || index < 0) return false;
        item.Photos = item.Photos.Select((entry, position) => position == index ? replacement : entry).ToList();
        Touch(item);
        await SaveAsync(db, cancellationToken);
        return true;
    }

    /// <summary>Puts the photos in the given order (the first is the main photo); the keys must be exactly the item's photos.</summary>
    public async Task<IReadOnlyList<StockItemPhoto>> ReorderPhotosAsync(Guid id, IReadOnlyList<string>? storageKeys, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.StockItems.SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw ItemNotFound();
        item.Photos = PhotoOrder.Apply(item.Photos, storageKeys);
        Touch(item);
        await SaveAsync(db, cancellationToken);
        return item.Photos;
    }

    /// <summary>Removes the photo from the item and returns it, so the caller can delete the stored files.</summary>
    public async Task<StockItemPhoto> RemovePhotoAsync(Guid id, string storageKey, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.StockItems.SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw ItemNotFound();
        var photo = item.Photos.FirstOrDefault(entry => entry.StorageKey == storageKey)
            ?? throw new RetailException(404, "The photo was not found.");
        item.Photos = item.Photos.Where(entry => entry != photo).ToList();
        Touch(item);
        await SaveAsync(db, cancellationToken);
        return photo;
    }

    /// <summary>Model names already in stock that match what the owner is typing, spellings grouped by model key.</summary>
    public async Task<IReadOnlyList<ModelNameSuggestion>> SuggestModelsAsync(string? text, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        var key = ModelName.Key(text ?? string.Empty);
        if (key.Length < MinSuggestionKeyLength) return [];
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = db.StockItems.AsNoTracking().Where(item => item.ModelKey.Contains(key));
        if (categoryId is Guid category) items = items.Where(item => item.CategoryId == category);
        var candidates = await items.OrderByDescending(item => item.CreatedAtUtc).Take(SuggestionCandidates)
            .Select(item => new StockItemTemplate(item.Id, item.Code, item.ModelName, item.CategoryId, item.BrandId, item.ProductId, item.Specs,
                item.PhotoCount))
            .ToListAsync(cancellationToken);
        return candidates.GroupBy(item => ModelName.Key(item.ModelName))
            .Select(group => new ModelNameSuggestion(
                group.GroupBy(item => item.ModelName).OrderByDescending(spelling => spelling.Count()).First().Key,
                group.Select(item => item.CategoryId).FirstOrDefault(value => value is not null),
                group.Select(item => item.ProductId).FirstOrDefault(value => value is not null),
                group.Count(),
                StockItemRules.PickTemplate(group)))
            .OrderByDescending(suggestion => suggestion.ItemCount).ThenBy(suggestion => suggestion.ModelName, StringComparer.Ordinal)
            .Take(MaxSuggestions).ToList();
    }

    private void Touch(StockItem item)
    {
        item.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        item.RowVersion++;
    }

    private static async Task SaveAsync(RetailDbContext db, CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw ChangedByAnotherUser(); }
    }

    private static RetailException ItemNotFound() => new(404, "The item was not found.");
    private static RetailException ChangedByAnotherUser() => new(409, "The item was changed by another user.");
}

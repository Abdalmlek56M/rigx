using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Stock items for the PC builder: published items only, never cost, serial or internal note.</summary>
public sealed class BuilderItemService(IDbContextFactory<RetailDbContext> contextFactory)
{
    private const int MaxPageSize = 60;

    /// <summary>Options of a builder slot: available first, then the cheapest in-build price, then the newest.</summary>
    public async Task<PagedResult<BuilderItemView>> ListOptionsAsync(IReadOnlyCollection<Guid> categoryIds, string? condition, string? search, int page,
        int pageSize, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = db.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter).Matching(search, includeSerials: false)
            .Where(item => item.CategoryId != null && categoryIds.Contains(item.CategoryId.Value));
        if (!string.IsNullOrWhiteSpace(condition))
        {
            var parsed = StockCondition.Parse(condition);
            items = items.Where(item => item.Condition == parsed);
        }
        var total = await items.CountAsync(cancellationToken);
        (page, pageSize) = PageRequest.Of(page, pageSize, MaxPageSize);
        var rows = await items
            .Select(item => new { Item = item, Available = item.Units.Count(unit => unit.Status == UnitStatus.Available) })
            .OrderByDescending(row => row.Available > 0).ThenBy(row => row.Item.Price).ThenByDescending(row => row.Item.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<BuilderItemView>(rows.Select(row => View(row.Item, row.Available)).ToList(), page, pageSize, total);
    }

    /// <summary>The published items with these codes; a code that is missing or not published is simply absent.</summary>
    public async Task<IReadOnlyList<BuilderItemView>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter).Where(item => codes.Contains(item.Code))
            .Select(item => new { Item = item, Available = item.Units.Count(unit => unit.Status == UnitStatus.Available) })
            .ToListAsync(cancellationToken);
        return rows.Select(row => View(row.Item, row.Available)).ToList();
    }

    /// <summary>
    /// "Edit this build": for each part of a published build, the same model from free stock (the build itself stays untouched).
    /// Prefers the same condition, then the lowest in-build price.
    /// </summary>
    public async Task<BuildMatchView> MatchBuildAsync(string buildCode, CancellationToken cancellationToken = default)
    {
        if (!PublicCode.TryNormalize(buildCode, PublicCode.Build, out var code)) throw BuildNotFound();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await db.Assemblies.AsNoTracking().WithUnits()
            .SingleOrDefaultAsync(assembly => assembly.Code == code && assembly.Status == AssemblyStatus.Published, cancellationToken)
            ?? throw BuildNotFound();
        var modelKeys = build.Units.Select(link => link.Unit.StockItem.ModelKey).Distinct().ToList();
        var rows = await db.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter).Where(item => modelKeys.Contains(item.ModelKey))
            .Select(item => new { Item = item, Available = item.Units.Count(unit => unit.Status == UnitStatus.Available) })
            .Where(row => row.Available > 0).ToListAsync(cancellationToken);
        var parts = build.Units.OrderBy(link => link.Unit.StockItem.Code).Select(link =>
        {
            var unit = link.Unit;
            var best = rows.Where(row => row.Item.ModelKey == unit.StockItem.ModelKey)
                .OrderByDescending(row => row.Item.Condition == unit.Condition).ThenBy(row => row.Item.Price)
                .FirstOrDefault();
            return new BuildPartMatch(unit.Name, unit.Condition, unit.StockItem.CategoryId, best is null ? null : View(best.Item, best.Available));
        }).ToList();
        return new BuildMatchView(build.Code, build.PublicTitleAr ?? build.PublicTitleEn ?? build.Name, build.Usage, parts);
    }

    private static BuilderItemView View(StockItem item, int available) =>
        new(item.Id, item.Code, item.ModelName, item.Condition, item.CategoryId, item.WarrantyLabel,
            StockItemRules.Prices(item.Price, item.WithBoardExtra, item.AloneExtra), available,
            item.Photos.FirstOrDefault()?.ThumbnailStorageKey, item.Specs);

    private static RetailException BuildNotFound() => new(404, "The build was not found.");
}

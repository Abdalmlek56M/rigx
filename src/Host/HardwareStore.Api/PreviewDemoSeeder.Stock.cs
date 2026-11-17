using HardwareStore.Common.Domain;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;

internal sealed partial class PreviewDemoSeeder
{
    /// <summary>
    /// The 32 published stock items (3 photos each, costs, specs and a demo supplier note). Used pieces are single RX items,
    /// new pieces are lots, exactly as the real purchase receipt makes them.
    /// </summary>
    private async Task<int> SeedStockAsync(CancellationToken cancellationToken)
    {
        clock.UtcNow = seedNow;
        var categories = await CategoryIdsAsync(cancellationToken);
        var existing = await ExistingDemoItemsAsync(cancellationToken);
        var created = 0;
        foreach (var definition in PreviewDemoDefinitions.StockItems())
        {
            var note = PreviewDemoDefinitions.StockNotePrefix + definition.Key;
            if (!existing.TryGetValue(note, out var item))
            {
                var line = new ReceiptLine(definition.ModelName, definition.Condition, definition.Quantity, definition.Cost,
                    definition.Price, CategoryId: categories.GetValueOrDefault(definition.CategorySlug),
                    WithBoardExtra: definition.WithBoardExtra, AloneExtra: definition.AloneExtra, InternalNote: note,
                    Specs: definition.Specs);
                var receipt = await Units.ReceiveAsync(new ReceiveRequest(Guid.CreateVersion7(), null,
                    new ReceiptSource("مورد ديمو", "Company", "Cash", today.AddDays(-32)), [line]), ownerId, cancellationToken);
                var received = receipt.Items.Single();
                item = new DemoStockItem(received.Id, 0, received.Units.Select(unit => unit.Id).ToList());
                created++;
            }

            itemIdsByKey[definition.Key] = item.Id;
            unitsByKey[definition.Key] = item.UnitIds;
            if (item.Photos < StockItemRules.MinPhotosToPublish)
                await AddStockPhotosAsync(item.Id, definition.Key, StockItemRules.MinPhotosToPublish - item.Photos,
                    cancellationToken);
        }
        return created;
    }

    /// <summary>The three published builds (gaming, study/work, budget) made from one available unit of each listed item.</summary>
    private async Task<int> SeedBuildsAsync(CancellationToken cancellationToken)
    {
        clock.UtcNow = seedNow.AddDays(-26);
        await using var db = await retailFactory.CreateDbContextAsync(cancellationToken);
        var existingNames = await db.Assemblies.AsNoTracking()
            .Where(assembly => assembly.Name.StartsWith(PreviewDemoDefinitions.BuildNamePrefix))
            .Select(assembly => assembly.Name)
            .ToListAsync(cancellationToken);
        var usedUnits = new HashSet<Guid>();
        var created = 0;
        foreach (var definition in PreviewDemoDefinitions.Builds())
        {
            var name = PreviewDemoDefinitions.BuildNamePrefix + definition.Key;
            if (existingNames.Contains(name)) continue;
            var unitIds = new List<Guid>();
            foreach (var partKey in definition.PartKeys)
            {
                var unitId = unitsByKey.TryGetValue(partKey, out var units)
                    ? units.FirstOrDefault(candidate => usedUnits.Add(candidate))
                    : Guid.Empty;
                if (unitId == Guid.Empty)
                    throw new InvalidOperationException($"Demo stock is missing a free unit for '{partKey}'.");
                unitIds.Add(unitId);
            }

            var build = await Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest(name, unitIds), true, cancellationToken);
            var card = await Assemblies.UpdateAssemblyAsync(build.Id, new AssemblyUpdateRequest(name, definition.TitleAr,
                definition.TitleEn, definition.SalePrice, build.RowVersion, definition.Usage, AssemblySellMode.Full,
                Copies: 1, SuitableForAr: definition.SuitableForAr, SuitableForEn: definition.SuitableForEn), true,
                cancellationToken);
            card = await Assemblies.SetAssemblyPublishedAsync(card.Id, true, card.RowVersion, true, cancellationToken);
            var upload = PreviewInitializationService.CreatePlaceholderUpload(
                definition.Key.GetHashCode(StringComparison.Ordinal));
            var stored = await mediaStorage.SaveImageAsync(card.Id, Guid.CreateVersion7(), upload, null, cancellationToken);
            await Assemblies.AddPhotoAsync(card.Id, new StockItemPhoto(stored.StorageKey, stored.ThumbnailStorageKey),
                cancellationToken);
            created++;
        }
        return created;
    }

    private async Task<Dictionary<string, Guid>> CategoryIdsAsync(CancellationToken cancellationToken)
    {
        await using var db = await catalogFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().ToDictionaryAsync(category => category.Slug, category => category.Id,
            StringComparer.Ordinal, cancellationToken);
    }

    private async Task<Dictionary<string, DemoStockItem>> ExistingDemoItemsAsync(CancellationToken cancellationToken)
    {
        await using var db = await retailFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.StockItems.AsNoTracking().Include(item => item.Units)
            .Where(item => item.InternalNote != null && item.InternalNote.StartsWith(PreviewDemoDefinitions.StockNotePrefix))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(item => item.InternalNote!,
            item => new DemoStockItem(item.Id, item.Photos.Count, item.Units.Select(unit => unit.Id).ToList()),
            StringComparer.Ordinal);
    }

    private sealed record DemoStockItem(Guid Id, int Photos, List<Guid> UnitIds);

    private async Task AddStockPhotosAsync(Guid itemId, string seedKey, int count, CancellationToken cancellationToken)
    {
        for (var index = 0; index < count; index++)
        {
            var upload = PreviewInitializationService.CreatePlaceholderUpload(
                seedKey.GetHashCode(StringComparison.Ordinal) + index);
            var stored = await mediaStorage.SaveImageAsync(itemId, Guid.CreateVersion7(), upload, null, cancellationToken);
            await StockItems.AddPhotoAsync(itemId, new StockItemPhoto(stored.StorageKey, stored.ThumbnailStorageKey),
                cancellationToken);
        }
    }
}

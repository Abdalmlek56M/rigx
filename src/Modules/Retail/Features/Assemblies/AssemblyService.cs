using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Builds (assemblies): create from available units, edit the build card (usage, sell mode, warranty, copies,
/// "suitable for", photos), allocation percentages, publish, disassemble and the storefront list.
/// Part changes live in <see cref="AssemblyPartService"/>.
/// </summary>
public sealed class AssemblyService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AssemblyView>> ListAssembliesAsync(bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Assemblies.AsNoTracking().WithUnits()
            .OrderByDescending(a => a.UpdatedAtUtc).ToListAsync(cancellationToken);
        return rows.Select(assembly => AssemblyMapping.ToView(assembly, includeCosts)).ToList();
    }

    public async Task<AssemblyView> GetAsync(Guid id, bool includeCosts, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await db.Assemblies.AsNoTracking().WithUnits().SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Assembly was not found.");
        return AssemblyMapping.ToView(assembly, includeCosts);
    }

    public async Task<AssemblyView> CreateAssemblyAsync(AssemblyCreateRequest request, bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new RetailException(400, "Assembly name is required.");
        var unitIds = request.UnitIds?.Distinct().ToList() ?? [];
        // A draft may start empty: its parts can be new pieces that are not in stock yet (round 3, slice J); publishing still needs parts.
        if (unitIds.Count > AssemblyRules.MaxParts)
            throw new RetailException(400, $"An assembly can have at most {AssemblyRules.MaxParts} units.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var assembly = new Assembly
            {
                Id = Guid.CreateVersion7(), Code = await db.NextCodeAsync(CodeSequence.Assembly, PublicCode.Build, cancellationToken),
                Name = request.Name.Trim(), Status = AssemblyStatus.Draft, CreatedAtUtc = now, UpdatedAtUtc = now
            };
            var claimed = await db.Units.Include(unit => unit.StockItem)
                .Where(unit => unitIds.Contains(unit.Id) && unit.Status == UnitStatus.Available)
                .ToListAsync(cancellationToken);
            if (claimed.Count != unitIds.Count) throw new RetailException(409, "Some selected units are no longer available.");
            foreach (var unit in claimed) AssemblyEditing.Link(assembly, unit, 0m);
            AssemblyEditing.ResetAllocations(assembly);
            db.Assemblies.Add(assembly);
            await AssemblyEditing.SaveAsync(db, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AssemblyMapping.ToView(assembly, includeCosts);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    /// <summary>Replaces the build card. A warranty label (resolved from the settings list) applies to every part without an override.</summary>
    public async Task<AssemblyView> UpdateAssemblyAsync(Guid id, AssemblyUpdateRequest request, bool includeCosts = true,
        CancellationToken cancellationToken = default)
    {
        if (request.SalePrice < 0) throw new RetailException(400, "Sale price cannot be negative.");
        var usage = AssemblyUsage.Parse(request.Usage);
        var sellMode = AssemblySellMode.Parse(request.SellMode);
        StockItemRules.ValidateWarranty(request.WarrantyLabel, request.WarrantyMonths);
        AssemblyRules.ValidateCopies(request.Copies);
        var suitableForAr = AssemblyRules.CleanSuitableFor(request.SuitableForAr);
        var suitableForEn = AssemblyRules.CleanSuitableFor(request.SuitableForEn);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await AssemblyEditing.LoadEditableAsync(db, id, request.RowVersion, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Name)) assembly.Name = request.Name.Trim();
        assembly.PublicTitleAr = request.PublicTitleAr?.Trim(); assembly.PublicTitleEn = request.PublicTitleEn?.Trim();
        assembly.SalePrice = request.SalePrice;
        assembly.Usage = usage;
        assembly.SellMode = sellMode;
        assembly.WarrantyLabel = string.IsNullOrWhiteSpace(request.WarrantyLabel) ? null : request.WarrantyLabel.Trim();
        assembly.WarrantyMonths = assembly.WarrantyLabel is null ? null : request.WarrantyMonths;
        assembly.Copies = request.Copies;
        assembly.SuitableForAr = suitableForAr;
        assembly.SuitableForEn = suitableForEn;
        AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
        await AssemblyEditing.SaveAsync(db, cancellationToken);
        return AssemblyMapping.ToView(assembly, includeCosts);
    }

    public async Task<AssemblyView> SetAssemblyAllocationsAsync(Guid id, AssemblyAllocationRequest request, bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await AssemblyEditing.LoadEditableAsync(db, id, request.RowVersion, cancellationToken);
        var inputs = request.Allocations?.ToList() ?? [];
        if (inputs.Count != assembly.Units.Count || inputs.Select(item => item.UnitId).Distinct().Count() != inputs.Count)
            throw new RetailException(400, "Provide one allocation row for every component.");
        if (inputs.Any(item => item.Percent is < 0 or > 100))
            throw new RetailException(400, "Each allocation percent must be between 0 and 100.");
        if (!AllocationMath.SumsToHundred(inputs.Select(item => item.Percent)))
            throw new RetailException(400, "Allocation percentages must sum to 100.");
        foreach (var link in assembly.Units)
        {
            var input = inputs.SingleOrDefault(item => item.UnitId == link.UnitId)
                ?? throw new RetailException(400, "An allocation row refers to a unit outside this assembly.");
            link.AllocationPercent = decimal.Round(input.Percent, 2);
        }
        AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
        await AssemblyEditing.SaveAsync(db, cancellationToken);
        return AssemblyMapping.ToView(assembly, includeCosts);
    }

    public async Task<AssemblyView> SetAssemblyPublishedAsync(Guid id, bool published, long rowVersion, bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await AssemblyEditing.LoadEditableAsync(db, id, rowVersion, cancellationToken);
        if (published) AssemblyRules.EnsureNoPlannedParts(assembly);
        if (published && assembly.SalePrice <= 0) throw new RetailException(400, "A sale price is required before publishing.");
        if (published && (assembly.Units.Count == 0 || !AllocationMath.SumsToHundred(assembly.Units.Select(link => link.AllocationPercent))))
            throw new RetailException(400, "Component allocations must sum to 100 before publishing.");
        assembly.Status = published ? AssemblyStatus.Published : AssemblyStatus.Draft;
        AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
        await AssemblyEditing.SaveAsync(db, cancellationToken);
        return AssemblyMapping.ToView(assembly, includeCosts);
    }

    public async Task<AssemblyView> DisassembleAsync(Guid id, long rowVersion, bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assembly = await AssemblyEditing.LoadEditableAsync(db, id, rowVersion, cancellationToken);
            foreach (var link in assembly.Units.ToList())
            {
                link.Unit.Status = UnitStatus.Available; link.Unit.AssemblyId = null; link.Unit.RowVersion++;
                AssemblyEditing.Unlink(db, assembly, link);
            }
            assembly.Status = AssemblyStatus.Disassembled;
            AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
            await AssemblyEditing.SaveAsync(db, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AssemblyMapping.ToView(assembly, includeCosts);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<IReadOnlyList<StockItemPhoto>> AddPhotoAsync(Guid id, StockItemPhoto photo, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await LoadForPhotosAsync(db, id, cancellationToken);
        if (assembly.Photos.Count >= StockItemRules.MaxPhotos)
            throw new RetailException(409, $"A build can have at most {StockItemRules.MaxPhotos} photos.");
        assembly.Photos = [.. assembly.Photos, photo];
        AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
        await AssemblyEditing.SaveAsync(db, cancellationToken);
        return assembly.Photos;
    }

    public async Task<IReadOnlyList<OwnedPhoto>> ListPhotosAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Assemblies.AsNoTracking().Select(build => new { build.Id, build.Photos }).ToListAsync(cancellationToken);
        return rows.SelectMany(row => row.Photos.Select(photo => new OwnedPhoto(row.Id, photo))).ToList();
    }

    /// <summary>Swaps one photo for a re-rendered one in place; false when the old photo is no longer on the build (removed meanwhile).</summary>
    public async Task<bool> ReplacePhotoAsync(Guid id, string oldStorageKey, StockItemPhoto replacement, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await db.Assemblies.SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        var index = assembly?.Photos.FindIndex(entry => entry.StorageKey == oldStorageKey) ?? -1;
        if (assembly is null || index < 0) return false;
        assembly.Photos = assembly.Photos.Select((entry, position) => position == index ? replacement : entry).ToList();
        AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
        await AssemblyEditing.SaveAsync(db, cancellationToken);
        return true;
    }

    /// <summary>Removes the photo from the build and returns it, so the caller can delete the stored files.</summary>
    public async Task<StockItemPhoto> RemovePhotoAsync(Guid id, string storageKey, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await LoadForPhotosAsync(db, id, cancellationToken);
        var photo = assembly.Photos.FirstOrDefault(entry => entry.StorageKey == storageKey)
            ?? throw new RetailException(404, "The photo was not found.");
        assembly.Photos = assembly.Photos.Where(entry => entry != photo).ToList();
        AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
        await AssemblyEditing.SaveAsync(db, cancellationToken);
        return photo;
    }

    /// <summary>Published builds for the storefront, filtered by usage and budget (sale price range).</summary>
    public async Task<IReadOnlyList<AssemblyPublicView>> ListPublishedAssembliesAsync(AssemblyStoreQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        query ??= new AssemblyStoreQuery();
        var usage = AssemblyUsage.Parse(query.Usage);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var builds = db.Assemblies.AsNoTracking().WithUnits().Where(a => a.Status == AssemblyStatus.Published);
        if (usage is not null) builds = builds.Where(a => a.Usage == usage);
        if (query.MinPrice is decimal min) builds = builds.Where(a => a.SalePrice >= min);
        if (query.MaxPrice is decimal max) builds = builds.Where(a => a.SalePrice <= max);
        var rows = await builds.OrderByDescending(a => a.UpdatedAtUtc).AsSplitQuery().ToListAsync(cancellationToken);
        return rows.Select(AssemblyMapping.ToPublicView).ToList();
    }

    /// <summary>One published build by its public code (<c>BLD-07</c>); unknown, draft or sold builds are 404.</summary>
    public async Task<AssemblyPublicView> GetPublishedAssemblyAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!PublicCode.TryNormalize(code, PublicCode.Build, out var normalized)) throw BuildNotFound();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await db.Assemblies.AsNoTracking().WithUnits().AsSplitQuery()
            .SingleOrDefaultAsync(a => a.Status == AssemblyStatus.Published && a.Code == normalized, cancellationToken) ?? throw BuildNotFound();
        return AssemblyMapping.ToPublicView(build);
    }

    private static RetailException BuildNotFound() => new(404, "The build was not found.");

    private static async Task<Assembly> LoadForPhotosAsync(RetailDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var assembly = await db.Assemblies.SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Assembly was not found.");
        AssemblyRules.EnsureEditable(assembly);
        return assembly;
    }
}

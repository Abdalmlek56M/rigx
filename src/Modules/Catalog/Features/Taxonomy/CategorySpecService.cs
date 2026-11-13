using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>A category's spec schema: edit its own definitions, read the inherited result, check item values against it.</summary>
public sealed class CategorySpecService(IDbContextFactory<CatalogDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<CategoryView> SetSpecsAsync(Guid categoryId, IReadOnlyList<SpecDefinition>? specs, int? rowVersion,
        CancellationToken cancellationToken = default)
    {
        var normalized = CategorySpecSchema.Normalize(specs);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken)
            ?? throw new CatalogNotFoundException("Category was not found.");
        CatalogValidation.EnsureVersion(category.RowVersion, rowVersion, categoryId);
        category.SpecDefinitions = normalized.ToList();
        category.RowVersion++;
        category.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        await db.SaveChangesAsync(cancellationToken);
        return category.ToView(await CategoryHierarchy.IsEffectivelyActiveAsync(db, category.Id, cancellationToken));
    }

    public async Task<IReadOnlyList<SpecDefinition>> GetEffectiveSpecsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var nodes = await db.Categories.AsNoTracking()
            .Select(category => new { category.Id, category.ParentId, category.SpecDefinitions })
            .ToDictionaryAsync(category => category.Id, cancellationToken);
        if (!nodes.ContainsKey(categoryId)) throw new CatalogNotFoundException("Category was not found.");
        var chain = new List<IReadOnlyList<SpecDefinition>>();
        var visited = new HashSet<Guid>();
        Guid? current = categoryId;
        while (current is Guid id && visited.Add(id) && nodes.TryGetValue(id, out var node))
        {
            chain.Add(node.SpecDefinitions);
            current = node.ParentId;
        }
        chain.Reverse();
        return CategorySpecSchema.Effective(chain);
    }

    /// <summary>No category means free-form specs; otherwise values are checked and cleaned against the category's schema.</summary>
    public async Task<IReadOnlyDictionary<string, string>> CheckValuesAsync(Guid? categoryId, IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default) =>
        categoryId is Guid id
            ? CategorySpecSchema.CheckValues(await GetEffectiveSpecsAsync(id, cancellationToken), values)
            : values;
}

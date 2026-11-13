using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;

/// <summary>
/// The builder's view of the category tree: which categories feed each slot (a "show in builder" category whose code is the slot,
/// plus its sub-categories), which spec keys drive compatibility, and which categories decide the in-build price.
/// </summary>
public sealed class BuilderCatalog
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<Guid>> categoriesBySlot;
    private readonly IReadOnlyDictionary<Guid, string> slotByCategory;

    private BuilderCatalog(IReadOnlyList<BuilderSlotView> slots, IReadOnlyDictionary<string, IReadOnlyList<Guid>> categoriesBySlot,
        IReadOnlyDictionary<Guid, string> slotByCategory, IReadOnlySet<string> compatibilityKeys)
    {
        Slots = slots;
        this.categoriesBySlot = categoriesBySlot;
        this.slotByCategory = slotByCategory;
        CompatibilityKeys = compatibilityKeys;
    }

    /// <summary>Only the slots that have categories, in display order.</summary>
    public IReadOnlyList<BuilderSlotView> Slots { get; }

    public IReadOnlySet<string> CompatibilityKeys { get; }

    public BuildPriceRule BuildPriceRule =>
        new(CategoryIdsOf(BuilderSlot.Cpu).ToHashSet(), CategoryIdsOf(BuilderSlot.Board).ToHashSet());

    public IReadOnlyList<Guid> CategoryIdsOf(string slot) => categoriesBySlot.GetValueOrDefault(slot) ?? [];

    public string? SlotOf(Guid? categoryId) => categoryId is Guid id ? slotByCategory.GetValueOrDefault(id) : null;

    public static BuilderCatalog From(IReadOnlyList<CategoryView> categories)
    {
        var byId = categories.ToDictionary(category => category.Id);
        var slots = new List<BuilderSlotView>();
        var categoriesBySlot = new Dictionary<string, IReadOnlyList<Guid>>();
        var slotByCategory = new Dictionary<Guid, string>();
        foreach (var slot in BuilderSlot.All)
        {
            var roots = categories.Where(category => category.IsEffectivelyActive && category.ShowInBuilder && category.Code == slot).ToList();
            if (roots.Count == 0) continue;
            var ids = roots.SelectMany(root => Subtree(root.Id, categories)).Distinct().ToList();
            slots.Add(new BuilderSlotView(slot, roots[0].Name, ids));
            categoriesBySlot[slot] = ids;
            foreach (var id in ids) slotByCategory.TryAdd(id, slot);
        }
        var keys = slotByCategory.Keys
            .SelectMany(id => CategorySpecSchema.Effective(Chain(id, byId).Select(category => category.Specs)))
            .Where(spec => spec.IsCompatibility).Select(spec => spec.Key).ToHashSet(StringComparer.Ordinal);
        return new BuilderCatalog(slots, categoriesBySlot, slotByCategory, keys);
    }

    /// <summary>The category and every active descendant.</summary>
    private static IEnumerable<Guid> Subtree(Guid rootId, IReadOnlyList<CategoryView> categories)
    {
        var found = new List<Guid> { rootId };
        for (var index = 0; index < found.Count; index++)
            found.AddRange(categories.Where(category => category.ParentId == found[index] && category.IsEffectivelyActive).Select(category => category.Id));
        return found;
    }

    /// <summary>Root → leaf chain of a category.</summary>
    private static IEnumerable<CategoryView> Chain(Guid id, IReadOnlyDictionary<Guid, CategoryView> byId)
    {
        var chain = new List<CategoryView>();
        for (var current = byId.GetValueOrDefault(id); current is not null; current = current.ParentId is Guid parent ? byId.GetValueOrDefault(parent) : null)
            chain.Insert(0, current);
        return chain;
    }
}

/// <summary>Builds the <see cref="BuilderCatalog"/> from the live category tree, once per use.</summary>
public sealed class BuilderCatalogLoader(TaxonomyService taxonomy)
{
    public async Task<BuilderCatalog> LoadAsync(CancellationToken cancellationToken) =>
        BuilderCatalog.From(await taxonomy.GetCategoriesAsync(cancellationToken));

    /// <summary>The in-build price rule for checkout and quotes; the category tree is read only when a line asks for that price.</summary>
    public async Task<BuildPriceRule?> BuildPriceRuleForAsync(IReadOnlyList<OrderLineRequest>? lines, CancellationToken cancellationToken) =>
        lines?.Any(line => line.PriceOption?.Trim() == OrderPriceOption.InBuild) == true
            ? (await LoadAsync(cancellationToken)).BuildPriceRule
            : null;
}

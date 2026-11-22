namespace HardwareStore.Modules.Catalog;

/// <summary>
/// The sale prices a category really offers. A category's own switches only turn prices off, and a child follows its parent:
/// a price is offered when the category and every category above it offer it. When that leaves nothing, the category's own switches apply,
/// so an item always has a price to sell at.
/// </summary>
public static class CategoryPriceRules
{
    public static CategoryPriceOptions Combine(CategoryPriceOptions own, CategoryPriceOptions? parentEffective)
    {
        if (parentEffective is null) return own;
        var combined = new CategoryPriceOptions(own.Alone && parentEffective.Alone, own.WithBoard && parentEffective.WithBoard, own.InBuild && parentEffective.InBuild);
        return combined.Any ? combined : own;
    }

    /// <summary>The effective switches of every category in the list, by category id (a parent missing from the list counts as the root).</summary>
    public static IReadOnlyDictionary<Guid, CategoryPriceOptions> Effective(IReadOnlyCollection<CategoryView> categories)
    {
        var byId = categories.ToDictionary(category => category.Id);
        var effective = new Dictionary<Guid, CategoryPriceOptions>();

        CategoryPriceOptions Of(CategoryView category)
        {
            if (effective.TryGetValue(category.Id, out var known)) return known;
            var own = category.PriceOptions ?? new CategoryPriceOptions();
            var parent = category.ParentId is Guid parentId && byId.TryGetValue(parentId, out var parentCategory) ? Of(parentCategory) : null;
            return effective[category.Id] = Combine(own, parent);
        }

        foreach (var category in categories) Of(category);
        return effective;
    }
}

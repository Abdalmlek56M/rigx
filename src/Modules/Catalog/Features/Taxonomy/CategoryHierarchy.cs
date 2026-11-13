using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>Category tree rules: a category is visible only when it and all its parents are active.</summary>
internal static class CategoryHierarchy
{
    /// <summary>Ids of categories that are active and whose whole parent chain is active.</summary>
    public static async Task<Guid[]> GetEffectiveIdsAsync(
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var nodes = await db.Categories.AsNoTracking()
            .Select(category => new CatalogCategoryNode(
                category.Id, category.ParentId, category.IsActive))
            .ToListAsync(cancellationToken);
        var effective = CatalogCategoryPolicy.GetEffectivelyActiveIds(nodes);
        return effective.ToArray();
    }

    public static async Task<bool> IsEffectivelyActiveAsync(
        CatalogDbContext db,
        Guid categoryId,
        CancellationToken cancellationToken) =>
        (await GetEffectiveIdsAsync(db, cancellationToken)).Contains(categoryId);


    public static async Task<Guid[]> GetDescendantIdsAsync(
        CatalogDbContext db,
        Guid rootId,
        IReadOnlyCollection<Guid> effectiveCategoryIds,
        CancellationToken cancellationToken)
    {
        var edges = await db.Categories.AsNoTracking()
            .Where(category => effectiveCategoryIds.Contains(category.Id))
            .Select(category => new { category.Id, category.ParentId })
            .ToListAsync(cancellationToken);
        var result = new HashSet<Guid> { rootId };
        var pending = new Queue<Guid>();
        pending.Enqueue(rootId);
        while (pending.TryDequeue(out var parentId))
        {
            foreach (var child in edges.Where(item => item.ParentId == parentId))
            {
                if (result.Add(child.Id))
                    pending.Enqueue(child.Id);
            }
        }
        return result.ToArray();
    }

    public static async Task EnsureParentIsAcyclicAsync(
        CatalogDbContext db,
        Guid categoryId,
        Guid parentId,
        CancellationToken cancellationToken)
    {
        var parents = await db.Categories.AsNoTracking()
            .Select(category => new { category.Id, category.ParentId })
            .ToDictionaryAsync(category => category.Id, category => category.ParentId, cancellationToken);
        if (!parents.ContainsKey(parentId))
            throw new CatalogValidationException("parentId", "Parent category was not found.");
        var visited = new HashSet<Guid>();
        Guid? current = parentId;
        while (current is not null)
        {
            if (current == categoryId)
                throw new CatalogValidationException("parentId", "Category hierarchy cannot contain a cycle.");
            if (!visited.Add(current.Value) ||
                !parents.TryGetValue(current.Value, out var next))
                break;
            current = next;
        }
    }

    public static IReadOnlyList<CategoryView> BuildTree(IReadOnlyList<CategoryView> categories)
    {
        var childrenByParent = categories
            .Where(category => category.ParentId is not null)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        CategoryView Attach(CategoryView category) => category with
        {
            Children = childrenByParent.TryGetValue(category.Id, out var children)
                ? children.Select(Attach).ToList()
                : []
        };

        return categories.Where(category => category.ParentId is null).Select(Attach).ToList();
    }
}

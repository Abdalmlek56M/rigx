namespace HardwareStore.Modules.Catalog;

public sealed record CatalogCategoryNode(Guid Id, Guid? ParentId, bool IsActive);

public static class CatalogCategoryPolicy
{
    /// <summary>
    /// Publicly effective means the category and all ancestors are active. Cycles are treated as inactive.
    /// </summary>
    public static HashSet<Guid> GetEffectivelyActiveIds(IEnumerable<CatalogCategoryNode> nodes)
    {
        var byId = nodes.ToDictionary(node => node.Id);
        var resolved = new Dictionary<Guid, bool>();
        bool Resolve(Guid id, HashSet<Guid> path)
        {
            if (resolved.TryGetValue(id, out var result))
                return result;
            if (!byId.TryGetValue(id, out var node) || !path.Add(id))
                return false;
            result = node.IsActive &&
                     (node.ParentId is null || Resolve(node.ParentId.Value, path));
            path.Remove(id);
            resolved[id] = result;
            return result;
        }
        foreach (var id in byId.Keys)
            Resolve(id, []);
        return resolved.Where(pair => pair.Value).Select(pair => pair.Key).ToHashSet();
    }
}

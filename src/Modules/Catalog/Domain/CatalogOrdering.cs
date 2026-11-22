namespace HardwareStore.Modules.Catalog;

/// <summary>The one rule for putting brands or sibling categories in a hand-chosen order: the list names every record once, so a stale screen cannot scramble the rest.</summary>
public static class CatalogOrdering
{
    /// <summary>The ids as asked; 400 for a missing, empty or repeating list.</summary>
    public static IReadOnlyList<Guid> Require(IReadOnlyList<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
            throw new CatalogValidationException("ids", "The list to order is required.");
        if (ids.Distinct().Count() != ids.Count)
            throw new CatalogValidationException("ids", "The list to order repeats a record.");
        return ids;
    }

    /// <summary>Each id's position (0, 1, 2…) in the requested order.</summary>
    public static IReadOnlyDictionary<Guid, int> Positions(IReadOnlyList<Guid> order) =>
        order.Select((id, index) => (id, index)).ToDictionary(pair => pair.id, pair => pair.index);

    /// <summary>409 when the list is not exactly the records that exist now (one was added, hidden away or moved since the screen loaded).</summary>
    public static void EnsureSameRecords(IReadOnlyList<Guid> requested, IReadOnlyCollection<Guid> current)
    {
        if (requested.Count != current.Count || !current.All(requested.Contains))
            throw new CatalogConflictException("The list changed. Reload it and order again.");
    }
}

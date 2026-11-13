using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Search box for stock items, shared by the back office and the storefront: a public code ("rx-142") or any
/// spelling of a model ("rtx3060", "RTX 3060"). Serial numbers are matched only where the caller may see them.
/// A text like "rx 580" is both a possible code and a model, so both are matched.
/// </summary>
internal static class StockItemSearch
{
    public static IQueryable<StockItem> Matching(this IQueryable<StockItem> query, string? search, bool includeSerials)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        var key = ModelName.Key(search);
        var isCode = PublicCode.TryNormalize(search, PublicCode.StockItem, out var code);
        var serial = search.Trim();
        return query.Where(item =>
            (isCode && item.Code == code)
            || (key.Length > 0 && item.ModelKey.Contains(key))
            || (includeSerials && item.Units.Any(unit => unit.SerialNumber == serial)));
    }
}

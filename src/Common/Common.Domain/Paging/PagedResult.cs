namespace HardwareStore.Common.Domain.Paging;

/// <summary>
/// The one paged list shape every API list returns: <c>items</c>, 1-based <c>page</c>, <c>pageSize</c> and <c>total</c>
/// (all matching rows). Query parameters are <c>page</c> and <c>pageSize</c>; <see cref="PageRequest"/> normalizes them.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

/// <summary>Normalized paging input: page ≥ 1, 1 ≤ pageSize ≤ <see cref="MaxPageSize"/>.</summary>
public readonly record struct PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;

    public static PageRequest Of(int page, int pageSize, int maxPageSize = MaxPageSize) =>
        new(Math.Max(1, page), Math.Clamp(pageSize, 1, maxPageSize));
}

namespace HardwareStore.Modules.Retail;

/// <summary>
/// The sale prices one category offers: alone, with a board (bundle) and inside a build (the three <see cref="OrderPriceOption"/> values).
/// A price that is off is not shown on the website or at the POS, and an online order cannot pick it.
/// </summary>
public sealed record PriceOptionSet(bool Alone = true, bool WithBoard = true, bool InBuild = true)
{
    public static PriceOptionSet All { get; } = new();

    /// <summary>The offered options, in the order shown: alone, with a board, inside a build.</summary>
    public IReadOnlyList<string> Names { get; } = [
        .. Alone ? [OrderPriceOption.Alone] : Array.Empty<string>(),
        .. WithBoard ? [OrderPriceOption.WithBoard] : Array.Empty<string>(),
        .. InBuild ? [OrderPriceOption.InBuild] : Array.Empty<string>()
    ];

    /// <summary>What a customer gets when no option is named: alone when offered, else the next one on.</summary>
    public string Default => Names.Count > 0 ? Names[0] : OrderPriceOption.Alone;

    public bool Allows(string option) => Names.Contains(option);
}

/// <summary>The categories that switch some sale price off (read from Catalog in Host); every other category offers all three.</summary>
public sealed class PriceOptionRule(IReadOnlyDictionary<Guid, PriceOptionSet> restricted)
{
    public static PriceOptionRule Open { get; } = new(new Dictionary<Guid, PriceOptionSet>());

    public PriceOptionSet ForCategory(Guid? categoryId) =>
        categoryId is Guid id && restricted.TryGetValue(id, out var set) ? set : PriceOptionSet.All;
}

/// <summary>Where Retail reads the per-category price switches; Host implements it from Catalog (architecture decision 11).</summary>
public interface IPriceOptionSource
{
    Task<PriceOptionRule> LoadAsync(CancellationToken cancellationToken = default);
}

using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// "الشحن لكل المحافظات": governorate groups with their own fee and delivery time, and a default fee for
/// every governorate outside the groups. A governorate belongs to at most one group. Free shipping is off by default:
/// when the owner turns it on, an order whose total after discounts (before shipping) reaches the minimum ships for nothing.
/// </summary>
public sealed record ShippingSettings : IStoreSettingsSection<ShippingSettings>
{
    public static string Key => "shipping";
    public static ShippingSettings Read(StoreSettings settings) => settings.Shipping;
    public static void Write(StoreSettings settings, ShippingSettings value) => settings.Shipping = value;

    public string? CourierName { get; init; }
    public bool PickupEnabled { get; init; } = true;
    public decimal DefaultFee { get; init; }
    public string? DefaultDeliveryTime { get; init; }
    public IReadOnlyList<ShippingGroup> Groups { get; init; } = [];
    public bool FreeShippingEnabled { get; init; }
    /// <summary>The order total (after discounts, before shipping) from which shipping is free; meaningful only while free shipping is on.</summary>
    public decimal FreeShippingMinimum { get; init; }

    /// <summary>The fee to charge: zero when free shipping is on and <paramref name="merchandiseTotal"/> reaches the minimum, else <paramref name="fee"/>.</summary>
    public decimal FeeFor(decimal fee, decimal merchandiseTotal) =>
        FreeShippingEnabled && merchandiseTotal >= FreeShippingMinimum ? 0m : fee;

    /// <summary>How much more the order needs to ship free; null when free shipping is off or already reached.</summary>
    public decimal? FreeShippingRemaining(decimal merchandiseTotal) =>
        FreeShippingEnabled && merchandiseTotal < FreeShippingMinimum ? FreeShippingMinimum - merchandiseTotal : null;

    /// <summary>The quote for an order of <paramref name="merchandiseTotal"/>: free shipping applied, and what is missing to get it when the fee is not zero.</summary>
    public ShippingQuote QuoteFor(string governorateCode, decimal merchandiseTotal)
    {
        var quote = QuoteFor(governorateCode);
        if (quote.Fee <= 0m) return quote;
        var fee = FeeFor(quote.Fee, merchandiseTotal);
        return quote with { Fee = fee, FreeApplied = fee == 0m, FreeShippingRemaining = FreeShippingRemaining(merchandiseTotal) };
    }

    public ShippingQuote QuoteFor(string governorateCode)
    {
        if (!EgyptGovernorates.IsKnown(governorateCode))
            throw new StorefrontException(400, "Unknown governorate.");
        var group = Groups.FirstOrDefault(item => item.Governorates.Contains(governorateCode, StringComparer.Ordinal));
        return group is null
            ? new ShippingQuote(DefaultFee, DefaultDeliveryTime)
            : new ShippingQuote(group.Fee, group.DeliveryTime ?? DefaultDeliveryTime);
    }

    /// <summary>Every governorate with its fee and delivery time, for the public shipping page and the checkout dropdown.</summary>
    public IReadOnlyList<GovernorateShippingRate> Rates() => EgyptGovernorates.NamesByCode.Select(entry =>
    {
        var quote = QuoteFor(entry.Key);
        return new GovernorateShippingRate(entry.Key, entry.Value, quote.Fee, quote.DeliveryTime, EgyptGovernorates.EnglishNamesByCode[entry.Key]);
    }).ToList();

    public ShippingSettings Normalize()
    {
        var groups = (Groups ?? []).Select(group => group.Normalize()).ToArray();
        SettingsValues.EnsureUnique(groups.Select(group => group.Name), "Shipping group name");
        SettingsValues.EnsureUnique(groups.SelectMany(group => group.Governorates), "Governorate (a governorate can be in one group only)");
        var freeMinimum = SettingsValues.Money(FreeShippingMinimum, "Free shipping minimum");
        if (FreeShippingEnabled && freeMinimum <= 0m)
            throw new StorefrontException(400, "Free shipping needs a minimum order amount above zero.");
        return new ShippingSettings
        {
            CourierName = ContentText.Optional(CourierName, 120, "Courier name"),
            PickupEnabled = PickupEnabled,
            DefaultFee = SettingsValues.Money(DefaultFee, "Default shipping fee"),
            DefaultDeliveryTime = ContentText.Optional(DefaultDeliveryTime, 60, "Default delivery time"),
            Groups = groups,
            FreeShippingEnabled = FreeShippingEnabled,
            FreeShippingMinimum = freeMinimum
        };
    }
}

public sealed record ShippingGroup
{
    public string Name { get; init; } = string.Empty;
    public decimal Fee { get; init; }
    public string? DeliveryTime { get; init; }
    public IReadOnlyList<string> Governorates { get; init; } = [];

    internal ShippingGroup Normalize()
    {
        var governorates = (Governorates ?? []).Select(code => code?.Trim().ToUpperInvariant() ?? string.Empty).ToArray();
        if (governorates.Length == 0 || governorates.Any(code => !EgyptGovernorates.IsKnown(code)))
            throw new StorefrontException(400, "Each shipping group needs one or more known governorate codes.");
        return new ShippingGroup
        {
            Name = ContentText.Required(Name, 60, "Shipping group name"),
            Fee = SettingsValues.Money(Fee, "Shipping group fee"),
            DeliveryTime = ContentText.Optional(DeliveryTime, 60, "Delivery time"),
            Governorates = governorates
        };
    }
}

/// <summary><see cref="FreeApplied"/>: the fee was waived by the free-shipping rule; <see cref="FreeShippingRemaining"/>: what the order still lacks for it.</summary>
public sealed record ShippingQuote(decimal Fee, string? DeliveryTime, bool FreeApplied = false, decimal? FreeShippingRemaining = null);

public sealed record GovernorateShippingRate(string Code, string Name, decimal Fee, string? DeliveryTime, string NameEn);

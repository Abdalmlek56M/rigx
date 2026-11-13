using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// "الشحن لكل المحافظات": governorate groups with their own fee and delivery time, and a default fee for
/// every governorate outside the groups. A governorate belongs to at most one group.
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

    public ShippingQuote QuoteFor(string governorateCode)
    {
        if (!EgyptGovernorates.IsKnown(governorateCode))
            throw new StorefrontException(400, "Unknown governorate.");
        var group = Groups.FirstOrDefault(item => item.Governorates.Contains(governorateCode, StringComparer.Ordinal));
        return group is null
            ? new ShippingQuote(DefaultFee, DefaultDeliveryTime)
            : new ShippingQuote(group.Fee, group.DeliveryTime ?? DefaultDeliveryTime);
    }

    public ShippingSettings Normalize()
    {
        var groups = (Groups ?? []).Select(group => group.Normalize()).ToArray();
        SettingsValues.EnsureUnique(groups.Select(group => group.Name), "Shipping group name");
        SettingsValues.EnsureUnique(groups.SelectMany(group => group.Governorates), "Governorate (a governorate can be in one group only)");
        return new ShippingSettings
        {
            CourierName = ContentText.Optional(CourierName, 120, "Courier name"),
            PickupEnabled = PickupEnabled,
            DefaultFee = SettingsValues.Money(DefaultFee, "Default shipping fee"),
            DefaultDeliveryTime = ContentText.Optional(DefaultDeliveryTime, 60, "Default delivery time"),
            Groups = groups
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

public sealed record ShippingQuote(decimal Fee, string? DeliveryTime);

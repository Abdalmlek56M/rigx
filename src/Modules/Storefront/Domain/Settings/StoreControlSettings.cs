namespace HardwareStore.Modules.Storefront;

public static class HomepageSectionKey
{
    public static readonly IReadOnlyList<string> All =
        ["hero", "categories", "brands", "offers", "discovery", "guide", "latest", "closing"];
}

public sealed record HomepageSection
{
    public string Key { get; init; } = string.Empty;
    public bool Visible { get; init; } = true;
}

/// <summary>Switches that change what the storefront shows, plus the homepage section order.</summary>
public sealed record StoreControlSettings : IStoreSettingsSection<StoreControlSettings>
{
    public static string Key => "controls";
    public static StoreControlSettings Read(StoreSettings settings) => settings.Controls;
    public static void Write(StoreSettings settings, StoreControlSettings value) => settings.Controls = value;

    public bool MaintenanceMode { get; init; }
    public string? AnnouncementText { get; init; }
    public bool ShowSoldOut { get; init; } = true;
    public bool ShowOfferCountdown { get; init; } = true;
    public bool GuestCheckoutEnabled { get; init; }
    public decimal MinimumOrderAmount { get; init; }
    public IReadOnlyList<HomepageSection> HomepageSections { get; init; } =
        HomepageSectionKey.All.Select(key => new HomepageSection { Key = key }).ToArray();

    public StoreControlSettings Normalize()
    {
        var sections = (HomepageSections ?? []).ToArray();
        var keys = sections.Select(section => section.Key).ToArray();
        if (keys.Length != HomepageSectionKey.All.Count ||
            !keys.Order(StringComparer.Ordinal).SequenceEqual(HomepageSectionKey.All.Order(StringComparer.Ordinal)))
            throw new StorefrontException(400,
                $"Homepage sections must list each of these once: {string.Join(", ", HomepageSectionKey.All)}.");
        return this with
        {
            AnnouncementText = ContentText.Optional(AnnouncementText, 200, "Announcement"),
            MinimumOrderAmount = SettingsValues.Money(MinimumOrderAmount, "Minimum order amount"),
            HomepageSections = sections
        };
    }
}

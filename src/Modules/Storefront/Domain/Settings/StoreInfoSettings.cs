namespace HardwareStore.Modules.Storefront;

/// <summary>Store identity and pickup details. Empty optional values are hidden by the UI.</summary>
public sealed record StoreInfoSettings : IStoreSettingsSection<StoreInfoSettings>
{
    public static string Key => "store-info";
    public static StoreInfoSettings Read(StoreSettings settings) => settings.StoreInfo;
    public static void Write(StoreSettings settings, StoreInfoSettings value) => settings.StoreInfo = value;

    public string Name { get; init; } = "RIGX Hardware";
    public string? Domain { get; init; } = "rigxhardware.com";
    public string? Email { get; init; } = "rigxhardware@gmail.com";
    public string? WhatsAppHours { get; init; }
    public string? PickupAddress { get; init; }
    public string? PickupHours { get; init; }
    public string? PickupInstructions { get; init; }
    public string? WarrantySummary { get; init; }
    public string? ServiceSummary { get; init; }
    public string? ConsultationSummary { get; init; }

    public StoreInfoSettings Normalize() => new()
    {
        Name = ContentText.Required(Name, 120, "Store name"),
        Domain = SettingsValues.OptionalDomain(Domain, "Domain"),
        Email = SettingsValues.OptionalEmail(Email, "Store email"),
        WhatsAppHours = ContentText.Optional(WhatsAppHours, 255, "WhatsApp hours"),
        PickupAddress = ContentText.Optional(PickupAddress, 500, "Pickup address"),
        PickupHours = ContentText.Optional(PickupHours, 255, "Pickup hours"),
        PickupInstructions = ContentText.Optional(PickupInstructions, 1000, "Pickup instructions"),
        WarrantySummary = ContentText.Optional(WarrantySummary, 1000, "Warranty summary"),
        ServiceSummary = ContentText.Optional(ServiceSummary, 1000, "Service summary"),
        ConsultationSummary = ContentText.Optional(ConsultationSummary, 1000, "Consultation summary")
    };
}

public static class ContactPlatform
{
    public const string WhatsApp = "WhatsApp";
    public const string Phone = "Phone";
    public const string Telegram = "Telegram";
    public const string Facebook = "Facebook";
    public const string Instagram = "Instagram";
    public const string TikTok = "TikTok";
    public const string YouTube = "YouTube";

    public static readonly IReadOnlyList<string> All = [WhatsApp, Phone, Telegram, Facebook, Instagram, TikTok, YouTube];

    /// <summary>WhatsApp and phone links hold a number (international digits); the rest hold a URL.</summary>
    public static bool IsNumber(string platform) => platform is WhatsApp or Phone;
}

/// <summary>One contact/social link. List order is display order.</summary>
public sealed record ContactLink
{
    public string Platform { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public bool ShowOnSite { get; init; } = true;
    public bool ShowOnInvoice { get; init; }
    public bool ShowInFloatingButton { get; init; }

    internal ContactLink Normalize()
    {
        var platform = SettingsValues.OneOf(Platform, ContactPlatform.All, "Link platform");
        return this with
        {
            Platform = platform,
            Value = ContactPlatform.IsNumber(platform)
                ? SettingsValues.PhoneDigits(Value, $"{platform} number")
                : SettingsValues.HttpUrl(Value, $"{platform} link")
        };
    }
}

public sealed record ContactLinkSettings : IStoreSettingsSection<ContactLinkSettings>
{
    public const int MaxLinks = 20;
    private const string StoreNumber = "201289186840";

    public static string Key => "contact-links";
    public static ContactLinkSettings Read(StoreSettings settings) => settings.ContactLinks;
    public static void Write(StoreSettings settings, ContactLinkSettings value) => settings.ContactLinks = value;

    public IReadOnlyList<ContactLink> Links { get; init; } =
    [
        new() { Platform = ContactPlatform.WhatsApp, Value = StoreNumber, ShowOnInvoice = true, ShowInFloatingButton = true },
        new() { Platform = ContactPlatform.Phone, Value = StoreNumber, ShowOnInvoice = true, ShowInFloatingButton = true }
    ];

    public ContactLinkSettings Normalize()
    {
        var links = (Links ?? []).Select(link => link.Normalize()).ToArray();
        if (links.Length > MaxLinks)
            throw new StorefrontException(400, $"At most {MaxLinks} contact links are allowed.");
        SettingsValues.EnsureUnique(links.Select(link => $"{link.Platform}|{link.Value}"), "Contact link");
        return new ContactLinkSettings { Links = links };
    }
}

using System.Text.RegularExpressions;

namespace HardwareStore.Modules.Storefront;

public static class WatermarkPosition
{
    public static readonly IReadOnlyList<string> All = ["TopLeft", "TopRight", "BottomLeft", "BottomRight", "Center"];
}

/// <summary>Watermark applied to item/build photos on upload (2.9). Off until the owner turns it on.</summary>
public sealed record WatermarkSettings : IStoreSettingsSection<WatermarkSettings>
{
    public static string Key => "watermark";
    public static WatermarkSettings Read(StoreSettings settings) => settings.Watermark;
    public static void Write(StoreSettings settings, WatermarkSettings value) => settings.Watermark = value;

    public bool Enabled { get; init; }
    public string Position { get; init; } = "BottomRight";
    public int OpacityPercent { get; init; } = 40;
    public int SizePercent { get; init; } = 20;
    public bool ApplyToArticles { get; init; }

    public WatermarkSettings Normalize() => this with
    {
        Position = SettingsValues.OneOf(Position, WatermarkPosition.All, "Watermark position"),
        OpacityPercent = SettingsValues.Range(OpacityPercent, 5, 100, "Watermark opacity"),
        SizePercent = SettingsValues.Range(SizePercent, 5, 50, "Watermark size")
    };
}

/// <summary>Reviews are off by default; stars show only from a minimum number of approved reviews.</summary>
public sealed record ReviewSettings : IStoreSettingsSection<ReviewSettings>
{
    public static string Key => "reviews";
    public static ReviewSettings Read(StoreSettings settings) => settings.Reviews;
    public static void Write(StoreSettings settings, ReviewSettings value) => settings.Reviews = value;

    public bool Enabled { get; init; }
    public int MinimumCountToShowStars { get; init; } = 3;

    public ReviewSettings Normalize() => this with
    {
        MinimumCountToShowStars = SettingsValues.Range(MinimumCountToShowStars, 1, 50, "Minimum reviews to show stars")
    };
}

public sealed record LanguageSettings : IStoreSettingsSection<LanguageSettings>
{
    public static readonly IReadOnlyList<string> Supported = ["ar", "en"];

    public static string Key => "language";
    public static LanguageSettings Read(StoreSettings settings) => settings.Language;
    public static void Write(StoreSettings settings, LanguageSettings value) => settings.Language = value;

    public string DefaultLanguage { get; init; } = "ar";
    public bool EnglishEnabled { get; init; } = true;

    public LanguageSettings Normalize()
    {
        var language = SettingsValues.OneOf(DefaultLanguage, Supported, "Default language");
        if (language == "en" && !EnglishEnabled)
            throw new StorefrontException(400, "English must be enabled to be the default language.");
        return this with { DefaultLanguage = language };
    }
}

/// <summary>Online invoice look: number prefix (INV-YYYY-NNNN), background logo, warranty QR, terms line.</summary>
public sealed partial record InvoiceSettings : IStoreSettingsSection<InvoiceSettings>
{
    public static string Key => "invoice";
    public static InvoiceSettings Read(StoreSettings settings) => settings.Invoice;
    public static void Write(StoreSettings settings, InvoiceSettings value) => settings.Invoice = value;

    public string NumberPrefix { get; init; } = "INV";
    public bool ShowBackgroundLogo { get; init; } = true;
    public bool ShowWarrantyQr { get; init; } = true;
    public string? TermsLine { get; init; }

    public InvoiceSettings Normalize()
    {
        var prefix = NumberPrefix?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!PrefixPattern().IsMatch(prefix))
            throw new StorefrontException(400, "Invoice prefix must be 2 to 6 English letters.");
        return this with { NumberPrefix = prefix, TermsLine = ContentText.Optional(TermsLine, 300, "Invoice terms line") };
    }

    [GeneratedRegex("^[A-Z]{2,6}$", RegexOptions.CultureInvariant)]
    private static partial Regex PrefixPattern();
}

/// <summary>"عرفتنا منين؟" choices (owner decision D5). Orders keep the chosen text.</summary>
public sealed record LeadSourceSettings : IStoreSettingsSection<LeadSourceSettings>
{
    public static string Key => "lead-sources";
    public static LeadSourceSettings Read(StoreSettings settings) => settings.LeadSources;
    public static void Write(StoreSettings settings, LeadSourceSettings value) => settings.LeadSources = value;

    public IReadOnlyList<string> Sources { get; init; } =
        ["فيسبوك", "Marketplace", "إنستجرام", "تيك توك", "يوتيوب", "تليجرام", "جوجل", "صاحبي رشحكم", "حاجة تانية"];

    public LeadSourceSettings Normalize() => new() { Sources = SettingsValues.Labels(Sources, 30, "Lead source") };
}

/// <summary>Warranty choices (owner decision D6). <c>Months</c> is empty for the manufacturer's warranty.</summary>
public sealed record WarrantyOption
{
    public string Label { get; init; } = string.Empty;
    public int? Months { get; init; }
}

public sealed record WarrantyOptionSettings : IStoreSettingsSection<WarrantyOptionSettings>
{
    public static string Key => "warranty-options";
    public static WarrantyOptionSettings Read(StoreSettings settings) => settings.WarrantyOptions;
    public static void Write(StoreSettings settings, WarrantyOptionSettings value) => settings.WarrantyOptions = value;

    public IReadOnlyList<WarrantyOption> Options { get; init; } =
    [
        new() { Label = "شهر", Months = 1 },
        new() { Label = "٣ شهور", Months = 3 },
        new() { Label = "سنة", Months = 12 },
        new() { Label = "ضمان الوكيل" }
    ];

    public WarrantyOptionSettings Normalize()
    {
        var options = (Options ?? []).ToArray();
        var labels = SettingsValues.Labels(options.Select(option => option.Label).ToArray(), 20, "Warranty option");
        return new WarrantyOptionSettings
        {
            Options = options.Select((option, index) => new WarrantyOption
            {
                Label = labels[index],
                Months = option.Months is null ? null : SettingsValues.Range(option.Months.Value, 1, 120, "Warranty months")
            }).ToArray()
        };
    }
}

/// <summary>
/// Orders and POS: how long a confirmed order holds its stock and what happens after (owner decision, 13 Nov 2026:
/// 24 hours, notify only), the POS discount limit for staff without the price-override permission, and the public
/// "اتباع قبل كده" page (governorate shown only when switched on).
/// </summary>
public sealed record SalesSettings : IStoreSettingsSection<SalesSettings>
{
    public static string Key => "sales";
    public static SalesSettings Read(StoreSettings settings) => settings.Sales;
    public static void Write(StoreSettings settings, SalesSettings value) => settings.Sales = value;

    public int ReservationHours { get; init; } = 24;
    public bool ReservationAutoRelease { get; init; }
    public decimal PosMaxDiscountPercent { get; init; } = 10m;
    public bool SoldFeedEnabled { get; init; } = true;
    public bool SoldFeedShowGovernorate { get; init; }

    public SalesSettings Normalize()
    {
        if (PosMaxDiscountPercent is < 0 or > 100 || decimal.Round(PosMaxDiscountPercent, 2) != PosMaxDiscountPercent)
            throw new StorefrontException(400, "POS discount limit must be between 0 and 100 with at most 2 decimals.");
        return this with { ReservationHours = SettingsValues.Range(ReservationHours, 1, 720, "Reservation hours") };
    }
}

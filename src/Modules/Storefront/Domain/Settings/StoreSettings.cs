namespace HardwareStore.Modules.Storefront;

/// <summary>
/// The single store settings row. Each section is one JSON column, replaced as a whole and validated by
/// its own <c>Normalize</c>. Defaults live in the section records, so a missing value reads as the default.
/// </summary>
public sealed class StoreSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public StoreInfoSettings StoreInfo { get; set; } = new();
    public ContactLinkSettings ContactLinks { get; set; } = new();
    public ShippingSettings Shipping { get; set; } = new();
    public PaymentSettings Payment { get; set; } = new();
    public WatermarkSettings Watermark { get; set; } = new();
    public StoreControlSettings Controls { get; set; } = new();
    public WhatsAppTemplateSettings WhatsAppTemplates { get; set; } = new();
    public ReviewSettings Reviews { get; set; } = new();
    public LanguageSettings Language { get; set; } = new();
    public InvoiceSettings Invoice { get; set; } = new();
    public LeadSourceSettings LeadSources { get; set; } = new();
    public WarrantyOptionSettings WarrantyOptions { get; set; } = new();
    public SalesSettings Sales { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

/// <summary>One settings section: a URL key, how it is read from/written to the row, and its validation.</summary>
public interface IStoreSettingsSection<TSelf> where TSelf : class, IStoreSettingsSection<TSelf>
{
    static abstract string Key { get; }
    static abstract TSelf Read(StoreSettings settings);
    static abstract void Write(StoreSettings settings, TSelf value);

    /// <summary>Returns the trimmed, canonical value or throws a 400 <see cref="StorefrontException"/>.</summary>
    TSelf Normalize();
}

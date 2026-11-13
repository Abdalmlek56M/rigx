namespace HardwareStore.Modules.Storefront;

/// <summary>Every settings section, for the admin settings screen.</summary>
public sealed record StoreSettingsAdminView(
    StoreInfoSettings StoreInfo,
    ContactLinkSettings ContactLinks,
    ShippingSettings Shipping,
    PaymentSettings Payment,
    WatermarkSettings Watermark,
    StoreControlSettings Controls,
    WhatsAppTemplateSettings WhatsAppTemplates,
    ReviewSettings Reviews,
    LanguageSettings Language,
    InvoiceSettings Invoice,
    LeadSourceSettings LeadSources,
    WarrantyOptionSettings WarrantyOptions,
    long RowVersion);

/// <summary>What the storefront needs: only links shown on the site; no watermark, invoice or warranty-list settings.</summary>
public sealed record StoreSettingsPublicView(
    StoreInfoSettings StoreInfo,
    IReadOnlyList<ContactLink> ContactLinks,
    ShippingSettings Shipping,
    PaymentSettings Payment,
    StoreControlSettings Controls,
    WhatsAppTemplateSettings WhatsAppTemplates,
    ReviewSettings Reviews,
    LanguageSettings Language,
    IReadOnlyList<string> LeadSources);

/// <summary>Replaces one section; <see cref="RowVersion"/> is the settings row version the admin edited.</summary>
public sealed record StoreSettingsSectionUpdate<T>(T Value, long RowVersion);

/// <summary>Shipping fee and what must be paid before the order ships.</summary>
public sealed record CheckoutQuoteView(
    decimal ShippingFee,
    string? DeliveryTime,
    decimal UpfrontShipping,
    decimal Deposit,
    decimal UpfrontTotal);

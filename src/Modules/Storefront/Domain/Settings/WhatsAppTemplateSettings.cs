using System.Text.RegularExpressions;

namespace HardwareStore.Modules.Storefront;

/// <summary>Placeholders a WhatsApp template may use; the page fills them with the customer/item data it has.</summary>
public static class WhatsAppPlaceholder
{
    public const string Name = "{الاسم}";
    public const string Phone = "{الموبايل}";
    public const string Item = "{القطعة}";
    public const string Code = "{الكود}";
    public const string Price = "{السعر}";
    public const string Link = "{اللينك}";
    public const string Order = "{الطلب}";

    public static readonly IReadOnlyList<string> All = [Name, Phone, Item, Code, Price, Link, Order];
}

/// <summary>Prepared WhatsApp (wa.me) messages, one per place they are sent from.</summary>
public sealed partial record WhatsAppTemplateSettings : IStoreSettingsSection<WhatsAppTemplateSettings>
{
    public static string Key => "whatsapp-templates";
    public static WhatsAppTemplateSettings Read(StoreSettings settings) => settings.WhatsAppTemplates;
    public static void Write(StoreSettings settings, WhatsAppTemplateSettings value) => settings.WhatsAppTemplates = value;

    public string General { get; init; } = "مرحبًا RIGX، لديّ استفسار.";
    public string Product { get; init; } = "مرحبًا RIGX، أود الاستفسار عن {القطعة} ({الكود}) بسعر {السعر}.\n{اللينك}";
    public string Build { get; init; } = "مرحبًا RIGX، أود الاستفسار عن التجميعة {القطعة} ({الكود}) بسعر {السعر}.\n{اللينك}";
    public string Order { get; init; } = "مرحبًا RIGX، بخصوص طلبي رقم {الطلب}.";
    public string Warranty { get; init; } = "مرحبًا RIGX، لديّ مشكلة في {القطعة} ({الكود}) وعليها ضمان.";
    public string RequestReply { get; init; } = "مرحبًا {الاسم}، بخصوص طلبك لدى RIGX: ";
    public string BackInStock { get; init; } = "مرحبًا {الاسم}، القطعة التي طلبتها ({القطعة}) توفرت لدينا في RIGX.\n{اللينك}";

    public WhatsAppTemplateSettings Normalize() => new()
    {
        General = Template(General, "General template"),
        Product = Template(Product, "Product template"),
        Build = Template(Build, "Build template"),
        Order = Template(Order, "Order template"),
        Warranty = Template(Warranty, "Warranty template"),
        RequestReply = Template(RequestReply, "Request reply template"),
        BackInStock = Template(BackInStock, "Back in stock template")
    };

    private static string Template(string? value, string label)
    {
        var text = ContentText.Required(value, 500, label);
        if (Placeholders().Matches(text).Any(match => !WhatsAppPlaceholder.All.Contains(match.Value, StringComparer.Ordinal)))
            throw new StorefrontException(400,
                $"{label} uses an unknown placeholder. Allowed: {string.Join(" ", WhatsAppPlaceholder.All)}.");
        return text;
    }

    [GeneratedRegex("\\{[^{}]*\\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholders();
}

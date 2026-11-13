using System.Text.RegularExpressions;
using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>Fills a WhatsApp template with a request's data and builds the wa.me link that opens it.</summary>
public static partial class WhatsAppReply
{
    public static WhatsAppReplyView For(CustomerRequest request, string template)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [WhatsAppPlaceholder.Name] = request.Name,
            [WhatsAppPlaceholder.Phone] = request.Phone,
            [WhatsAppPlaceholder.Item] = request.ModelName ?? request.ItemCode ?? string.Empty,
            [WhatsAppPlaceholder.Code] = request.Code,
            [WhatsAppPlaceholder.Price] = request.OfferAmount?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
        var text = Fill(template, values);
        return new WhatsAppReplyView(request.Phone, text, Url(request.Phone, text));
    }

    /// <summary>Replaces each placeholder with its value; unknown or missing ones become empty.</summary>
    public static string Fill(string template, IReadOnlyDictionary<string, string> values) =>
        Placeholders().Replace(template, match => values.GetValueOrDefault(match.Value) ?? string.Empty);

    public static string Url(string phone, string text) =>
        $"https://wa.me/{EgyptMobile.International(phone)}?text={Uri.EscapeDataString(text)}";

    [GeneratedRegex(@"\{[^{}]*\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholders();
}

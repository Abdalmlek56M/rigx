namespace HardwareStore.Modules.Retail;

/// <summary>
/// What the public tracking link (/t/{token}, no login) shows. The link is the customer's proof, so it keeps working from any device,
/// but it never shows the whole phone number, and some time after delivery it shows only where the order stands.
/// </summary>
public static class TrackingRules
{
    /// <summary>
    /// Days after delivery during which the link still shows the details (items, amounts, courier, address); after that only the status.
    /// The one place to change it; <c>null</c> means the details never expire.
    /// </summary>
    public static readonly int? DetailsVisibleDays = 90;

    private const int VisiblePrefix = 3;
    private const int VisibleSuffix = 2;

    /// <summary>"01012345678" → "010••••••78": enough for the customer to recognise it, never the whole number.</summary>
    public static string MaskPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length <= VisiblePrefix + VisibleSuffix) return new string('•', Math.Max(digits.Length, 4));
        return digits[..VisiblePrefix] + new string('•', digits.Length - VisiblePrefix - VisibleSuffix) + digits[^VisibleSuffix..];
    }

    /// <summary>True when the order was delivered more than <see cref="DetailsVisibleDays"/> ago (the day count is whole days after the delivery time).</summary>
    public static bool DetailsExpired(DateTimeOffset? deliveredAtUtc, DateTimeOffset now, int? visibleDays) =>
        visibleDays is { } days && deliveredAtUtc is { } delivered && now >= delivered.AddDays(days);
}

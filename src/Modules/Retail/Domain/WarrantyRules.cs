namespace HardwareStore.Modules.Retail;

/// <summary>What a public warranty search text means.</summary>
public enum WarrantyLookupKind { Code, OrderReference, Phone, Serial }

/// <summary>Warranty rules: end date, computed status, public search parsing, phone masking and voiding.</summary>
public static class WarrantyRules
{
    private const int MaxQueryLength = 64;
    private const int MinSerialLength = 4;
    private const string QueryHint = "Enter a phone number, order number or item code.";

    public static DateTimeOffset? EndsAt(DateTimeOffset start, int? months) => months is null ? null : start.AddMonths(months.Value);

    public static string Status(Warranty warranty, DateTimeOffset now)
    {
        if (warranty.VoidedAtUtc is not null) return WarrantyStatus.Void;
        return warranty.EndsAtUtc is null || warranty.EndsAtUtc > now ? WarrantyStatus.Active : WarrantyStatus.Expired;
    }

    /// <summary>Whole days left (rounded up, never negative); null when void or the warranty has no end date.</summary>
    public static int? DaysLeft(Warranty warranty, DateTimeOffset now) =>
        warranty.VoidedAtUtc is not null || warranty.EndsAtUtc is null
            ? null
            : Math.Max(0, (int)Math.Ceiling((warranty.EndsAtUtc.Value - now).TotalDays));

    /// <summary>Shows only the last three digits of a phone number.</summary>
    public static string MaskPhone(string digits) =>
        digits.Length <= 3 ? digits : new string('*', digits.Length - 3) + digits[^3..];

    /// <summary>
    /// Reads the public search text: RX- is an item code, RIGX- an order reference (stands in for the invoice number
    /// until invoices exist), all digits a phone number, anything else a serial number.
    /// </summary>
    public static (WarrantyLookupKind Kind, string Value) ParseLookup(string? query)
    {
        var text = query?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > MaxQueryLength) throw new RetailException(400, QueryHint);
        var upper = text.ToUpperInvariant();
        if (upper.StartsWith("RX-", StringComparison.Ordinal)) return (WarrantyLookupKind.Code, upper);
        if (upper.StartsWith("RIGX-", StringComparison.Ordinal)) return (WarrantyLookupKind.OrderReference, upper);
        var stripped = text.Where(c => c is not (' ' or '-' or '+' or '(' or ')')).ToArray();
        if (stripped.Length > 0 && stripped.All(char.IsAsciiDigit)) return (WarrantyLookupKind.Phone, OrderRules.Phone(text));
        if (text.Length < MinSerialLength) throw new RetailException(400, QueryHint);
        return (WarrantyLookupKind.Serial, text);
    }

    public static void Void(Warranty warranty, string? reason, Guid? actorId, DateTimeOffset now)
    {
        if (warranty.VoidedAtUtc is not null) throw new RetailException(409, "The warranty is already void.");
        warranty.VoidReason = OrderRules.Required(reason, 500, "Void reason");
        warranty.VoidedAtUtc = now; warranty.VoidedByUserAccountId = actorId; warranty.RowVersion++;
    }
}

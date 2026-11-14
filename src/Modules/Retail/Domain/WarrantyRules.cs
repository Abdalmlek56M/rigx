namespace HardwareStore.Modules.Retail;

/// <summary>What a public warranty search text means.</summary>
public enum WarrantyLookupKind { Code, OrderReference, InvoiceNumber, Phone, Serial }

/// <summary>Warranty rules: end date, computed status, public search parsing, phone masking and voiding.</summary>
public static class WarrantyRules
{
    private const int MaxQueryLength = 64;
    private const int MinSerialLength = 4;
    public const int MaxExtensionMonths = 36;
    private const string QueryHint = "Enter a phone number, invoice number or item code.";

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
    /// Reads the public search text: an invoice number (INV-2026-0001), RX- an item code, RIGX- an order reference
    /// (staff use), all digits a phone number, anything else a serial number.
    /// </summary>
    public static (WarrantyLookupKind Kind, string Value) ParseLookup(string? query)
    {
        var text = query?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > MaxQueryLength) throw new RetailException(400, QueryHint);
        var upper = text.ToUpperInvariant();
        if (InvoiceRules.TryParseNumber(text, out var invoiceNumber)) return (WarrantyLookupKind.InvoiceNumber, invoiceNumber);
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

    /// <summary>Adds months to the end date of a warranty that has one; the new end must still be in the future.</summary>
    public static WarrantyExtension Extend(Warranty warranty, int months, string? reason, Guid actorId, DateTimeOffset now)
    {
        if (warranty.VoidedAtUtc is not null) throw new RetailException(409, "A void warranty cannot be extended.");
        if (warranty.EndsAtUtc is not { } previousEnd) throw new RetailException(409, "A manufacturer warranty has no end date to extend.");
        if (months is < 1 or > MaxExtensionMonths) throw new RetailException(400, $"Months must be between 1 and {MaxExtensionMonths}.");
        var text = OrderRules.Required(reason, 500, "Reason");
        var newEnd = previousEnd.AddMonths(months);
        if (newEnd <= now) throw new RetailException(400, "The extended warranty must end in the future.");
        warranty.EndsAtUtc = newEnd; warranty.RowVersion++;
        return new WarrantyExtension
        {
            Id = Guid.CreateVersion7(), WarrantyId = warranty.Id, Months = months, PreviousEndUtc = previousEnd, NewEndUtc = newEnd,
            Reason = text, ActorUserAccountId = actorId, CreatedAtUtc = now
        };
    }
}

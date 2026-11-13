using System.Globalization;
using System.Text.RegularExpressions;

namespace HardwareStore.Modules.Retail;

/// <summary>Invoice rules: number format and parsing, kind and status (computed from the order), access token.</summary>
public static partial class InvoiceRules
{
    private const int TokenBytes = 32;

    /// <summary>"INV-2026-0001": the sequence width is a minimum, so a 5-digit sequence is still valid.</summary>
    public static string FormatNumber(string prefix, int year, int sequence) =>
        string.Create(CultureInfo.InvariantCulture, $"{prefix}-{year}-{sequence:0000}");

    /// <summary>Reads an invoice number typed by a person (case and spaces ignored). Order references (RIGX-20261113-…) never match.</summary>
    public static bool TryParseNumber(string? text, out string number)
    {
        number = text?.Trim().ToUpperInvariant() ?? string.Empty;
        return NumberPattern().IsMatch(number);
    }

    public static string Kind(StoreOrder order) => Kind(order.Items.Any(item => item.AssemblyId is not null));

    public static string Kind(bool hasBuildLine) => hasBuildLine ? InvoiceKind.Build : InvoiceKind.Parts;

    public static string Status(string orderStatus) => orderStatus == OrderStatus.Cancelled ? InvoiceStatus.Void : InvoiceStatus.Issued;

    public static string NewAccessToken() => OrderRules.NewToken(TokenBytes);

    /// <summary>The year the sequence restarts in (UTC year of the issue time).</summary>
    public static int YearOf(DateTimeOffset issuedAtUtc) => issuedAtUtc.UtcDateTime.Year;

    [GeneratedRegex(@"^[A-Z]{2,6}-\d{4}-\d{4,9}$", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();
}

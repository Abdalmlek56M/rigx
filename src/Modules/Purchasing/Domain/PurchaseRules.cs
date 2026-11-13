using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Purchasing;

/// <summary>Expected Purchasing business failure; mapped to ProblemDetails by the global handler.</summary>
public sealed class PurchasingException(int statusCode, string message) : AppException(statusCode, message);

/// <summary>Who we bought from: a person, a trader, or a company.</summary>
public static class SellerKind
{
    public const string Person = "Person";
    public const string Trader = "Trader";
    public const string Company = "Company";
    public static readonly IReadOnlyList<string> All = [Person, Trader, Company];
}

public static class PurchaseStatus
{
    public const string Draft = "Draft";
    public const string Received = "Received";
    public const string Cancelled = "Cancelled";
}

/// <summary>Parts bought one by one, or a whole PC (split into parts on receipt, optionally kept together as a build).</summary>
public static class PurchaseKind
{
    public const string Parts = "Parts";
    public const string FullPc = "FullPc";
}

/// <summary>How the seller is paid. Trade-in = the seller takes items from us as (part of) the payment.</summary>
public static class SellerPaymentMethod
{
    public const string Cash = "Cash";
    public const string InstaPay = "InstaPay";
    public const string Wallet = "Wallet";
    public const string TradeIn = "TradeIn";
    public const string CashAndTradeIn = "CashAndTradeIn";
    public static readonly IReadOnlyList<string> All = [Cash, InstaPay, Wallet, TradeIn, CashAndTradeIn];
}

/// <summary>Money side of a purchase: total owed to the seller, paid in money, given as trade-in, and what is left.</summary>
public sealed record SellerPayment(string Method, decimal PaidAmount, decimal TradeInValue, string? TradeInNote);

public static class PurchaseRules
{
    public const int MaxLines = 100;

    /// <summary>Egyptian mobile in one form (01xxxxxxxxx): spaces, dashes and a +20 / 0020 prefix are removed.</summary>
    public static string NormalizePhone(string? phone) =>
        EgyptMobile.TryNormalize(phone, out var mobile)
            ? mobile
            : throw new PurchasingException(400, "Enter an Egyptian mobile number (01xxxxxxxxx).");

    public static string ParseSellerKind(string? kind) =>
        SellerKind.All.FirstOrDefault(value => string.Equals(value, kind?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new PurchasingException(400, "Seller kind must be Person, Trader or Company.");

    public static string? ParseGovernorate(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var clean = code.Trim().ToUpperInvariant();
        return EgyptGovernorates.IsKnown(clean) ? clean : throw new PurchasingException(400, "Unknown governorate.");
    }

    /// <summary>A full PC needs at least two parts; only a full PC can be kept together as a build.</summary>
    public static void CheckShape(string kind, int lineCount, bool keepAsBuild)
    {
        if (kind is not (PurchaseKind.Parts or PurchaseKind.FullPc)) throw new PurchasingException(400, "Purchase kind must be Parts or FullPc.");
        if (lineCount is < 1 or > MaxLines) throw new PurchasingException(400, $"A purchase needs between 1 and {MaxLines} lines.");
        if (kind == PurchaseKind.FullPc && lineCount < 2) throw new PurchasingException(400, "A full PC purchase needs at least two parts.");
        if (keepAsBuild && kind != PurchaseKind.FullPc) throw new PurchasingException(400, "Only a full PC purchase can be kept as a build.");
    }

    /// <summary>Line amounts: quantity 1–1000, cost and suggested price not negative.</summary>
    public static decimal LineTotal(int quantity, decimal unitCost, decimal price)
    {
        if (quantity is < 1 or > 1000) throw new PurchasingException(400, "Quantity must be between 1 and 1000.");
        if (unitCost < 0 || price < 0) throw new PurchasingException(400, "Cost and price cannot be negative.");
        return decimal.Round(quantity * unitCost, 2);
    }

    /// <summary>
    /// Checks the payment against the method and the total: money-only methods have no trade-in, trade-in has no money,
    /// cash + trade-in has both; paid + trade-in never exceeds the total. Returns the remaining amount owed to the seller.
    /// </summary>
    public static decimal Remaining(decimal total, SellerPayment payment)
    {
        var method = SellerPaymentMethod.All.FirstOrDefault(value => value == payment.Method)
            ?? throw new PurchasingException(400, "Unknown payment method.");
        if (payment.PaidAmount < 0 || payment.TradeInValue < 0) throw new PurchasingException(400, "Payment amounts cannot be negative.");
        var hasTradeIn = payment.TradeInValue > 0;
        switch (method)
        {
            case SellerPaymentMethod.TradeIn when !hasTradeIn || payment.PaidAmount > 0:
                throw new PurchasingException(400, "A trade-in payment needs a trade-in value and no money paid.");
            case SellerPaymentMethod.CashAndTradeIn when !hasTradeIn || payment.PaidAmount == 0:
                throw new PurchasingException(400, "Cash + trade-in needs both a cash amount and a trade-in value.");
            case SellerPaymentMethod.Cash or SellerPaymentMethod.InstaPay or SellerPaymentMethod.Wallet when hasTradeIn:
                throw new PurchasingException(400, "Only trade-in methods can have a trade-in value.");
        }
        if (hasTradeIn && string.IsNullOrWhiteSpace(payment.TradeInNote))
            throw new PurchasingException(400, "Write what the seller took as trade-in.");
        var remaining = total - payment.PaidAmount - payment.TradeInValue;
        if (remaining < 0) throw new PurchasingException(400, "Paid amount and trade-in exceed the purchase total.");
        return decimal.Round(remaining, 2);
    }
}

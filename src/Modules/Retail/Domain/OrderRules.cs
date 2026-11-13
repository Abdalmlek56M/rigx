using System.Security.Cryptography;
using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Retail;

/// <summary>"هتستخدمه في إيه؟" on an order: the build usages plus "upgrade my PC" and a free-text "other".</summary>
public static class OrderUsage
{
    public const string Upgrade = "Upgrade";
    public const string Other = "Other";

    private static readonly string[] All =
        [AssemblyUsage.Gaming, AssemblyUsage.Editing, AssemblyUsage.StudyWork, AssemblyUsage.Streaming, Upgrade, Other];

    /// <summary>Optional answer; "Other" needs its text, the note is dropped for every other answer.</summary>
    public static (string? Usage, string? Note) Parse(string? value, string? note)
    {
        if (string.IsNullOrWhiteSpace(value)) return (null, null);
        var usage = All.FirstOrDefault(entry => entry == value.Trim())
            ?? throw new RetailException(400, "Usage must be Gaming, Editing, StudyWork, Streaming, Upgrade or Other.");
        if (usage != Other) return (usage, null);
        var text = OrderRules.Optional(note, 200, "Usage note")
            ?? throw new RetailException(400, "Write the usage when choosing Other.");
        return (usage, text);
    }
}

/// <summary>Structured Egyptian delivery address (governorate code from <see cref="EgyptGovernorates"/>).</summary>
public sealed record OrderAddress(string? Governorate, string? Area, string? Street, string? Building, string? Landmark);

/// <summary>Order rules shared by checkout, POS, the status workflow and the public tracking page (business §5).</summary>
public static class OrderRules
{
    public const int MaxLines = 50;
    public const int MaxReservationHours = 720;

    public static string Required(string? value, int maxLength, string label)
    {
        var text = Optional(value, maxLength, label);
        return text ?? throw new RetailException(400, $"{label} is required.");
    }

    public static string? Optional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length > maxLength) throw new RetailException(400, $"{label} must be at most {maxLength} characters.");
        return text;
    }

    /// <summary>
    /// Phone numbers are kept as digits only (8–15), and an Egyptian mobile in international form (+20 / 0020) is stored
    /// in its local form (01…), so blacklist, customer search and warranty lookup match any spelling.
    /// </summary>
    public static string Phone(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 14 && digits.StartsWith("00201", StringComparison.Ordinal)) digits = digits[3..];
        if (digits.Length == 12 && digits.StartsWith("201", StringComparison.Ordinal)) digits = "0" + digits[2..];
        if (digits.Length is < 8 or > 15) throw new RetailException(400, "Enter a valid phone number.");
        return digits;
    }

    public static string? Email(string? value)
    {
        var email = Optional(value, 256, "Email")?.ToLowerInvariant();
        if (email is not null && (email.Contains(' ') || email.IndexOf('@') is < 1 || email.EndsWith('@')))
            throw new RetailException(400, "Enter a valid email address.");
        return email;
    }

    public static string Fulfillment(string? value) => value?.Trim() switch
    {
        OrderFulfillment.Delivery => OrderFulfillment.Delivery,
        OrderFulfillment.Pickup => OrderFulfillment.Pickup,
        _ => throw new RetailException(400, "Fulfillment must be Delivery or Pickup.")
    };

    /// <summary>Delivery needs governorate, area and street; pickup keeps no address.</summary>
    public static OrderAddress? Address(OrderAddress? address, string fulfillment)
    {
        if (fulfillment == OrderFulfillment.Pickup) return null;
        var governorate = address?.Governorate?.Trim().ToUpperInvariant();
        if (governorate is null || !EgyptGovernorates.IsKnown(governorate))
            throw new RetailException(400, "Choose the governorate for delivery.");
        return new OrderAddress(governorate, Required(address!.Area, 120, "Area"), Required(address.Street, 200, "Street"),
            Optional(address.Building, 120, "Building, floor and flat"), Optional(address.Landmark, 200, "Landmark"));
    }

    /// <summary>Normalized address used to match the blacklist (case, spacing and punctuation ignored).</summary>
    public static string? AddressKey(string? governorate, string? area, string? street, string? building)
    {
        if (governorate is null || area is null || street is null) return null;
        var text = string.Join('|', governorate, area, street, building ?? string.Empty).ToLowerInvariant();
        return new string(text.Where(ch => char.IsLetterOrDigit(ch) || ch == '|').ToArray());
    }

    /// <summary>Online orders pay cash on delivery, by InstaPay or by wallet; the store also takes cash and bank transfer.</summary>
    public static string PaymentMethod(string? value, string channel)
    {
        var method = value?.Trim();
        var allowed = channel == OrderChannel.Online
            ? method is OrderPaymentMethod.CashOnDelivery or OrderPaymentMethod.InstaPay or OrderPaymentMethod.Wallet
            : method is OrderPaymentMethod.Cash or OrderPaymentMethod.InstaPay or OrderPaymentMethod.Wallet or OrderPaymentMethod.BankTransfer;
        return allowed ? method! : throw new RetailException(400, channel == OrderChannel.Online
            ? "Payment must be CashOnDelivery, InstaPay or Wallet."
            : "Payment must be Cash, InstaPay, Wallet or BankTransfer.");
    }

    /// <summary>
    /// New → Confirmed → Preparing → Shipped → Delivered; a pickup order goes Preparing → Delivered.
    /// Any open order may be cancelled; Completed has its own action.
    /// </summary>
    public static bool CanMove(StoreOrder order, string next) => order.Status switch
    {
        OrderStatus.New => next is OrderStatus.Confirmed or OrderStatus.Cancelled,
        OrderStatus.Confirmed => next is OrderStatus.Preparing or OrderStatus.Cancelled,
        OrderStatus.Preparing => next == OrderStatus.Cancelled ||
            next == (order.FulfillmentMethod == OrderFulfillment.Delivery ? OrderStatus.Shipped : OrderStatus.Delivered),
        OrderStatus.Shipped => next is OrderStatus.Delivered or OrderStatus.Cancelled,
        OrderStatus.Delivered => next == OrderStatus.Cancelled,
        _ => false
    };

    /// <summary>Stock is held for the order only while it waits to leave the store.</summary>
    public static bool HoldsReservation(string status) => status is OrderStatus.Confirmed or OrderStatus.Preparing;

    public static void ValidateReservationHours(int hours)
    {
        if (hours is < 1 or > MaxReservationHours)
            throw new RetailException(400, $"Reservation must be between 1 and {MaxReservationHours} hours.");
    }

    /// <summary>A blacklisted customer's order is confirmed only after full prepayment.</summary>
    public static void EnsureCanConfirm(StoreOrder order)
    {
        if (order.IsBlacklisted && order.PaymentStatus != PaymentStatus.Paid)
            throw new RetailException(409, "This customer is on the blacklist: confirm the order only after full prepayment.");
    }

    public static decimal Remaining(StoreOrder order) =>
        order.PaymentStatus is PaymentStatus.Paid or PaymentStatus.Waived ? 0m : Math.Max(0m, order.Total - order.PaidAmount);

    public static void ValidatePayment(decimal amount)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new RetailException(400, "A payment must be a positive amount with at most 2 decimals.");
    }

    public static void ValidateRating(int score)
    {
        if (score is < 1 or > 5) throw new RetailException(400, "Rating must be from 1 to 5.");
    }

    /// <summary>
    /// POS discount limit: <paramref name="unlimited"/> (owner/manager permission) skips it; otherwise the total discount
    /// may not exceed <paramref name="maxPercent"/> of the shown prices.
    /// </summary>
    public static void EnsureDiscountAllowed(decimal shownTotal, decimal discount, decimal maxPercent, bool unlimited)
    {
        if (unlimited || discount <= 0) return;
        if (discount > decimal.Round(shownTotal * maxPercent / 100m, 2))
            throw new RetailException(403, $"The discount is above your limit ({maxPercent:0.##}% of the shown price).");
    }

    /// <summary>Unguessable token for the public tracking link (/t/{token}).</summary>
    public static string NewTrackingToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

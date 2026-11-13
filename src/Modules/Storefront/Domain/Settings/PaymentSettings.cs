namespace HardwareStore.Modules.Storefront;

/// <summary>
/// Payment methods and the upfront rule: shipping is paid upfront, plus a deposit (default 10%) when the
/// order is above the threshold (default 10,000 EGP). Pickup from the store needs nothing upfront.
/// </summary>
public sealed record PaymentSettings : IStoreSettingsSection<PaymentSettings>
{
    public static string Key => "payment";
    public static PaymentSettings Read(StoreSettings settings) => settings.Payment;
    public static void Write(StoreSettings settings, PaymentSettings value) => settings.Payment = value;

    public decimal DepositThreshold { get; init; } = 10_000m;
    public decimal DepositPercent { get; init; } = 10m;
    public bool ShippingPaidUpfront { get; init; } = true;
    public bool CashOnDeliveryEnabled { get; init; } = true;
    public bool InstaPayEnabled { get; init; }
    public string? InstaPayAddress { get; init; }
    public bool WalletEnabled { get; init; }
    public string? WalletNumber { get; init; }

    public decimal DepositFor(decimal subtotal) =>
        subtotal > DepositThreshold
            ? decimal.Round(subtotal * DepositPercent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

    public UpfrontPayment UpfrontFor(decimal subtotal, decimal shippingFee, bool pickup)
    {
        if (pickup)
            return new UpfrontPayment(0m, 0m, 0m);
        var shipping = ShippingPaidUpfront ? shippingFee : 0m;
        var deposit = DepositFor(subtotal);
        return new UpfrontPayment(shipping, deposit, shipping + deposit);
    }

    public PaymentSettings Normalize()
    {
        if (DepositPercent is < 0 or > 100 || decimal.Round(DepositPercent, 2) != DepositPercent)
            throw new StorefrontException(400, "Deposit percent must be between 0 and 100 with at most 2 decimals.");
        if (!CashOnDeliveryEnabled && !InstaPayEnabled && !WalletEnabled)
            throw new StorefrontException(400, "At least one payment method must be enabled.");
        var instaPay = ContentText.Optional(InstaPayAddress, 100, "InstaPay address");
        var wallet = SettingsValues.OptionalPhoneDigits(WalletNumber, "Wallet number");
        if (InstaPayEnabled && instaPay is null)
            throw new StorefrontException(400, "InstaPay needs an InstaPay address before it can be enabled.");
        if (WalletEnabled && wallet is null)
            throw new StorefrontException(400, "Wallet payment needs a wallet number before it can be enabled.");
        return this with
        {
            DepositThreshold = SettingsValues.Money(DepositThreshold, "Deposit threshold"),
            InstaPayAddress = instaPay,
            WalletNumber = wallet
        };
    }
}

public sealed record UpfrontPayment(decimal Shipping, decimal Deposit, decimal Total);

namespace HardwareStore.Modules.Retail;

public static class MoneyRules
{
    public const int MaxNameLength = 60;

    /// <summary>Account kind a payment method lands in: no method or cash on delivery is cash; anything unknown is refused.</summary>
    public static string KindForMethod(string? method) => method?.Trim() switch
    {
        null or "" or OrderPaymentMethod.Cash or OrderPaymentMethod.CashOnDelivery => MoneyAccountKind.Cash,
        OrderPaymentMethod.InstaPay => MoneyAccountKind.InstaPay,
        OrderPaymentMethod.Wallet => MoneyAccountKind.Wallet,
        OrderPaymentMethod.BankTransfer => MoneyAccountKind.BankTransfer,
        _ => throw new RetailException(400, "The payment method must be Cash, InstaPay, Wallet or BankTransfer.")
    };

    public static string Kind(string? value) => value?.Trim() switch
    {
        MoneyAccountKind.Cash => MoneyAccountKind.Cash,
        MoneyAccountKind.InstaPay => MoneyAccountKind.InstaPay,
        MoneyAccountKind.Wallet => MoneyAccountKind.Wallet,
        MoneyAccountKind.BankTransfer => MoneyAccountKind.BankTransfer,
        _ => throw new RetailException(400, "The account kind must be Cash, InstaPay, Wallet or BankTransfer.")
    };

    public static string Name(string? value) => OrderRules.Required(value, MaxNameLength, "Account name");

    public static decimal Opening(decimal value)
    {
        var rounded = decimal.Round(value, 2);
        return rounded >= 0 ? rounded : throw new RetailException(400, "The opening balance cannot be negative.");
    }

    public static decimal Amount(decimal value)
    {
        var rounded = decimal.Round(value, 2);
        return rounded > 0 ? rounded : throw new RetailException(400, "The amount must be greater than zero.");
    }

    public static void EnsureCanPay(decimal balance, decimal amount)
    {
        if (amount > balance) throw new RetailException(409, "The account balance is not enough.");
    }
}

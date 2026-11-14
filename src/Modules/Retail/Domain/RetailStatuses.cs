namespace HardwareStore.Modules.Retail;

// Stored status/option values (persisted as strings; never rename a value without a migration).
public static class UnitStatus
{
    public const string Available = "Available";
    public const string InAssembly = "InAssembly";
    public const string Sold = "Sold";
    public const string PendingInspection = "PendingInspection";
    public const string Scrapped = "Scrapped";
    public const string Returned = "Returned";
}
public static class AssemblyStatus { public const string Draft = "Draft"; public const string Published = "Published"; public const string Sold = "Sold"; public const string Disassembled = "Disassembled"; }
public static class OrderStatus { public const string New = "New"; public const string Confirmed = "Confirmed"; public const string Preparing = "Preparing"; public const string Shipped = "Shipped"; public const string Delivered = "Delivered"; public const string Completed = "Completed"; public const string Cancelled = "Cancelled"; }
public static class PaymentStatus { public const string Unpaid = "Unpaid"; public const string PendingVerification = "PendingVerification"; public const string Paid = "Paid"; public const string Refunded = "Refunded"; public const string Waived = "Waived"; }
public static class EarningStatus { public const string Outstanding = "Outstanding"; public const string Paid = "Paid"; public const string Reversed = "Reversed"; }
public static class ReturnStatus { public const string PendingInspection = "PendingInspection"; public const string Inspected = "Inspected"; public const string Rejected = "Rejected"; }
public static class AdjustmentStatus { public const string Open = "Open"; public const string Applied = "Applied"; }
/// <summary>Where the order was taken: the website checkout or the store (POS).</summary>
public static class OrderChannel { public const string Online = "Online"; public const string Store = "Store"; }
public static class OrderFulfillment { public const string Delivery = "Delivery"; public const string Pickup = "Pickup"; }
public static class OrderPaymentMethod
{
    public const string Cash = "Cash";
    public const string CashOnDelivery = "CashOnDelivery";
    public const string InstaPay = "InstaPay";
    public const string Wallet = "Wallet";
    public const string BankTransfer = "BankTransfer";
    /// <summary>Order paid with more than one method (POS).</summary>
    public const string Split = "Split";
}
/// <summary>Computed warranty state (never stored): Void when voided, Expired once the end date passed.</summary>
public static class WarrantyStatus { public const string Active = "Active"; public const string Expired = "Expired"; public const string Void = "Void"; }
/// <summary>What an invoice documents: a whole build ("تجميعة كاملة") or single parts ("قطع منفردة").</summary>
public static class InvoiceKind { public const string Build = "Build"; public const string Parts = "Parts"; }
/// <summary>Computed invoice state (never stored): Void once its order is cancelled.</summary>
public static class InvoiceStatus { public const string Issued = "Issued"; public const string Void = "Void"; }
/// <summary>Kind of a money account (where store money sits).</summary>
public static class MoneyAccountKind
{
    public const string Cash = "Cash";
    public const string InstaPay = "InstaPay";
    public const string Wallet = "Wallet";
    public const string BankTransfer = "BankTransfer";
}
/// <summary>What produced a money movement; with its source id it is unique, so a posting can never be repeated.</summary>
public static class MoneySource
{
    public const string Sale = "Sale";
    public const string Refund = "Refund";
    public const string Purchase = "Purchase";
    public const string Expense = "Expense";
    public const string PartnerPayout = "PartnerPayout";
    public const string CapitalDeposit = "CapitalDeposit";
    public const string CapitalWithdrawal = "CapitalWithdrawal";
    public const string TransferIn = "TransferIn";
    public const string TransferOut = "TransferOut";
}
/// <summary>Method stored on a partner settlement that was an expense paid from the partner's own money (no account movement).</summary>
public static class PartnerSettlementMethod { public const string PartnerExpense = "PartnerExpense"; }
public static class ExpenseClassification { public const string Direct = "Direct"; public const string GeneralOverhead = "GeneralOverhead"; }

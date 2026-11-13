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

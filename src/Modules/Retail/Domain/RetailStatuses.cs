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
public static class OrderStatus { public const string New = "New"; public const string Confirmed = "Confirmed"; public const string Preparing = "Preparing"; public const string Ready = "Ready"; public const string Delivered = "Delivered"; public const string Completed = "Completed"; public const string Cancelled = "Cancelled"; }
public static class PaymentStatus { public const string Unpaid = "Unpaid"; public const string PendingVerification = "PendingVerification"; public const string Paid = "Paid"; public const string Refunded = "Refunded"; public const string Waived = "Waived"; }
public static class EarningStatus { public const string Outstanding = "Outstanding"; public const string Paid = "Paid"; public const string Reversed = "Reversed"; }
public static class ReturnStatus { public const string PendingInspection = "PendingInspection"; public const string Inspected = "Inspected"; public const string Rejected = "Rejected"; }
public static class AdjustmentStatus { public const string Open = "Open"; public const string Applied = "Applied"; }
public static class LeadSource { public const string Website = "Website"; public const string Phone = "Phone"; public const string Social = "Social"; public const string Manual = "Manual"; }
public static class OrderFulfillment { public const string Delivery = "Delivery"; public const string Pickup = "Pickup"; }
public static class OrderPaymentMethod
{
    public const string CashOnDelivery = "CashOnDelivery";
    public const string BankTransfer = "BankTransfer";
    public const string MobileWalletTransfer = "MobileWalletTransfer";
    public const string ToBeArranged = "ToBeArranged";
}

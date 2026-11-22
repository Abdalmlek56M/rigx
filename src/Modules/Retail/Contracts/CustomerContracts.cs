namespace HardwareStore.Modules.Retail;

/// <summary>The account a customer's sales records are matched to: its id and its phone number (orders without an account match by phone).</summary>
public sealed record CustomerSalesKey(Guid AccountId, string? Phone);

public sealed record CustomerOrderRow(Guid Id, string Reference, DateTimeOffset CreatedAtUtc, string Status, decimal Total, decimal PaidAmount,
    string Channel, bool LinkedToAccount);

/// <summary>Lead source and usage come from the earliest order that has them, the governorate from the latest order that has one.</summary>
public sealed record CustomerOrderStats(int OrderCount, decimal TotalSpent, DateTimeOffset? LastOrderAtUtc, string? LeadSource, string? Usage,
    string? Governorate);

/// <summary>One coupon the customer used: the code, what it took off the order, and the order it was used on (newest first in lists).</summary>
public sealed record CouponUseView(string Code, decimal Discount, Guid OrderId, string Reference, DateTimeOffset UsedAtUtc, string OrderStatus);

public sealed record CustomerSalesView(CustomerOrderStats Stats, IReadOnlyList<CustomerOrderRow> Orders, IReadOnlyList<WarrantyAdminView> Warranties,
    bool Blacklisted, IReadOnlyList<CouponUseView> CouponUses);

/// <summary>How many records moved to the account that was kept.</summary>
public sealed record CustomerSalesMoved(int Orders, int Warranties);

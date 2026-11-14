namespace HardwareStore.Modules.Retail;

/// <summary>The account a customer's sales records are matched to: its id and its phone number (orders without an account match by phone).</summary>
public sealed record CustomerSalesKey(Guid AccountId, string? Phone);

public sealed record CustomerOrderRow(Guid Id, string Reference, DateTimeOffset CreatedAtUtc, string Status, decimal Total, decimal PaidAmount,
    string Channel, bool LinkedToAccount);

/// <summary>Lead source and usage come from the earliest order that has them, the governorate from the latest order that has one.</summary>
public sealed record CustomerOrderStats(int OrderCount, decimal TotalSpent, DateTimeOffset? LastOrderAtUtc, string? LeadSource, string? Usage,
    string? Governorate);

public sealed record CustomerSalesView(CustomerOrderStats Stats, IReadOnlyList<CustomerOrderRow> Orders, IReadOnlyList<WarrantyAdminView> Warranties,
    bool Blacklisted);

/// <summary>How many records moved to the account that was kept.</summary>
public sealed record CustomerSalesMoved(int Orders, int Warranties);

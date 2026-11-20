namespace HardwareStore.Modules.Retail;

public sealed record WarrantyVoidRequest(string? Reason, long RowVersion);
public sealed record WarrantyExtendRequest(int Months, string? Reason);

/// <summary>What anyone may see about a warranty: no customer name, full phone, serial, ids or prices.</summary>
public sealed record WarrantyPublicView(string Code, string Name, string Condition, string? BuildCode, string Label,
    DateTimeOffset StartsAtUtc, DateTimeOffset? EndsAtUtc, string Status, int? DaysLeft);
public sealed record WarrantyLookupResult(IReadOnlyList<WarrantyPublicView> Items);

/// <summary>A signed-in customer's own warranty (public fields plus the order it came from).</summary>
public sealed record WarrantyAccountView(Guid OrderId, string OrderReference, string? InvoiceNumber, string Code, string Name, string Condition, string? BuildCode,
    string Label, DateTimeOffset StartsAtUtc, DateTimeOffset? EndsAtUtc, string Status, int? DaysLeft);

/// <summary>Admin row; the customer name/phone and the serial are only filled for staff who may see them.</summary>
public sealed record WarrantyAdminView(Guid Id, Guid OrderId, string OrderReference, string? InvoiceNumber, string Code, string Name, string Condition, string? BuildCode,
    string Label, DateTimeOffset StartsAtUtc, DateTimeOffset? EndsAtUtc, string Status, int? DaysLeft, string? CustomerName, string? PhoneNumber,
    string? SerialNumber, DateTimeOffset? VoidedAtUtc, string? VoidReason, long RowVersion, int ExtendedMonths);

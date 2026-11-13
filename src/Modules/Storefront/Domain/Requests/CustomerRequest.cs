namespace HardwareStore.Modules.Storefront;

/// <summary>One customer request in the admin inbox (sell, trade-in, upgrade, notify-me, support, contact, service).</summary>
public sealed class CustomerRequest
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Type { get; set; }
    public required string Status { get; set; }
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public string? Email { get; set; }
    public string? Governorate { get; set; }
    public string? Source { get; set; }
    public Guid? UserAccountId { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? PartType { get; set; }
    public string? ModelName { get; set; }
    public string? OwnedFor { get; set; }
    public string? ItemCode { get; set; }
    public string? ServiceKind { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public decimal? OfferAmount { get; set; }
    public decimal? ExpectedResale { get; set; }
    public string? InternalNote { get; set; }
    public Guid? PurchaseId { get; set; }
    public string? PurchaseCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

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
    /// <summary>Notify-me: the normalized model key the customer waits for (<c>ModelName.Key</c>); null on other types.</summary>
    public string? ModelKey { get; set; }
    public Guid? CategoryId { get; set; }
    /// <summary>Notify-me: the condition the customer waits for ("New" / "Used"); null = any condition.</summary>
    public string? Condition { get; set; }
    /// <summary>Notify-me: the first item of the model that became available, and when.</summary>
    public string? AvailableItemCode { get; set; }
    public DateTimeOffset? AvailableAtUtc { get; set; }
    /// <summary>Notify-me: when the owner sent the WhatsApp message.</summary>
    public DateTimeOffset? NotifiedAtUtc { get; set; }
    public string? ServiceKind { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public decimal? OfferAmount { get; set; }
    public decimal? ExpectedResale { get; set; }
    public string? InternalNote { get; set; }
    /// <summary>Photo upload: only the SHA-256 of the token the customer holds, and when it stops working.</summary>
    public string? PhotoTokenHash { get; set; }
    public DateTimeOffset? PhotoTokenExpiresAtUtc { get; set; }
    /// <summary>Private storage keys of the photos the customer attached (see <see cref="RequestPhotoRules"/>).</summary>
    public List<string> Photos { get; set; } = [];
    public Guid? PurchaseId { get; set; }
    public string? PurchaseCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

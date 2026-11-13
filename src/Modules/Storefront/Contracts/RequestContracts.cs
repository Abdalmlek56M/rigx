namespace HardwareStore.Modules.Storefront;

/// <summary>Public form body. <paramref name="Website"/> is the honeypot: a real person leaves it empty.</summary>
public sealed record CustomerRequestSubmission(string? Type, string? Name, string? Phone, string? Email, string? Governorate,
    string? Source, string? Subject, string? Message, string? PartType, string? ModelName, string? OwnedFor, string? ItemCode,
    string? ServiceKind, DateOnly? PreferredDate, string? Website);

/// <summary>The code shown to the customer; null when the honeypot was filled (nothing was stored).</summary>
public sealed record CustomerRequestReceipt(string? Code);

public sealed record CustomerRequestView(Guid Id, string Code, string Type, string Status, string Name, string Phone, string? Email,
    string? Governorate, string? Source, Guid? UserAccountId, string? Subject, string? Message, string? PartType, string? ModelName,
    string? OwnedFor, string? ItemCode, string? ServiceKind, DateOnly? PreferredDate, decimal? OfferAmount, decimal? ExpectedResale,
    string? InternalNote, Guid? PurchaseId, string? PurchaseCode, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ClosedAtUtc, long RowVersion);

public sealed record CustomerRequestListItem(Guid Id, string Code, string Type, string Status, string Name, string Phone,
    string? Governorate, string? ModelName, string? ItemCode, string? ServiceKind, decimal? OfferAmount, DateTimeOffset CreatedAtUtc);

public sealed record CustomerRequestQuery(string? Type = null, string? Status = null, string? Search = null, int Page = 1, int PageSize = 25);

/// <param name="NewByType">How many requests are still New, per type (inbox badges).</param>
public sealed record CustomerRequestPage(IReadOnlyList<CustomerRequestListItem> Items, int Page, int PageSize, int Total,
    IReadOnlyDictionary<string, int> NewByType);

public sealed record RequestStatusRequest(string? Status, long RowVersion);

public sealed record RequestOfferRequest(decimal OfferAmount, decimal? ExpectedResale, long RowVersion);

public sealed record RequestDecisionRequest(string? Decision, long RowVersion);

public sealed record RequestNoteRequest(string? InternalNote, long RowVersion);

public sealed record RequestConvertRequest(long RowVersion);

/// <summary>A prepared WhatsApp reply: the customer's number, the filled text and the wa.me link that opens it.</summary>
public sealed record WhatsAppReplyView(string Phone, string Text, string Url);

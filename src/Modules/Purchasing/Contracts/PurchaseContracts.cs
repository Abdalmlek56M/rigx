namespace HardwareStore.Modules.Purchasing;

public sealed record PurchaseLineRequest(string? ModelName, string? Condition, int Quantity, decimal UnitCost, decimal Price,
    Guid? CategoryId = null, Guid? ProductId = null, decimal? WithBoardExtra = null, decimal? AloneExtra = null,
    string? SerialNumber = null, decimal? PartnerPercent = null, bool IsHidden = false, string? WarrantyLabel = null,
    bool HasBox = false, string? InternalNote = null, Dictionary<string, string>? Specs = null,
    Guid? PartnerId = null);

/// <summary>Create/update body; <paramref name="RowVersion"/> is required on update only. No date means today.</summary>
public sealed record PurchaseRequest(Guid SellerId, string? Kind, bool KeepAsBuild, string? BuildName, DateOnly? PurchaseDate,
    string? Notes, string? PaymentMethod, decimal PaidAmount, decimal TradeInValue, string? TradeInNote,
    IReadOnlyList<PurchaseLineRequest>? Lines, long? RowVersion = null);

public sealed record PurchaseLineView(Guid Id, int SortOrder, Guid? CategoryId, Guid? ProductId, string ModelName, string Condition,
    int Quantity, decimal UnitCost, decimal LineTotal, decimal Price, decimal? WithBoardExtra, decimal? AloneExtra,
    string? SerialNumber, decimal? PartnerPercent, Guid? PartnerId, bool IsHidden, string? WarrantyLabel, bool HasBox, string? InternalNote,
    IReadOnlyDictionary<string, string> Specs);

public sealed record PurchaseView(Guid Id, string Code, SellerSummary Seller, string Kind, bool KeepAsBuild, string? BuildName,
    string Status, DateOnly PurchaseDate, string? Notes, decimal Total, string PaymentMethod, decimal PaidAmount,
    decimal TradeInValue, string? TradeInNote, decimal Remaining, DateTimeOffset? ReceivedAtUtc, Guid? AssemblyId,
    Guid CreatedByUserAccountId, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion,
    IReadOnlyList<PurchaseLineView> Lines);

public sealed record PurchaseListItem(Guid Id, string Code, SellerSummary Seller, string Kind, string Status, DateOnly PurchaseDate,
    decimal Total, decimal Remaining, int LineCount, DateTimeOffset CreatedAtUtc);

public sealed record PurchaseQuery(string? Status = null, string? Search = null, int Page = 1, int PageSize = 25);

public sealed record PurchasePage(IReadOnlyList<PurchaseListItem> Items, int Page, int PageSize, int Total);

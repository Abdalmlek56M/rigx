namespace HardwareStore.Modules.Purchasing;

/// <summary>One buying event from a seller: its lines, what is owed, and how the seller is paid.</summary>
public sealed class Purchase
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public Guid SellerId { get; set; }
    public Seller Seller { get; set; } = null!;
    public required string Kind { get; set; }
    public bool KeepAsBuild { get; set; }
    public string? BuildName { get; set; }
    public string Status { get; set; } = PurchaseStatus.Draft;
    public DateOnly PurchaseDate { get; set; }
    public string? Notes { get; set; }
    public decimal Total { get; set; }
    public required string PaymentMethod { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal TradeInValue { get; set; }
    public string? TradeInNote { get; set; }
    public bool PayFromCash { get; set; } = true;
    public decimal Remaining { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
    public Guid? AssemblyId { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public List<PurchaseLine> Lines { get; set; } = [];

    /// <summary>Replaces the editable content with an already checked request.</summary>
    public void Revise(Guid sellerId, CheckedPurchase checkedPurchase, DateTimeOffset now)
    {
        SellerId = sellerId;
        Kind = checkedPurchase.Kind;
        KeepAsBuild = checkedPurchase.KeepAsBuild;
        BuildName = checkedPurchase.BuildName;
        PurchaseDate = checkedPurchase.PurchaseDate;
        Notes = checkedPurchase.Notes;
        Total = checkedPurchase.Total;
        PaymentMethod = checkedPurchase.Payment.Method;
        PaidAmount = checkedPurchase.Payment.PaidAmount;
        TradeInValue = checkedPurchase.Payment.TradeInValue;
        TradeInNote = checkedPurchase.Payment.TradeInNote;
        PayFromCash = checkedPurchase.Payment.PayFromCash;
        Remaining = checkedPurchase.Remaining;
        UpdatedAtUtc = now;
        Lines.Clear();
        foreach (var line in checkedPurchase.Lines)
        {
            line.PurchaseId = Id;
            Lines.Add(line);
        }
    }

    /// <summary>Only a draft can be edited, cancelled or received; everything else is history.</summary>
    public void EnsureDraft()
    {
        if (Status != PurchaseStatus.Draft)
            throw new PurchasingException(409, $"Purchase {Code} is {Status} and can no longer be changed.");
    }
}

public sealed class PurchaseLine
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public int SortOrder { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ProductId { get; set; }
    public required string ModelName { get; set; }
    public required string Condition { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal Price { get; set; }
    public decimal? WithBoardExtra { get; set; }
    public decimal? AloneExtra { get; set; }
    public string? SerialNumber { get; set; }
    public decimal? PartnerPercent { get; set; }
    /// <summary>The partner financing these pieces (a Retail partner id; no foreign key across modules).</summary>
    public Guid? PartnerId { get; set; }
    public bool IsHidden { get; set; }
    public string? WarrantyLabel { get; set; }
    public bool HasBox { get; set; }
    public string? InternalNote { get; set; }
    public Dictionary<string, string> Specs { get; set; } = [];
    /// <summary>The Catalog brand (no foreign key across modules).</summary>
    public Guid? BrandId { get; set; }
    /// <summary>
    /// "نسخ من منتج موجود": the Retail stock item whose photos every item of this line gets a copy of when it is received.
    /// The other copied facts (model, category, brand, specs) are already in the line's own fields.
    /// </summary>
    public Guid? CopyPhotosFromItemId { get; set; }
}

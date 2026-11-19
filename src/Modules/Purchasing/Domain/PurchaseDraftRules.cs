using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Purchasing;

/// <summary>Condition of a purchased piece; the same two words the store uses for stock.</summary>
public static class PurchaseCondition
{
    public const string New = "New";
    public const string Used = "Used";
}

/// <summary>A purchase request that passed every rule: cleaned fields, built lines and the money side worked out.</summary>
public sealed record CheckedPurchase(string Kind, bool KeepAsBuild, string? BuildName, DateOnly PurchaseDate, string? Notes,
    decimal Total, SellerPayment Payment, decimal Remaining, IReadOnlyList<PurchaseLine> Lines);

public static class PurchaseDraftRules
{
    public const int MaxBuildNameLength = 200;
    public const int MaxNotesLength = 2000;
    public const int MaxTradeInNoteLength = 500;
    public const int MaxSerialLength = 64;
    public const int MaxWarrantyLabelLength = 64;
    public const int MaxInternalNoteLength = 1000;
    public const int MaxSpecs = 40;
    private const int MaxSpecKeyLength = 64;
    private const int MaxSpecValueLength = 200;

    public static CheckedPurchase Check(PurchaseRequest request, DateOnly today)
    {
        var requested = request.Lines ?? [];
        var kind = request.Kind?.Trim() ?? string.Empty;
        PurchaseRules.CheckShape(kind, requested.Count, request.KeepAsBuild);
        var lines = requested.Select(BuildLine).ToList();
        var total = lines.Sum(line => line.LineTotal);
        var payment = new SellerPayment(request.PaymentMethod?.Trim() ?? string.Empty, request.PaidAmount, request.TradeInValue,
            TextRules.Optional(request.TradeInNote, MaxTradeInNoteLength, "Trade-in note"));
        var remaining = PurchaseRules.Remaining(total, payment);
        var buildName = request.KeepAsBuild ? TextRules.Optional(request.BuildName, MaxBuildNameLength, "Build name") : null;
        return new CheckedPurchase(kind, request.KeepAsBuild, buildName, request.PurchaseDate ?? today,
            TextRules.Optional(request.Notes, MaxNotesLength, "Notes"), total, payment, remaining, lines);
    }

    private static PurchaseLine BuildLine(PurchaseLineRequest request, int sortOrder)
    {
        var model = string.IsNullOrWhiteSpace(request.ModelName) ? string.Empty : ModelName.Clean(request.ModelName);
        if (ModelName.Key(model).Length == 0) throw new PurchasingException(400, "Every line needs a model name.");
        if (model.Length > ModelName.MaxLength) throw new PurchasingException(400, "The model name is too long.");
        var lineTotal = PurchaseRules.LineTotal(request.Quantity, request.UnitCost, request.Price);
        if (request.WithBoardExtra < 0 || request.AloneExtra < 0) throw new PurchasingException(400, "Price extras cannot be negative.");
        if (request.PartnerPercent is < 0 or > 100) throw new PurchasingException(400, "Partner percentage must be between 0 and 100.");
        var serial = TextRules.Optional(request.SerialNumber, MaxSerialLength, "Serial number");
        if (serial is not null && request.Quantity != 1) throw new PurchasingException(400, "A serial number needs a quantity of 1.");
        return new PurchaseLine
        {
            Id = Guid.CreateVersion7(),
            SortOrder = sortOrder,
            CategoryId = request.CategoryId,
            ProductId = request.ProductId,
            ModelName = model,
            Condition = ParseCondition(request.Condition),
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            LineTotal = lineTotal,
            Price = request.Price,
            WithBoardExtra = request.WithBoardExtra,
            AloneExtra = request.AloneExtra,
            SerialNumber = serial,
            PartnerPercent = request.PartnerPercent,
            PartnerId = request.PartnerId,
            IsHidden = request.IsHidden,
            WarrantyLabel = TextRules.Optional(request.WarrantyLabel, MaxWarrantyLabelLength, "Warranty"),
            HasBox = request.HasBox,
            InternalNote = TextRules.Optional(request.InternalNote, MaxInternalNoteLength, "Internal note"),
            Specs = CleanSpecs(request.Specs),
            BrandId = request.BrandId,
            CopyPhotosFromItemId = request.CopyPhotosFromItemId
        };
    }

    private static string ParseCondition(string? condition) => condition?.Trim() switch
    {
        PurchaseCondition.New => PurchaseCondition.New,
        PurchaseCondition.Used => PurchaseCondition.Used,
        _ => throw new PurchasingException(400, "Condition must be New or Used.")
    };

    private static Dictionary<string, string> CleanSpecs(Dictionary<string, string>? specs)
    {
        var clean = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in specs ?? [])
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
            if (key.Trim().Length > MaxSpecKeyLength || value.Trim().Length > MaxSpecValueLength)
                throw new PurchasingException(400, "A spec name or value is too long.");
            clean[key.Trim()] = value.Trim();
        }
        if (clean.Count > MaxSpecs) throw new PurchasingException(400, $"A line can have at most {MaxSpecs} specs.");
        return clean;
    }
}

namespace HardwareStore.Modules.Purchasing;

public static class SellerRules
{
    public const int MaxNameLength = 160;
    public const int MaxNationalIdLength = 20;
    public const int MaxAreaLength = 120;
    public const int MaxSourceLength = 64;
    public const int MaxNotesLength = 1000;

    /// <summary>Checks the request and copies the cleaned values onto <paramref name="seller"/>.</summary>
    public static void Apply(Seller seller, SellerRequest request)
    {
        seller.Kind = PurchaseRules.ParseSellerKind(request.Kind);
        seller.Name = TextRules.Required(request.Name, MaxNameLength, "Seller name");
        seller.Phone = PurchaseRules.NormalizePhone(request.Phone);
        seller.NationalId = CleanNationalId(request.NationalId);
        seller.Governorate = PurchaseRules.ParseGovernorate(request.Governorate);
        seller.Area = TextRules.Optional(request.Area, MaxAreaLength, "Area");
        seller.Source = TextRules.Optional(request.Source, MaxSourceLength, "Source");
        seller.Notes = TextRules.Optional(request.Notes, MaxNotesLength, "Notes");
    }

    private static string? CleanNationalId(string? nationalId)
    {
        var clean = TextRules.Optional(nationalId, MaxNationalIdLength, "National ID");
        if (clean is not null && !clean.All(char.IsAsciiDigit)) throw new PurchasingException(400, "National ID must contain digits only.");
        return clean;
    }
}

namespace HardwareStore.Common.Domain;

/// <summary>The 27 Egyptian governorates, keyed by their ISO 3166-2:EG code. Used by shipping and addresses.</summary>
public static class EgyptGovernorates
{
    public static readonly IReadOnlyDictionary<string, string> NamesByCode = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["C"] = "القاهرة",
        ["GZ"] = "الجيزة",
        ["ALX"] = "الإسكندرية",
        ["KB"] = "القليوبية",
        ["BH"] = "البحيرة",
        ["MT"] = "مطروح",
        ["DK"] = "الدقهلية",
        ["GH"] = "الغربية",
        ["MNF"] = "المنوفية",
        ["SHR"] = "الشرقية",
        ["KFS"] = "كفر الشيخ",
        ["DT"] = "دمياط",
        ["PTS"] = "بورسعيد",
        ["IS"] = "الإسماعيلية",
        ["SUZ"] = "السويس",
        ["SIN"] = "شمال سيناء",
        ["JS"] = "جنوب سيناء",
        ["FYM"] = "الفيوم",
        ["BNS"] = "بني سويف",
        ["MN"] = "المنيا",
        ["AST"] = "أسيوط",
        ["SHG"] = "سوهاج",
        ["KN"] = "قنا",
        ["LX"] = "الأقصر",
        ["ASN"] = "أسوان",
        ["BA"] = "البحر الأحمر",
        ["WAD"] = "الوادي الجديد"
    };

    public static bool IsKnown(string? code) => code is not null && NamesByCode.ContainsKey(code);

    /// <summary>The Arabic name for a governorate given by its Arabic name or its code; <c>null</c> when it is not one of the 27.</summary>
    public static string? NameOf(string? value)
    {
        var clean = value?.Trim();
        if (string.IsNullOrEmpty(clean)) return null;
        if (NamesByCode.TryGetValue(clean.ToUpperInvariant(), out var name)) return name;
        return NamesByCode.Values.FirstOrDefault(known => string.Equals(known, clean, StringComparison.Ordinal));
    }
}

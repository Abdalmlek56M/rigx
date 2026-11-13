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
}

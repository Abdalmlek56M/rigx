namespace HardwareStore.Modules.Catalog;

/// <summary>A category the store starts with; <see cref="ParentSlug"/> is null for a main category.</summary>
public sealed record BaseCategory(string Slug, string Name, string? ParentSlug = null);

/// <summary>A base category whose starting name changed: the old name becomes the new one only where the owner kept the old name.</summary>
public sealed record BaseCategoryRename(string Slug, string OldName, string NewName);

/// <summary>A brand the store starts with.</summary>
public sealed record BaseBrand(string Slug, string Name);

/// <summary>
/// The category tree and brand list the store starts with (owner decision, 19 Nov 2026). They are added when missing and
/// never removed afterwards: the owner edits them in the admin like any other category or brand. A starting name is renamed only
/// through <see cref="Renames"/>, and only while the owner has not changed it.
/// The storefront picks a category icon from these slugs (<c>apps/web/src/components/ui/category-icon.tsx</c>).
/// </summary>
public static class BaseCatalog
{
    public static readonly IReadOnlyList<BaseCategory> Categories =
    [
        new("processors", "معالجات"),
        new("intel", "Intel", "processors"),
        new("amd", "AMD", "processors"),
        new("graphics-cards", "كروت شاشة"),
        new("monitors", "شاشات"),
        new("accessories", "إكسسوارات"),
        new("motherboards", "بوردات"),
        new("memory", "رامات"),
        new("cases", "كيس"),
        new("power-supplies", "باورات"),
        new("storage", "وحدات تخزين"),
        new("cooling", "تبريد")
    ];

    /// <summary>Starting names the owner asked to change (round 3, 19 Nov 2026).</summary>
    public static readonly IReadOnlyList<BaseCategoryRename> Renames =
    [
        new("cases", "كيسات", "كيس")
    ];

    public static readonly IReadOnlyList<BaseBrand> Brands =
    [
        new("asus", "ASUS"), new("msi", "MSI"), new("gigabyte", "Gigabyte"), new("asrock", "ASRock"), new("palit", "Palit"),
        new("pny", "PNY"), new("zotac", "Zotac"), new("sapphire", "Sapphire"), new("powercolor", "PowerColor"), new("xfx", "XFX"),
        new("inno3d", "Inno3D"), new("gainward", "Gainward"), new("evga", "EVGA"), new("intel", "Intel"), new("amd", "AMD"),
        new("nvidia", "NVIDIA"), new("corsair", "Corsair"), new("kingston", "Kingston"), new("g-skill", "G.Skill"),
        new("crucial", "Crucial"), new("teamgroup", "TeamGroup"), new("adata", "ADATA"), new("lexar", "Lexar"),
        new("samsung", "Samsung"), new("western-digital", "Western Digital"), new("seagate", "Seagate"), new("toshiba", "Toshiba"),
        new("cooler-master", "Cooler Master"), new("nzxt", "NZXT"), new("lian-li", "Lian Li"), new("thermaltake", "Thermaltake"),
        new("be-quiet", "be quiet!"), new("deepcool", "DeepCool"), new("arctic", "Arctic"), new("noctua", "Noctua"),
        new("seasonic", "Seasonic"), new("antec", "Antec"), new("fractal-design", "Fractal Design"), new("logitech", "Logitech"),
        new("razer", "Razer"), new("hyperx", "HyperX"), new("steelseries", "SteelSeries"), new("redragon", "Redragon"),
        new("dell", "Dell"), new("lg", "LG"), new("aoc", "AOC"), new("benq", "BenQ"), new("viewsonic", "ViewSonic"),
        new("xiaomi", "Xiaomi")
    ];
}

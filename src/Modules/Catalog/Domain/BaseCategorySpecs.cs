namespace HardwareStore.Modules.Catalog;

/// <summary>
/// Starter spec fields for the base categories (round 3, slice J; the owner delegated the list), so the stock form asks for e.g. a
/// graphics card's VRAM in GB. A base category gets them only while it has no specs of its own; the owner edits them in the admin.
/// Keys follow the builder's conventions: <c>power_w</c> is a part's draw (a power supply's output), and the compatibility specs
/// (<c>socket</c>, <c>memory_type</c>) must match between the parts that carry them, so a card's memory type has its own key.
/// </summary>
public static class BaseCategorySpecs
{
    private static readonly string[] Sockets = ["AM4", "AM5", "LGA1151", "LGA1200", "LGA1700", "LGA1851"];
    private static readonly string[] MemoryTypes = ["DDR3", "DDR4", "DDR5"];
    private static readonly string[] FormFactors = ["ATX", "Micro-ATX", "Mini-ITX", "E-ATX"];

    private static SpecDefinition Socket => new("socket", "السوكت", SpecType.Choice, Sockets, IsFilter: true, IsCompatibility: true);
    private static SpecDefinition MemoryType => new("memory_type", "نوع الرامات", SpecType.Choice, MemoryTypes, IsFilter: true, IsCompatibility: true);
    private static SpecDefinition Power(string name) => new("power_w", name, SpecType.Number, Unit: "W");

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<SpecDefinition>> BySlug = new Dictionary<string, IReadOnlyList<SpecDefinition>>
    {
        ["processors"] =
        [
            Socket,
            new("cores", "عدد الأنوية", SpecType.Number, IsFilter: true),
            new("threads", "عدد الثريدات", SpecType.Number),
            new("integrated_gpu", "فيه كارت شاشة مدمج", SpecType.YesNo, IsFilter: true),
            Power("استهلاك الكهربا")
        ],
        ["graphics-cards"] =
        [
            new("vram", "الذاكرة", SpecType.Number, Unit: "GB", IsFilter: true),
            new("vram_type", "نوع الذاكرة", SpecType.Choice, ["GDDR5", "GDDR6", "GDDR6X", "GDDR7"]),
            Power("استهلاك الكهربا")
        ],
        ["motherboards"] = [Socket, MemoryType, new("form_factor", "المقاس", SpecType.Choice, FormFactors, IsFilter: true)],
        ["memory"] =
        [
            new("capacity", "المساحة", SpecType.Number, Unit: "GB", IsFilter: true),
            MemoryType,
            new("speed", "السرعة", SpecType.Number, Unit: "MHz")
        ],
        ["storage"] =
        [
            new("capacity", "المساحة", SpecType.Number, Unit: "GB", IsFilter: true),
            new("storage_type", "النوع", SpecType.Choice, ["HDD", "SATA SSD", "NVMe SSD"], IsFilter: true)
        ],
        ["power-supplies"] =
        [
            Power("القدرة"),
            new("efficiency", "الشهادة", SpecType.Choice, ["80+", "80+ Bronze", "80+ Silver", "80+ Gold", "80+ Platinum", "80+ Titanium"], IsFilter: true)
        ],
        ["monitors"] =
        [
            new("size", "المقاس", SpecType.Number, Unit: "inch", IsFilter: true),
            new("refresh_rate", "معدل التحديث", SpecType.Number, Unit: "Hz", IsFilter: true),
            new("resolution", "الدقة", SpecType.Choice, ["HD", "Full HD", "2K", "4K"], IsFilter: true),
            new("panel", "نوع الشاشة", SpecType.Choice, ["IPS", "VA", "TN", "OLED"])
        ],
        ["cases"] = [new("form_factor", "مقاس البوردة", SpecType.Choice, FormFactors, IsFilter: true)],
        ["cooling"] = [new("cooler_type", "النوع", SpecType.Choice, ["هوا", "مية"], IsFilter: true)]
    };
}

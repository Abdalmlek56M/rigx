namespace HardwareStore.Modules.Catalog;

/// <summary>
/// Starter spec fields for the base categories (round 3, slice J; the owner delegated the list), so the stock form asks for e.g. a
/// graphics card's VRAM in GB. A base category gets them only while it has no specs of its own; the owner edits them in the admin.
/// Keys follow the builder's conventions: <c>power_w</c> is a part's draw (a power supply's output), the compatibility specs
/// (<c>socket</c>, <c>memory_type</c>) must match between the parts that carry them, so a card's memory type has its own key, and
/// <c>max_length</c> on a case limits the <c>length</c> of a graphics card.
/// Every spec is optional on every item (business §3): the owner leaves a field empty on a part it does not fit, and adds, removes
/// or reorders fields per category in the admin. The brand and the warranty are item fields already, not specs.
/// </summary>
public static class BaseCategorySpecs
{
    private static readonly string[] Sockets = ["AM4", "AM5", "LGA1151", "LGA1200", "LGA1700", "LGA1851"];
    private static readonly string[] MemoryTypes = ["DDR3", "DDR4", "DDR5"];
    private static readonly string[] FormFactors = ["ATX", "Micro-ATX", "Mini-ITX", "E-ATX"];

    private static SpecDefinition Socket => new("socket", "السوكت", SpecType.Choice, Sockets, IsFilter: true, IsCompatibility: true);
    private static SpecDefinition MemoryType => new("memory_type", "نوع الرامات", SpecType.Choice, MemoryTypes, IsFilter: true, IsCompatibility: true);
    private static SpecDefinition Power(string name) => new("power_w", name, SpecType.Number, Unit: "W");

    /// <summary>The starter list a store gets today.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<SpecDefinition>> BySlug = new Dictionary<string, IReadOnlyList<SpecDefinition>>
    {
        ["processors"] =
        [
            Socket,
            new("cores", "عدد الأنوية", SpecType.Number, IsFilter: true),
            new("threads", "عدد الخيوط", SpecType.Number),
            new("integrated_gpu", "يحتوي على كرت شاشة مدمج", SpecType.YesNo, IsFilter: true),
            Power("استهلاك الطاقة"),
            new("base_clock", "التردد الأساسي", SpecType.Number, Unit: "GHz"),
            new("cache", "الذاكرة المؤقتة", SpecType.Number, Unit: "MB"),
            new("cooler_included", "يأتي معه مبرّد", SpecType.YesNo)
        ],
        ["graphics-cards"] =
        [
            new("vram", "الذاكرة", SpecType.Number, Unit: "GB", IsFilter: true),
            new("vram_type", "نوع الذاكرة", SpecType.Choice, ["GDDR5", "GDDR6", "GDDR6X", "GDDR7"]),
            Power("استهلاك الطاقة"),
            new("gpu_maker", "شركة المعالج الرسومي", SpecType.Choice, ["NVIDIA", "AMD", "Intel"], IsFilter: true),
            new("length", "الطول", SpecType.Number, Unit: "mm"),
            new("fans", "عدد المراوح", SpecType.Number),
            new("power_connectors", "وصلات الطاقة", SpecType.Text)
        ],
        ["motherboards"] =
        [
            Socket,
            MemoryType,
            new("form_factor", "المقاس", SpecType.Choice, FormFactors, IsFilter: true),
            new("chipset", "الشريحة", SpecType.Text),
            new("memory_slots", "عدد فتحات الرام", SpecType.Number),
            new("m2_slots", "عدد فتحات M.2", SpecType.Number),
            new("wifi", "يحتوي على واي فاي", SpecType.YesNo)
        ],
        ["memory"] =
        [
            new("capacity", "المساحة", SpecType.Number, Unit: "GB", IsFilter: true),
            MemoryType,
            new("speed", "السرعة", SpecType.Number, Unit: "MHz"),
            new("modules", "عدد الشرائح في الطقم", SpecType.Number),
            new("latency", "زمن الاستجابة (CL)", SpecType.Number),
            new("rgb", "إضاءة RGB", SpecType.YesNo)
        ],
        ["storage"] =
        [
            new("capacity", "المساحة", SpecType.Number, Unit: "GB", IsFilter: true),
            new("storage_type", "النوع", SpecType.Choice, ["HDD", "SATA SSD", "NVMe SSD"], IsFilter: true),
            new("interface", "الواجهة", SpecType.Choice, ["SATA", "PCIe 3.0", "PCIe 4.0", "PCIe 5.0"]),
            new("read_speed", "سرعة القراءة", SpecType.Number, Unit: "MB/s")
        ],
        ["power-supplies"] =
        [
            Power("القدرة"),
            new("efficiency", "الشهادة", SpecType.Choice, ["80+", "80+ Bronze", "80+ Silver", "80+ Gold", "80+ Platinum", "80+ Titanium"], IsFilter: true),
            new("form_factor", "المقاس", SpecType.Choice, ["ATX", "SFX", "SFX-L", "TFX"], IsFilter: true),
            new("modular", "نوع الكابلات", SpecType.Choice, ["Full Modular", "Semi Modular", "Non Modular"], IsFilter: true),
            new("fan_size", "حجم المروحة", SpecType.Number, Unit: "mm"),
            new("pcie_connectors", "وصلات كرت الشاشة", SpecType.Text)
        ],
        ["monitors"] =
        [
            new("size", "المقاس", SpecType.Number, Unit: "inch", IsFilter: true),
            new("refresh_rate", "معدل التحديث", SpecType.Number, Unit: "Hz", IsFilter: true),
            new("resolution", "الدقة", SpecType.Choice, ["HD", "Full HD", "2K", "4K"], IsFilter: true),
            new("panel", "نوع الشاشة", SpecType.Choice, ["IPS", "VA", "TN", "OLED"]),
            new("response_time", "زمن الاستجابة", SpecType.Number, Unit: "ms"),
            new("curved", "شاشة منحنية", SpecType.YesNo),
            new("ports", "المنافذ", SpecType.Text)
        ],
        ["cases"] =
        [
            new("form_factor", "مقاس البوردة", SpecType.Choice, FormFactors, IsFilter: true),
            new("case_size", "حجم الكيس", SpecType.Choice, ["Mini Tower", "Mid Tower", "Full Tower", "Small Form Factor"], IsFilter: true),
            new("side_panel", "الجانب", SpecType.Choice, ["زجاج مقسّى", "أكريليك", "معدن"]),
            new("fans_included", "عدد المراوح المرفقة", SpecType.Number),
            new("max_length", "أقصى طول لكرت الشاشة", SpecType.Number, Unit: "mm", IsCompatibility: true)
        ],
        ["cooling"] =
        [
            new("cooler_type", "النوع", SpecType.Choice, ["هواء", "ماء"], IsFilter: true),
            new("tdp", "القدرة على التبريد", SpecType.Number, Unit: "W"),
            new("fan_size", "حجم المروحة", SpecType.Number, Unit: "mm"),
            new("radiator", "حجم المشع", SpecType.Choice, ["120", "240", "280", "360"]),
            new("supported_sockets", "السوكتات المدعومة", SpecType.Text)
        ]
    };

    /// <summary>
    /// The first starter list (before the richer one above): a store whose category still has exactly this list, with no field added,
    /// removed or reordered, is upgraded by <see cref="Upgrade"/>. Never edit these; they are what already-seeded stores hold.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (string Key, string Name)[]> First = new Dictionary<string, (string, string)[]>
    {
        ["processors"] = [("socket", "السوكت"), ("cores", "عدد الأنوية"), ("threads", "عدد الثريدات"), ("integrated_gpu", "فيه كارت شاشة مدمج"), ("power_w", "استهلاك الكهربا")],
        ["graphics-cards"] = [("vram", "الذاكرة"), ("vram_type", "نوع الذاكرة"), ("power_w", "استهلاك الكهربا")],
        ["motherboards"] = [("socket", "السوكت"), ("memory_type", "نوع الرامات"), ("form_factor", "المقاس")],
        ["memory"] = [("capacity", "المساحة"), ("memory_type", "نوع الرامات"), ("speed", "السرعة")],
        ["storage"] = [("capacity", "المساحة"), ("storage_type", "النوع")],
        ["power-supplies"] = [("power_w", "القدرة"), ("efficiency", "الشهادة")],
        ["monitors"] = [("size", "المقاس"), ("refresh_rate", "معدل التحديث"), ("resolution", "الدقة"), ("panel", "نوع الشاشة")],
        ["cases"] = [("form_factor", "مقاس البوردة")],
        ["cooling"] = [("cooler_type", "النوع")]
    };

    /// <summary>
    /// The upgraded list for a category that still holds the first starter list untouched, or null when it does not (the owner changed it,
    /// or it is already upgraded). Each existing field keeps the owner's definition (so the values on items stay valid), except that a
    /// name still equal to the first starter name takes today's formal name; the new fields are appended.
    /// </summary>
    public static IReadOnlyList<SpecDefinition>? Upgrade(string slug, IReadOnlyList<SpecDefinition> current)
    {
        if (!First.TryGetValue(slug, out var first) || !BySlug.TryGetValue(slug, out var latest)) return null;
        if (!current.Select(spec => spec.Key).SequenceEqual(first.Select(entry => entry.Key))) return null;
        var kept = current.Select((spec, index) =>
        {
            var today = latest.FirstOrDefault(entry => entry.Key == spec.Key);
            return today is not null && spec.Name == first[index].Name ? spec with { Name = today.Name } : spec;
        });
        return [.. kept, .. latest.Where(entry => current.All(spec => spec.Key != entry.Key))];
    }
}

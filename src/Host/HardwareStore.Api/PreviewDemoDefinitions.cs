using HardwareStore.Modules.Retail;

/// <summary>
/// The fake Development demo data of roadmap 4.4g: stock items, builds, customers, orders, POS sales, expenses and content.
/// Nothing here is real; emails are @example.com and the phones are 010000000xx.
/// </summary>
internal static class PreviewDemoDefinitions
{
    public const string StockNotePrefix = "Demo 4.4g · ";
    public const string BuildNamePrefix = "Demo Build · ";
    public const string ExpenseNotePrefix = "Demo 4.4g · ";

    public static PreviewDemoStockItem[] StockItems() =>
    [
        S("cpu-ryzen5-5600-used", "AMD Ryzen 5 5600", StockCondition.Used, "amd", 1, 3800m, 5200m,
            withBoard: 200m, alone: 400m, specs: Specs(("Socket", "AM4"), ("Cores", "6"))),
        S("cpu-ryzen5-5600-new", "AMD Ryzen 5 5600", StockCondition.New, "amd", 3, 4500m, 6100m,
            withBoard: 200m, alone: 400m, specs: Specs(("Socket", "AM4"), ("Cores", "6"))),
        S("cpu-ryzen7-5700x", "AMD Ryzen 7 5700X", StockCondition.New, "amd", 3, 6800m, 8900m,
            withBoard: 200m, alone: 400m, specs: Specs(("Socket", "AM4"), ("Cores", "8"))),
        S("cpu-i5-12400f-used", "Intel Core i5-12400F", StockCondition.Used, "intel", 1, 4500m, 5900m,
            withBoard: 200m, alone: 400m, specs: Specs(("Socket", "LGA1700"), ("Cores", "6"))),
        S("cpu-i5-13400", "Intel Core i5-13400", StockCondition.New, "intel", 3, 7500m, 9600m,
            withBoard: 200m, alone: 400m, specs: Specs(("Socket", "LGA1700"), ("Cores", "10"))),

        S("gpu-rtx3060-used", "NVIDIA GeForce RTX 3060 12GB", StockCondition.Used, "graphics-cards", 1, 12000m, 16500m,
            alone: 500m, specs: Specs(("Memory", "12 GB"))),
        S("gpu-rtx4060", "NVIDIA GeForce RTX 4060 8GB", StockCondition.New, "graphics-cards", 3, 18500m, 22900m,
            alone: 500m, specs: Specs(("Memory", "8 GB"))),
        S("gpu-rx6600-used", "AMD Radeon RX 6600 8GB", StockCondition.Used, "graphics-cards", 1, 8500m, 11900m,
            alone: 500m, specs: Specs(("Memory", "8 GB"))),
        S("gpu-rx6600-new", "AMD Radeon RX 6600 8GB", StockCondition.New, "graphics-cards", 3, 11000m, 13900m,
            alone: 500m, specs: Specs(("Memory", "8 GB"))),
        S("gpu-gtx1660s-used", "NVIDIA GeForce GTX 1660 Super 6GB", StockCondition.Used, "graphics-cards", 1, 6000m, 8500m,
            alone: 500m, specs: Specs(("Memory", "6 GB"))),

        S("board-b450m-used", "ASUS Prime B450M-A", StockCondition.Used, "motherboards", 1, 1600m, 2900m,
            specs: Specs(("Socket", "AM4"))),
        S("board-b550m", "ASUS Prime B550M-A", StockCondition.New, "motherboards", 4, 3300m, 4200m,
            specs: Specs(("Socket", "AM4"))),
        S("board-b660m", "MSI PRO B660M-A", StockCondition.New, "motherboards", 4, 4300m, 5900m,
            specs: Specs(("Socket", "LGA1700"))),
        S("board-b650e", "ASUS ROG Strix B650E-F", StockCondition.New, "motherboards", 1, 11000m, 13400m,
            specs: Specs(("Socket", "AM5"))),

        S("ram-vengeance16", "Corsair Vengeance 16GB DDR4-3200", StockCondition.New, "memory", 6, 1450m, 2190m,
            specs: Specs(("Capacity", "16 GB"), ("Speed", "3200 MT/s"))),
        S("ram-fury32", "Kingston Fury 32GB DDR5-6000", StockCondition.New, "memory", 4, 3900m, 5490m,
            specs: Specs(("Capacity", "32 GB"), ("Speed", "6000 MT/s"))),
        S("ram-ddr4-8gb-used", "Kingston 8GB DDR4-2666", StockCondition.Used, "memory", 1, 350m, 650m,
            specs: Specs(("Capacity", "8 GB"))),

        S("ssd-nv3-1tb", "Kingston NV3 1TB NVMe", StockCondition.New, "storage", 6, 1850m, 2790m,
            specs: Specs(("Capacity", "1 TB"), ("Interface", "PCIe 4.0"))),
        S("ssd-980-500gb-used", "Samsung 980 500GB NVMe", StockCondition.Used, "storage", 1, 900m, 1450m,
            specs: Specs(("Capacity", "500 GB"))),
        S("hdd-wd-blue-1tb-used", "WD Blue 1TB HDD", StockCondition.Used, "storage", 1, 500m, 900m,
            specs: Specs(("Capacity", "1 TB"))),
        S("ssd-990pro-2tb", "Samsung 990 PRO 2TB NVMe", StockCondition.New, "storage", 1, 6500m, 8390m,
            specs: Specs(("Capacity", "2 TB"), ("Interface", "PCIe 4.0"))),

        S("psu-cv650", "Corsair CV650 650W", StockCondition.New, "power-supplies", 6, 2500m, 3600m,
            specs: Specs(("Wattage", "650 W"))),
        S("psu-s12iii-used", "SeaSonic S12III 500W", StockCondition.Used, "power-supplies", 1, 900m, 1500m,
            specs: Specs(("Wattage", "500 W"))),
        S("psu-rm850e", "Corsair RM850e Gold", StockCondition.New, "power-supplies", 1, 5200m, 6490m,
            specs: Specs(("Wattage", "850 W"), ("Efficiency", "80+ Gold"))),

        S("case-h510-used", "NZXT H510", StockCondition.Used, "cases", 1, 1200m, 2100m,
            specs: Specs(("Form Factor", "Mid Tower"))),
        S("case-matrexx40", "Deepcool MATREXX 40", StockCondition.New, "cases", 6, 1300m, 1900m,
            specs: Specs(("Form Factor", "Mid Tower"))),
        S("case-o11", "Lian Li O11 Vision", StockCondition.New, "cases", 1, 5800m, 7290m,
            specs: Specs(("Form Factor", "Mid Tower"))),

        S("cool-ak400", "Deepcool AK400", StockCondition.New, "cooling", 6, 1200m, 1750m,
            specs: Specs(("Type", "Air"))),
        S("cool-hyper212-used", "Cooler Master Hyper 212", StockCondition.Used, "cooling", 1, 500m, 950m,
            specs: Specs(("Type", "Air"))),
        S("cool-kraken360", "NZXT Kraken 360 RGB", StockCondition.New, "cooling", 1, 8000m, 9890m,
            specs: Specs(("Type", "Liquid"), ("Radiator", "360 mm"))),

        S("peripheral-redragon-k552", "Redragon K552 Keyboard", StockCondition.New, "accessories", 4, 900m, 1450m),
        S("peripheral-gpx2", "Logitech G Pro X Superlight 2", StockCondition.New, "accessories", 1, 4200m, 5990m,
            specs: Specs(("Type", "Mouse")))
    ];

    public static PreviewDemoBuild[] Builds() =>
    [
        new("Gaming", "تجميعة ألعاب RIGX", "RIGX Gaming Build", AssemblyUsage.Gaming, 46900m,
            "ألعاب أونلاين وإعداد فيديو خفيف", "Online gaming and light video editing",
            ["cpu-ryzen7-5700x", "gpu-rtx4060", "board-b660m", "ram-vengeance16", "ssd-nv3-1tb", "psu-cv650",
                "case-matrexx40", "cool-ak400"]),
        new("Study", "تجميعة شغل ودراسة", "RIGX Study & Work Build", AssemblyUsage.StudyWork, 25900m,
            "شغل مكتبي ودراسة ومونتاج خفيف", "Office work, study and light editing",
            ["cpu-i5-13400", "board-b660m", "ram-vengeance16", "ssd-nv3-1tb", "psu-cv650", "case-matrexx40",
                "cool-ak400"]),
        new("Budget", "تجميعة ألعاب اقتصادية", "RIGX Budget Gaming Build", AssemblyUsage.Gaming, 33900m,
            "ألعاب Full HD بميزانية", "Full HD gaming on a budget",
            ["cpu-ryzen5-5600-new", "gpu-rx6600-new", "board-b550m", "ram-vengeance16", "ssd-nv3-1tb", "psu-cv650",
                "case-matrexx40", "cool-ak400"])
    ];

    public static PreviewDemoCustomer[] Customers() =>
    [
        C("أحمد محمود", "01000000001", "demo1@example.com",
            A("المنزل", "C", "القاهرة", "مدينة نصر", "عباس العقاد", "عمارة 12 - الدور 3", true),
            A("الشغل", "GZ", "الجيزة", "الدقي", "شارع التحرير", "برج 5 - الدور 2", false)),
        C("محمود عبد الله", "01000000002", "demo2@example.com",
            A("المنزل", "C", "القاهرة", "المعادي", "شارع 9", "فيلا 4", true)),
        C("سارة إبراهيم", "01000000003", "demo3@example.com",
            A("المنزل", "ALX", "الإسكندرية", "سموحة", "شارع فوزي معاذ", "عمارة 8", true)),
        C("محمد حسن", "01000000004", "demo4@example.com",
            A("المنزل", "C", "القاهرة", "شبرا", "شارع الترعة البولاقية", "عمارة 22", true),
            A("الشغل", "KB", "القليوبية", "بنها", "شارع فريد ندا", "عمارة 3", false)),
        C("نورهان عادل", "01000000005", "demo5@example.com",
            A("المنزل", "GZ", "الجيزة", "الهرم", "شارع العريش", "عمارة 15", true)),
        C("كريم مصطفى", "01000000006", "demo6@example.com",
            A("المنزل", "C", "القاهرة", "حلوان", "شارع منصور", "عمارة 7", true)),
        C("مريم السيد", "01000000007", "demo7@example.com",
            A("المنزل", "DK", "الدقهلية", "المنصورة", "شارع الجيش", "عمارة 11", true),
            A("بيت العيلة", "GH", "الغربية", "طنطا", "شارع البحر", "عمارة 2", false)),
        C("يوسف خالد", "01000000008", "demo8@example.com",
            A("المنزل", "ALX", "الإسكندرية", "العجمي", "شارع البيطاش", "عمارة 19", true)),
        C("هبة فتحي", "01000000009", "demo9@example.com",
            A("المنزل", "C", "القاهرة", "مصر الجديدة", "شارع الحجاز", "عمارة 6", true)),
        C("عمر شريف", "01000000010", "demo10@example.com",
            A("المنزل", "PTS", "بورسعيد", "الشرق", "شارع 23 يوليو", "عمارة 14", true))
    ];

    public static PreviewDemoOrder[] Orders() =>
    [
        O("demo1@example.com", 26, 0, OrderFulfillment.Pickup, OrderPaymentMethod.CashOnDelivery, OrderStatus.New, L("peripheral-gpx2")),
        O("demo2@example.com", 22, 0, OrderFulfillment.Delivery, OrderPaymentMethod.CashOnDelivery, OrderStatus.New, L("board-b650e")),
        O("demo3@example.com", 18, 0, OrderFulfillment.Delivery, OrderPaymentMethod.InstaPay, OrderStatus.New, L("ram-fury32")),
        O("demo4@example.com", 0, 6, OrderFulfillment.Delivery, OrderPaymentMethod.CashOnDelivery, OrderStatus.Confirmed, L("board-b550m")),
        O("demo5@example.com", 0, 3, OrderFulfillment.Pickup, OrderPaymentMethod.CashOnDelivery, OrderStatus.Confirmed,
            L("cpu-i5-13400"), L("board-b660m")),
        O("demo6@example.com", 0, 10, OrderFulfillment.Delivery, OrderPaymentMethod.InstaPay, OrderStatus.Preparing,
            L("gpu-rx6600-new"), L("psu-cv650")),
        O("demo7@example.com", 0, 8, OrderFulfillment.Pickup, OrderPaymentMethod.CashOnDelivery, OrderStatus.Preparing,
            L("cpu-ryzen7-5700x"), L("gpu-rtx4060")),
        O("demo8@example.com", 7, 0, OrderFulfillment.Delivery, OrderPaymentMethod.InstaPay, OrderStatus.Shipped, L("board-b550m")),
        O("demo9@example.com", 9, 0, OrderFulfillment.Delivery, OrderPaymentMethod.Wallet, OrderStatus.Shipped,
            L("case-matrexx40"), L("cool-ak400")),
        O("demo10@example.com", 14, 0, OrderFulfillment.Delivery, OrderPaymentMethod.CashOnDelivery, OrderStatus.Delivered,
            L("ram-fury32"), L("peripheral-redragon-k552")),
        O("demo1@example.com", 20, 0, OrderFulfillment.Delivery, OrderPaymentMethod.InstaPay, OrderStatus.Delivered,
            L("psu-cv650"), L("cool-ak400"), L("case-matrexx40")),
        O("demo2@example.com", 15, 0, OrderFulfillment.Pickup, OrderPaymentMethod.CashOnDelivery, OrderStatus.Cancelled, L("peripheral-gpx2")),
        O("demo3@example.com", 24, 0, OrderFulfillment.Delivery, OrderPaymentMethod.CashOnDelivery, OrderStatus.Completed,
            L("ram-vengeance16")),
        O("demo6@example.com", 19, 0, OrderFulfillment.Pickup, OrderPaymentMethod.CashOnDelivery, OrderStatus.Completed, L("ssd-nv3-1tb")),
        O("demo8@example.com", 12, 0, OrderFulfillment.Delivery, OrderPaymentMethod.InstaPay, OrderStatus.Completed,
            L("ram-fury32")),
        O("demo9@example.com", 5, 0, OrderFulfillment.Pickup, OrderPaymentMethod.CashOnDelivery, OrderStatus.Completed,
            L("peripheral-redragon-k552"))
    ];

    public static PreviewDemoSale[] Sales() =>
    [
        new("عميل المحل", "01000000011", 27, OrderPaymentMethod.Cash, [L("gpu-gtx1660s-used"), L("hdd-wd-blue-1tb-used")]),
        new("عميلة المحل", "01000000012", 21, OrderPaymentMethod.Cash, [L("ram-ddr4-8gb-used"), L("psu-s12iii-used")]),
        new("عميل المحل", "01000000013", 14, OrderPaymentMethod.Cash, [L("ram-vengeance16"), L("ssd-nv3-1tb")]),
        new("عميلة المحل", "01000000014", 9, OrderPaymentMethod.Cash, [L("peripheral-redragon-k552")]),
        new("عميل المحل", "01000000015", 3, OrderPaymentMethod.Cash, [L("ram-fury32")])
    ];

    public static PreviewDemoExpense[] Expenses() =>
    [
        new(26, "إيجار", 6000m, "إيجار المحل"),
        new(20, "كهرباء ومياه", 1350m, "فاتورة الكهرباء"),
        new(15, "تغليف", 750m, "كراتين وتغليف"),
        new(10, "شحن وتوصيل", 950m, "مستلزمات الشحن"),
        new(6, "إعلانات", 1200m, "إعلانات السوشيال"),
        new(2, "إنترنت", 450m, "اشتراك الإنترنت")
    ];

    private static PreviewDemoStockItem S(string key, string modelName, string condition, string categorySlug, int quantity,
        decimal cost, decimal price, decimal? withBoard = null, decimal? alone = null,
        IReadOnlyDictionary<string, string>? specs = null) =>
        new(key, modelName, condition, categorySlug, quantity, cost, price, withBoard, alone, specs);

    private static IReadOnlyDictionary<string, string> Specs(params (string Key, string Value)[] values) =>
        values.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

    private static PreviewDemoCustomer C(string name, string phone, string email, params PreviewDemoAddress[] addresses) =>
        new(name, phone, email, addresses);

    private static PreviewDemoAddress A(string label, string governorateCode, string governorateName, string city, string street,
        string building, bool isDefault) =>
        new(label, governorateCode, governorateName, city, street, building, isDefault);

    private static PreviewDemoLine L(string key, int quantity = 1) => new(key, quantity);

    private static PreviewDemoOrder O(string customerEmail, int daysAgo, int hoursAgo, string fulfillment, string payment,
        string status, params PreviewDemoLine[] lines) =>
        new(customerEmail, daysAgo, hoursAgo, fulfillment, payment, status, lines);
}

internal sealed record PreviewDemoStockItem(
    string Key,
    string ModelName,
    string Condition,
    string CategorySlug,
    int Quantity,
    decimal Cost,
    decimal Price,
    decimal? WithBoardExtra,
    decimal? AloneExtra,
    IReadOnlyDictionary<string, string>? Specs);

internal sealed record PreviewDemoBuild(
    string Key,
    string TitleAr,
    string TitleEn,
    string Usage,
    decimal SalePrice,
    string SuitableForAr,
    string SuitableForEn,
    string[] PartKeys);

internal sealed record PreviewDemoCustomer(string Name, string Phone, string Email, PreviewDemoAddress[] Addresses);

internal sealed record PreviewDemoAddress(
    string Label,
    string GovernorateCode,
    string GovernorateName,
    string City,
    string Street,
    string Building,
    bool IsDefault);

internal sealed record PreviewDemoOrder(
    string CustomerEmail,
    int DaysAgo,
    int HoursAgo,
    string Fulfillment,
    string Payment,
    string Status,
    PreviewDemoLine[] Lines);

internal sealed record PreviewDemoLine(string Key, int Quantity);

internal sealed record PreviewDemoSale(string CustomerName, string Phone, int DaysAgo, string Payment, PreviewDemoLine[] Lines);

internal sealed record PreviewDemoExpense(int DaysAgo, string Category, decimal Amount, string Note);

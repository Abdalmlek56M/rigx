using System.Globalization;
using System.Text;
using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Retail;

public enum StockImportColumn
{
    Category, Model, Condition, Quantity, Cost, Price, PriceWithBoard, PriceAlone, WarrantyMonths, HasBox, Serial, InternalNote, Hidden, Brand
}

/// <summary>One column of the import sheet: its Arabic header, its English alias, and whether the sheet must have it.</summary>
public sealed record StockImportColumnDefinition(StockImportColumn Column, string Arabic, string English, bool Required);

/// <summary>A sheet row after parsing; <see cref="Line"/> is set only when the row has no errors.</summary>
public sealed record StockImportRow(int Row, string ModelName, string Condition, int Quantity, int ItemsToCreate,
    IReadOnlyList<string> Errors, ReceiptLine? Line);

/// <summary>
/// Opening-stock import rules: the sheet layout, and how one row becomes an intake line. Problems are returned as Arabic
/// messages per row (the owner fixes the sheet and tries again), never thrown.
/// </summary>
public static class StockImportRules
{
    public const int MaxFileBytes = 2 * 1024 * 1024;
    public const int MaxDataRows = 500;
    public const int MaxItems = 2000;
    public const string FileExtension = ".xlsx";
    public const decimal MaxMoney = 100_000_000m;

    public static readonly IReadOnlyList<StockImportColumnDefinition> Columns =
    [
        new(StockImportColumn.Category, "القسم", "Category", true),
        new(StockImportColumn.Model, "الموديل", "Model", true),
        new(StockImportColumn.Condition, "الحالة", "Condition", true),
        new(StockImportColumn.Quantity, "الكمية", "Quantity", false),
        new(StockImportColumn.Cost, "التكلفة", "Cost", true),
        new(StockImportColumn.Price, "السعر في تجميعة", "Price in build", true),
        new(StockImportColumn.PriceWithBoard, "السعر مع بوردة", "Price with board", false),
        new(StockImportColumn.PriceAlone, "السعر لوحدها", "Price alone", false),
        new(StockImportColumn.WarrantyMonths, "الضمان بالشهور", "Warranty months", false),
        new(StockImportColumn.HasBox, "بالكرتونة", "Has box", false),
        new(StockImportColumn.Serial, "السيريال", "Serial", false),
        new(StockImportColumn.InternalNote, "ملاحظة داخلية", "Internal note", false),
        new(StockImportColumn.Hidden, "مخفي", "Hidden", false),
        new(StockImportColumn.Brand, "الماركة", "Brand", false)
    ];

    /// <summary>The template: the header row and one example row that is a valid row for <paramref name="exampleCategory"/>.</summary>
    public static ExportTable Template(string exampleCategory) => new(
        [.. Columns.Select(column => column.Arabic)],
        [[ExportCell.Text(exampleCategory), ExportCell.Text("RTX 3060 12GB"), ExportCell.Text("مستعمل"), ExportCell.Number(1m),
            ExportCell.Number(8000m), ExportCell.Number(9500m), ExportCell.Number(10500m), ExportCell.Number(10000m), ExportCell.Empty,
            ExportCell.Text("نعم"), ExportCell.Empty, ExportCell.Empty, ExportCell.Text("لا"), ExportCell.Empty]]);

    /// <summary>Finds each column in the header row (Arabic or English, any case); the names of required columns that are missing are returned.</summary>
    public static (IReadOnlyDictionary<StockImportColumn, int> Indexes, IReadOnlyList<string> Missing) MapHeader(IReadOnlyList<string> header)
    {
        var indexes = new Dictionary<StockImportColumn, int>();
        foreach (var definition in Columns)
        {
            var index = header.ToList().FindIndex(name => Matches(name, definition.Arabic) || Matches(name, definition.English));
            if (index >= 0) indexes[definition.Column] = index;
        }
        return (indexes, [.. Columns.Where(column => column.Required && !indexes.ContainsKey(column.Column)).Select(column => column.Arabic)]);
    }

    public static StockImportRow ParseRow(int row, IReadOnlyList<string> cells, IReadOnlyDictionary<StockImportColumn, int> indexes,
        StockImportLookups lookups)
    {
        string Cell(StockImportColumn column) =>
            indexes.TryGetValue(column, out var index) && index < cells.Count ? cells[index].Trim() : string.Empty;
        var errors = new List<string>();

        var category = FindCategory(lookups.Categories, Cell(StockImportColumn.Category), errors);
        var modelName = ParseModel(Cell(StockImportColumn.Model), errors);
        var brand = FindBrand(lookups.Brands, Cell(StockImportColumn.Brand), errors);
        var condition = ParseCondition(Cell(StockImportColumn.Condition), errors);
        var quantity = ParseWholeNumber(Cell(StockImportColumn.Quantity), "الكمية", 1, StockItemRules.MaxQuantity, errors) ?? 1;
        var cost = ParseMoney(Cell(StockImportColumn.Cost), "التكلفة", required: true, errors);
        var price = ParseMoney(Cell(StockImportColumn.Price), "السعر في تجميعة", required: true, errors);
        var withBoard = ParseFullPrice(Cell(StockImportColumn.PriceWithBoard), "السعر مع بوردة", price, errors);
        var alone = ParseFullPrice(Cell(StockImportColumn.PriceAlone), "السعر لوحدها", price, errors);
        var warranty = ParseWholeNumber(Cell(StockImportColumn.WarrantyMonths), "الضمان بالشهور", 1, 120, errors);
        string? warrantyLabel = null;
        if (warranty is { } months && !lookups.WarrantyLabels.TryGetValue(months, out warrantyLabel))
            errors.Add($"الضمان {months} شهر مش موجود في قائمة الضمانات في الإعدادات");
        var hasBox = ParseYesNo(Cell(StockImportColumn.HasBox), "بالكرتونة", errors);
        var hidden = ParseYesNo(Cell(StockImportColumn.Hidden), "مخفي", errors);
        var serial = ParseSerial(Cell(StockImportColumn.Serial), quantity, errors);
        var note = ParseNote(Cell(StockImportColumn.InternalNote), errors);

        var itemsToCreate = errors.Count > 0 ? 0 : StockItemRules.UnitsPerItem(condition!, quantity).Count;
        ReceiptLine? line = errors.Count > 0 ? null : new ReceiptLine(modelName!, condition!, quantity, cost!.Value, price!.Value,
            category!.Id, null, withBoard is { } board ? board - price.Value : null, alone is { } single ? single - price.Value : null,
            serial, null, warrantyLabel, warranty, hasBox, hidden, note, BrandId: brand?.Id);
        return new StockImportRow(row, modelName ?? Cell(StockImportColumn.Model), condition ?? string.Empty, quantity, itemsToCreate, errors, line);
    }

    /// <summary>Parses a number the way people type it in a sheet: Arabic-Indic digits, thousands separators and an Arabic decimal mark.</summary>
    public static bool TryParseNumber(string text, out decimal value)
    {
        var plain = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (character is >= '٠' and <= '٩') plain.Append((char)('0' + character - '٠'));
            else if (character is >= '۰' and <= '۹') plain.Append((char)('0' + character - '۰'));
            else if (character == '٫') plain.Append('.');
            else if (character is ',' or '٬' or ' ' or ' ') continue;
            else plain.Append(character);
        }
        return decimal.TryParse(plain.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture, out value);
    }

    private static bool Matches(string header, string name) => string.Equals(Collapse(header), name, StringComparison.OrdinalIgnoreCase);

    private static string Collapse(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static StockImportCategory? FindCategory(IReadOnlyList<StockImportCategory> categories, string text, List<string> errors)
    {
        if (text.Length == 0)
        {
            errors.Add("القسم مطلوب");
            return null;
        }
        var found = categories.FirstOrDefault(category => string.Equals(category.Code, text, StringComparison.OrdinalIgnoreCase))
            ?? categories.FirstOrDefault(category => string.Equals(category.Name, text, StringComparison.OrdinalIgnoreCase));
        if (found is null) errors.Add($"القسم \"{text}\" مش موجود");
        return found;
    }

    /// <summary>An optional brand from the brand list, by name or slug (any case).</summary>
    private static StockImportBrand? FindBrand(IReadOnlyList<StockImportBrand> brands, string text, List<string> errors)
    {
        if (text.Length == 0) return null;
        var found = brands.FirstOrDefault(brand => string.Equals(brand.Name, text, StringComparison.OrdinalIgnoreCase)
            || string.Equals(brand.Slug, text, StringComparison.OrdinalIgnoreCase));
        if (found is null) errors.Add($"الماركة \"{text}\" مش موجودة في قائمة الماركات");
        return found;
    }

    private static string? ParseModel(string text, List<string> errors)
    {
        try
        {
            return StockItemRules.CleanModelName(text);
        }
        catch (RetailException)
        {
            errors.Add(text.Length == 0 ? "الموديل مطلوب" : $"الموديل طويل أوي (أقصى {ModelName.MaxLength} حرف)");
            return null;
        }
    }

    private static string? ParseCondition(string text, List<string> errors)
    {
        switch (text.ToLowerInvariant())
        {
            case "جديد" or "new": return StockCondition.New;
            case "مستعمل" or "used": return StockCondition.Used;
            default:
                errors.Add("الحالة لازم تكون جديد أو مستعمل");
                return null;
        }
    }

    private static int? ParseWholeNumber(string text, string column, int min, int max, List<string> errors)
    {
        if (text.Length == 0) return null;
        if (!TryParseNumber(text, out var number) || number != decimal.Truncate(number) || number < min || number > max)
        {
            errors.Add($"{column} لازم تكون رقم صحيح من {min} لـ {max}");
            return null;
        }
        return (int)number;
    }

    private static decimal? ParseMoney(string text, string column, bool required, List<string> errors)
    {
        if (text.Length == 0)
        {
            if (required) errors.Add($"{column} مطلوبة");
            return null;
        }
        if (!TryParseNumber(text, out var number) || number < 0 || number > StockImportRules.MaxMoney)
        {
            errors.Add($"{column} لازم تكون رقم من صفر وأكتر");
            return null;
        }
        return Math.Round(number, 2);
    }

    /// <summary>An optional full price for another way of buying; it cannot be below the in-build price.</summary>
    private static decimal? ParseFullPrice(string text, string column, decimal? price, List<string> errors)
    {
        var full = ParseMoney(text, column, required: false, errors);
        if (full is null || price is null) return full;
        if (full < price) errors.Add($"{column} لازم تكون أكبر من أو تساوي السعر في تجميعة");
        return full;
    }

    private static bool ParseYesNo(string text, string column, List<string> errors)
    {
        switch (text.ToLowerInvariant())
        {
            case "" or "لا" or "no" or "false" or "0": return false;
            case "نعم" or "yes" or "true" or "1": return true;
            default:
                errors.Add($"{column} لازم تكون نعم أو لا");
                return false;
        }
    }

    private static string? ParseSerial(string text, int quantity, List<string> errors)
    {
        if (text.Length == 0) return null;
        if (quantity != 1) errors.Add("السيريال ينفع بس لما الكمية 1");
        else if (text.Length > StockItemRules.MaxSerialLength) errors.Add("السيريال طويل أوي");
        return text;
    }

    private static string? ParseNote(string text, List<string> errors)
    {
        try
        {
            return StockItemRules.CleanNote(text);
        }
        catch (RetailException)
        {
            errors.Add($"الملاحظة طويلة أوي (أقصى {StockItemRules.MaxInternalNoteLength} حرف)");
            return null;
        }
    }
}

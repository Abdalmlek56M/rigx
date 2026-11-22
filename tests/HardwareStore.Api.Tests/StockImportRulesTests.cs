using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>How one sheet row becomes an intake line, without a database.</summary>
public sealed class StockImportRulesTests
{
    private static readonly Guid GpuId = Guid.NewGuid();
    private static readonly Guid MsiId = Guid.NewGuid();
    private static readonly StockImportLookups Lookups = new(
        [new StockImportCategory(GpuId, "GPU", "كروت الشاشة"), new StockImportCategory(Guid.NewGuid(), "RAM", "Memory")],
        [new StockImportBrand(MsiId, "msi", "MSI")],
        new Dictionary<int, string> { [12] = "سنة" });

    private static readonly string[] Header = [.. StockImportRules.Columns.Select(column => column.Arabic)];

    private static StockImportRow Parse(params string[] cells)
    {
        var (indexes, missing) = StockImportRules.MapHeader(Header);
        Assert.Empty(missing);
        return StockImportRules.ParseRow(2, cells, indexes, Lookups);
    }

    [Fact]
    public void A_full_row_becomes_a_receipt_line_with_the_extras_as_differences_from_the_base_price()
    {
        var row = Parse("gpu", "  RTX   3060 ", "مستعمل", "1", "8000", "9500", "10500", "10000", "12", "نعم", "SN1", "من فلان", "لا");

        Assert.Empty(row.Errors);
        var line = row.Line!;
        Assert.Equal(("RTX 3060", "Used", 1, 8000m, 9500m), (line.ModelName, line.Condition, line.Quantity, line.AcquisitionCost, line.Price));
        Assert.Equal((GpuId, 1000m, 500m), (line.CategoryId, line.WithBoardExtra, line.AloneExtra));
        Assert.Equal(("سنة", 12, true, false, "SN1", "من فلان"),
            (line.WarrantyLabel, line.WarrantyMonths, line.HasBox, line.IsHidden, line.SerialNumber, line.InternalNote));
        Assert.Equal(1, row.ItemsToCreate);
    }

    [Fact]
    public void Optional_cells_may_be_blank_and_english_words_and_category_names_work()
    {
        var row = Parse("memory", "DDR4 16GB", "New", "", "500", "900", "", "", "", "", "", "", "");

        Assert.Empty(row.Errors);
        Assert.Equal((1, "New", false), (row.Line!.Quantity, row.Line.Condition, row.Line.HasBox));
        Assert.Null(row.Line.WithBoardExtra);
        Assert.Null(row.Line.WarrantyMonths);
    }

    [Theory]
    [InlineData("١٢٬٥٠٠", 12500)]
    [InlineData("12,500", 12500)]
    [InlineData("١٢٫٥", 12.5)]
    [InlineData(" 1 200.50 ", 1200.5)]
    [InlineData("1.25E+4", 12500)]
    public void Numbers_are_normalised(string text, double expected)
    {
        Assert.True(StockImportRules.TryParseNumber(text, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Fact]
    public void Used_is_one_item_per_piece_and_new_is_one_lot()
    {
        Assert.Equal(2, Parse("GPU", "A", "مستعمل", "٢", "1", "2", "", "", "", "", "", "", "").ItemsToCreate);
        Assert.Equal(1, Parse("GPU", "A", "جديد", "3", "1", "2", "", "", "", "", "", "", "").ItemsToCreate);
    }

    [Fact]
    public void Every_problem_in_a_row_is_reported_in_arabic_and_the_row_has_no_line()
    {
        var row = Parse("Nope", "", "قديم", "0", "-5", "", "", "", "99", "ممكن", "", "", "");

        Assert.Null(row.Line);
        Assert.Contains("القسم \"Nope\" غير موجود", row.Errors);
        Assert.Contains("الموديل مطلوب", row.Errors);
        Assert.Contains("يجب أن تكون الحالة «جديد» أو «مستعمل»", row.Errors);
        Assert.Contains(row.Errors, error => error.Contains("الكمية"));
        Assert.Contains(row.Errors, error => error.Contains("التكلفة"));
        Assert.Contains("الحقل السعر في تجميعة مطلوب", row.Errors);
        Assert.Contains(row.Errors, error => error.StartsWith("الضمان 99 شهرًا"));
        Assert.Contains(row.Errors, error => error.Contains("بالكرتونة"));
    }

    [Fact]
    public void The_brand_is_optional_and_matched_by_name_or_slug_from_the_brand_list()
    {
        Assert.Null(Parse("GPU", "A", "جديد", "1", "1", "2", "", "", "", "", "", "", "", "").Line!.BrandId);
        Assert.Equal(MsiId, Parse("GPU", "A", "جديد", "1", "1", "2", "", "", "", "", "", "", "", "msi").Line!.BrandId);
        Assert.Equal(MsiId, Parse("GPU", "A", "جديد", "1", "1", "2", "", "", "", "", "", "", "", "MSI").Line!.BrandId);
        Assert.Equal(["الماركة \"Nope\" غير موجودة في قائمة الماركات"], Parse("GPU", "A", "جديد", "1", "1", "2", "", "", "", "", "", "", "", "Nope").Errors);
    }

    [Fact]
    public void A_warranty_must_be_one_of_the_store_warranty_options()
    {
        var row = Parse("GPU", "A", "جديد", "1", "1", "100", "", "", "6", "", "", "", "");

        Assert.Equal(["الضمان 6 شهرًا غير موجود في قائمة الضمانات في الإعدادات"], row.Errors);
    }

    [Fact]
    public void Full_prices_cannot_be_below_the_base_price_and_a_serial_needs_a_single_piece()
    {
        var below = Parse("GPU", "A", "جديد", "1", "1", "100", "90", "", "", "", "", "", "");
        Assert.Equal(["يجب ألا تقل قيمة السعر مع بوردة عن السعر في تجميعة"], below.Errors);

        var serial = Parse("GPU", "A", "جديد", "2", "1", "100", "", "", "", "", "SN", "", "");
        Assert.Equal(["يُسمح بالرقم التسلسلي فقط عندما تكون الكمية 1"], serial.Errors);
    }

    [Fact]
    public void The_header_accepts_english_aliases_in_any_case_and_names_the_missing_required_columns()
    {
        var (indexes, missing) = StockImportRules.MapHeader(["  CATEGORY ", "model", "Condition", "cost", "Price in build", "extra"]);
        Assert.Empty(missing);
        Assert.Equal(5, indexes.Count);

        var (_, absent) = StockImportRules.MapHeader(["القسم", "الموديل"]);
        Assert.Equal(["الحالة", "التكلفة", "السعر في تجميعة"], absent);
    }
}

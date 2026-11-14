using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Export;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The three export writers and the export rules, without a database.</summary>
public sealed class ExportWritersTests
{
    private static readonly TimeZoneInfo Cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

    private static ExportTable Sample() => new(["name", "amount", "day", "note"],
    [
        [ExportCell.Text("Ali, \"the\" tech"), ExportCell.Number(12.5m), ExportCell.Date(new DateOnly(2026, 11, 14)), ExportCell.Text("=1+1")],
        [ExportCell.Text("إيجار"), ExportCell.Number(100m), ExportCell.Empty, ExportCell.Text("line1\nline2")],
        [ExportCell.Text("+risky"), ExportCell.Number((decimal?)null), ExportCell.Date((DateOnly?)null), ExportCell.Text("@here -x")]
    ]);

    [Fact]
    public void Csv_has_a_byte_order_mark_quotes_per_rfc_4180_and_neutralizes_formulas()
    {
        var bytes = CsvExportWriter.Write(Sample());

        Assert.Equal("EFBBBF", Convert.ToHexString(bytes, 0, 3));
        var lines = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).Split("\r\n");
        Assert.Equal("name,amount,day,note", lines[0]);
        Assert.Equal("\"Ali, \"\"the\"\" tech\",12.5,2026-11-14,'=1+1", lines[1]);
        Assert.Equal("إيجار,100,,\"line1\nline2\"", lines[2]);
        Assert.Equal("'+risky,,,'@here -x", lines[3]);
    }

    [Fact]
    public void Csv_rows_end_with_crlf_and_text_starting_with_a_formula_character_gets_an_apostrophe()
    {
        var text = Encoding.UTF8.GetString(CsvExportWriter.Write(new ExportTable(["a"],
            [[ExportCell.Text("+1")], [ExportCell.Text("-1")], [ExportCell.Text("@x")], [ExportCell.Text("ok")]]))).TrimStart('﻿');

        Assert.Equal("a\r\n'+1\r\n'-1\r\n'@x\r\nok\r\n", text);
    }

    [Fact]
    public void Xlsx_is_a_package_whose_sheet_holds_the_cells_as_text_and_numbers_right_to_left()
    {
        using var package = new ZipArchive(new MemoryStream(XlsxExportWriter.Write(Sample())));

        Assert.Equal(["[Content_Types].xml", "_rels/.rels", "xl/_rels/workbook.xml.rels", "xl/workbook.xml", "xl/worksheets/sheet1.xml"],
            package.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).ToArray());
        var sheet = XDocument.Load(package.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        Assert.Equal("1", sheet.Descendants(main + "sheetView").Single().Attribute("rightToLeft")!.Value);
        var cells = sheet.Descendants(main + "c").ToDictionary(cell => cell.Attribute("r")!.Value);
        Assert.Equal("name", cells["A1"].Value);
        Assert.Equal("Ali, \"the\" tech", cells["A2"].Value);
        Assert.Equal("inlineStr", cells["A2"].Attribute("t")!.Value);
        Assert.Equal("12.5", cells["B2"].Value);
        Assert.Null(cells["B2"].Attribute("t"));
        Assert.Equal("2026-11-14", cells["C2"].Value);
        Assert.Equal("=1+1", cells["D2"].Value);
        Assert.Equal("إيجار", cells["A3"].Value);
        Assert.False(cells.ContainsKey("C3"));
        Assert.Equal(3, sheet.Descendants(main + "row").Count(row => row.Attribute("r")!.Value != "1"));
    }

    [Fact]
    public void Xlsx_column_letters_continue_after_z()
    {
        var columns = Enumerable.Range(0, 28).Select(index => $"c{index}").ToList();
        var table = new ExportTable(columns, [[.. columns.Select(ExportCell.Text)]]);
        using var package = new ZipArchive(new MemoryStream(XlsxExportWriter.Write(table)));

        var sheet = new StreamReader(package.GetEntry("xl/worksheets/sheet1.xml")!.Open()).ReadToEnd();

        Assert.Contains("r=\"Z1\"", sheet);
        Assert.Contains("r=\"AA1\"", sheet);
        Assert.Contains("r=\"AB2\"", sheet);
    }

    [Fact]
    public void Xml_has_an_export_root_with_a_row_per_record_and_a_col_per_column()
    {
        var generated = new DateTimeOffset(2026, 11, 14, 9, 30, 15, TimeSpan.Zero);

        var document = XDocument.Parse(Encoding.UTF8.GetString(XmlExportWriter.Write(Sample(), "orders", generated)));

        var root = document.Root!;
        Assert.Equal("export", root.Name.LocalName);
        Assert.Equal("orders", root.Attribute("dataset")!.Value);
        Assert.Equal("2026-11-14T09:30:15Z", root.Attribute("generatedAtUtc")!.Value);
        var rows = root.Elements("row").ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal(["name", "amount", "day", "note"], rows[0].Elements("col").Select(col => col.Attribute("name")!.Value).ToArray());
        Assert.Equal(["Ali, \"the\" tech", "12.5", "2026-11-14", "=1+1"], rows[0].Elements("col").Select(col => col.Value).ToArray());
        Assert.Equal("", rows[2].Elements("col").ElementAt(1).Value);
    }

    [Fact]
    public void Xml_and_xlsx_drop_control_characters_that_would_break_the_file()
    {
        var table = new ExportTable(["a"], [[ExportCell.Text("x\u0001y")]]);

        Assert.Equal("xy", XDocument.Parse(Encoding.UTF8.GetString(XmlExportWriter.Write(table, "t", DateTimeOffset.UnixEpoch))).Descendants("col").Single().Value);
        using var package = new ZipArchive(new MemoryStream(XlsxExportWriter.Write(table)));
        Assert.Contains(">xy<", new StreamReader(package.GetEntry("xl/worksheets/sheet1.xml")!.Open()).ReadToEnd());
    }

    [Fact]
    public void Formats_are_parsed_and_anything_else_is_a_400()
    {
        Assert.Equal(ExportFormat.Xlsx, ExportFiles.ParseFormat(" XLSX "));
        Assert.Equal(400, Assert.Throws<ExportException>(() => ExportFiles.ParseFormat("pdf")).StatusCode);
        Assert.Equal(400, Assert.Throws<ExportException>(() => ExportFiles.ParseFormat(null)).StatusCode);
    }

    [Fact]
    public void An_instant_is_shown_as_cairo_business_time()
    {
        var cell = ExportCell.Instant(new DateTimeOffset(2026, 11, 14, 22, 30, 0, TimeSpan.Zero), Cairo);

        Assert.Equal("2026-11-15 00:30", cell.AsText());
        Assert.Equal(string.Empty, ExportCell.Instant(null, Cairo).AsText());
    }

    [Fact]
    public void The_range_defaults_to_the_last_30_days_and_rejects_an_end_before_the_start()
    {
        var today = new DateOnly(2026, 11, 14);

        Assert.Equal((new DateOnly(2026, 10, 15), today), ExportRules.Range(null, null, today));
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), ExportRules.Range(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), today));
        Assert.Equal(400, Assert.Throws<ExportException>(() => ExportRules.Range(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1), today)).StatusCode);
    }

    [Fact]
    public void More_than_50000_rows_is_a_409()
    {
        ExportRules.EnsureWithinLimit(ExportRules.MaxRows);

        Assert.Equal(409, Assert.Throws<ExportException>(() => ExportRules.EnsureWithinLimit(ExportRules.MaxRows + 1)).StatusCode);
        Assert.Equal(ExportRules.MaxRows + 1, ExportRules.ReadLimit);
    }
}

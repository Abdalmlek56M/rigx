using System.IO.Compression;
using System.Text;
using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Export;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The minimal .xlsx reader used by the stock import, without a database.</summary>
public sealed class XlsxImportReaderTests
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static byte[] Package(params (string Path, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var package = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (path, content) in entries)
            {
                using var writer = new StreamWriter(package.CreateEntry(path).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        return stream.ToArray();
    }

    private static (string Path, string Content) Sheet(string rows, string path = "xl/worksheets/sheet1.xml") =>
        (path, $"<worksheet xmlns=\"{Main}\"><sheetData>{rows}</sheetData></worksheet>");

    [Fact]
    public void A_workbook_written_by_the_export_writer_reads_back_as_text()
    {
        var table = new ExportTable(["اسم", "مبلغ"],
            [[ExportCell.Text("كارت  شاشة"), ExportCell.Number(12.5m)], [ExportCell.Empty, ExportCell.Number(7m)]]);

        var rows = XlsxImportReader.ReadFirstSheet(XlsxExportWriter.Write(table));

        Assert.Equal(["اسم", "مبلغ"], rows[0]);
        Assert.Equal(["كارت  شاشة", "12.5"], rows[1]);
        Assert.Equal(["", "7"], rows[2]);
    }

    [Fact]
    public void Shared_strings_booleans_inline_strings_and_skipped_columns_are_read_by_cell_reference()
    {
        var shared = $"<sst xmlns=\"{Main}\"><si><t>Category</t></si><si><r><t>RTX </t></r><r><t>3060</t></r><rPh><t>ignored</t></rPh></si></sst>";
        var rows = XlsxImportReader.ReadFirstSheet(Package(
            Sheet("<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"D1\" t=\"s\"><v>1</v></c></row>" +
                  "<row r=\"3\"><c r=\"B3\" t=\"inlineStr\"><is><t> padded </t></is></c><c r=\"C3\" t=\"b\"><v>1</v></c><c r=\"E3\"><v>42</v></c></row>"),
            ("xl/sharedStrings.xml", shared)));

        Assert.Equal(["Category", "", "", "RTX 3060"], rows[0]);
        Assert.Empty(rows[1]);
        Assert.Equal(["", "padded", "TRUE", "", "42"], rows[2]);
    }

    [Fact]
    public void The_first_sheet_is_found_through_the_workbook_relationships()
    {
        var workbook = $"<workbook xmlns=\"{Main}\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                       "<sheets><sheet name=\"Data\" sheetId=\"1\" r:id=\"rId7\"/></sheets></workbook>";
        var rels = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId7\" Type=\"worksheet\" Target=\"worksheets/data.xml\"/></Relationships>";

        var rows = XlsxImportReader.ReadFirstSheet(Package(
            ("xl/workbook.xml", workbook), ("xl/_rels/workbook.xml.rels", rels),
            Sheet("<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>x</t></is></c></row>", "xl/worksheets/data.xml"),
            Sheet("<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>wrong</t></is></c></row>")));

        Assert.Equal(["x"], Assert.Single(rows));
    }

    [Fact]
    public void A_zip_that_is_not_a_workbook_is_a_clean_error()
    {
        Assert.Throws<XlsxReadException>(() => XlsxImportReader.ReadFirstSheet(Encoding.UTF8.GetBytes("not a zip at all")));
        Assert.Throws<XlsxReadException>(() => XlsxImportReader.ReadFirstSheet(Package(("readme.txt", "hello"))));
        Assert.Throws<XlsxReadException>(() => XlsxImportReader.ReadFirstSheet(Package(("xl/worksheets/sheet1.xml", "<worksheet><sheetData>"))));
    }

    [Fact]
    public void Too_many_parts_a_doctype_or_too_many_rows_are_refused()
    {
        var manyParts = Package([.. Enumerable.Range(0, XlsxImportReader.MaxEntries + 1).Select(index => ($"part{index}.txt", "x"))]);
        Assert.Throws<XlsxReadException>(() => XlsxImportReader.ReadFirstSheet(manyParts));

        var doctype = Package(("xl/worksheets/sheet1.xml", "<!DOCTYPE worksheet [<!ENTITY x \"boom\">]><worksheet><sheetData/></worksheet>"));
        Assert.Throws<XlsxReadException>(() => XlsxImportReader.ReadFirstSheet(doctype));

        var last = XlsxImportReader.MaxRows + 1;
        var tooMany = Package(Sheet($"<row r=\"{last}\"><c r=\"A{last}\"><v>1</v></c></row>"));
        Assert.Throws<XlsxReadException>(() => XlsxImportReader.ReadFirstSheet(tooMany));
        var styledEmptyRow = Package(Sheet($"<row r=\"{last}\"><c r=\"A{last}\" s=\"1\"/></row>"));
        Assert.Empty(XlsxImportReader.ReadFirstSheet(styledEmptyRow));
    }
}

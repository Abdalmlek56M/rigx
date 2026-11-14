using System.IO.Compression;
using System.Security;
using System.Text;
using HardwareStore.Common.Domain;

namespace HardwareStore.Common.Infrastructure.Export;

/// <summary>
/// A minimal Excel workbook (one right-to-left sheet): text as inline strings, numbers as numbers, dates as ISO text.
/// Inline strings are never read as formulas, so no neutralizing is needed here.
/// </summary>
public static class XlsxExportWriter
{
    private const string MainNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string Declaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";

    public static byte[] Write(ExportTable table)
    {
        using var stream = new MemoryStream();
        using (var package = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(package, "[Content_Types].xml",
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "</Types>");
            Add(package, "_rels/.rels", Relationship("officeDocument", "xl/workbook.xml"));
            Add(package, "xl/workbook.xml",
                $"<workbook xmlns=\"{MainNamespace}\" xmlns:r=\"{RelationshipNamespace}\"><sheets><sheet name=\"export\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add(package, "xl/_rels/workbook.xml.rels", Relationship("worksheet", "worksheets/sheet1.xml"));
            Add(package, "xl/worksheets/sheet1.xml", Sheet(table));
        }
        return stream.ToArray();
    }

    private static string Relationship(string type, string target) =>
        $"<Relationships xmlns=\"{PackageRelationshipNamespace}\"><Relationship Id=\"rId1\" Type=\"{RelationshipNamespace}/{type}\" Target=\"{target}\"/></Relationships>";

    private static string Sheet(ExportTable table)
    {
        var sheet = new StringBuilder($"<worksheet xmlns=\"{MainNamespace}\">")
            .Append("<sheetViews><sheetView rightToLeft=\"1\" workbookViewId=\"0\"/></sheetViews><sheetData>");
        AppendRow(sheet, 1, [.. table.Columns.Select(ExportCell.Text)]);
        for (var index = 0; index < table.Rows.Count; index++) AppendRow(sheet, index + 2, table.Rows[index]);
        return sheet.Append("</sheetData></worksheet>").ToString();
    }

    private static void AppendRow(StringBuilder sheet, int rowNumber, IReadOnlyList<ExportCell> cells)
    {
        sheet.Append("<row r=\"").Append(rowNumber).Append("\">");
        for (var index = 0; index < cells.Count; index++)
        {
            var reference = ColumnName(index) + rowNumber;
            if (cells[index] is ExportCell.NumberCell)
                sheet.Append("<c r=\"").Append(reference).Append("\"><v>").Append(cells[index].AsText()).Append("</v></c>");
            else if (cells[index].AsText() is { Length: > 0 } text)
                sheet.Append("<c r=\"").Append(reference).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">")
                    .Append(SecurityElement.Escape(ExportText.ForXml(text))).Append("</t></is></c>");
        }
        sheet.Append("</row>");
    }

    /// <summary>Spreadsheet column letters: 0 = A, 25 = Z, 26 = AA.</summary>
    private static string ColumnName(int index)
    {
        var name = string.Empty;
        for (var number = index + 1; number > 0; number = (number - 1) / 26)
            name = (char)('A' + (number - 1) % 26) + name;
        return name;
    }

    private static void Add(ZipArchive package, string path, string content)
    {
        using var writer = new StreamWriter(package.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(Declaration + content);
    }
}

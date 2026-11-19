using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using HardwareStore.Common.Domain;

namespace HardwareStore.Common.Infrastructure.Export;

/// <summary>The file is not a readable workbook, or it breaks a safety limit (too many parts, too big, too many rows).</summary>
public sealed class XlsxReadException(string message) : AppException(400, message);

/// <summary>
/// A minimal, hardened reader for the first sheet of an .xlsx file: shared and inline strings, numbers and booleans,
/// as trimmed text per cell (empty text for a blank one). Row <c>n</c> is at index <c>n - 1</c>, so a skipped row is an empty list.
/// Formulas are never evaluated; the cached value is read as it is.
/// </summary>
public static class XlsxImportReader
{
    public const int MaxEntries = 50;
    public const int MaxEntryBytes = 20 * 1024 * 1024;
    public const int MaxRows = 2000;
    public const int MaxColumns = 100;

    private const string DefaultSheetPath = "xl/worksheets/sheet1.xml";

    public static IReadOnlyList<IReadOnlyList<string>> ReadFirstSheet(byte[] content)
    {
        try
        {
            using var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);
            if (archive.Entries.Count > MaxEntries) throw new XlsxReadException("The workbook has too many parts.");
            if (archive.Entries.Any(entry => entry.Length > MaxEntryBytes)) throw new XlsxReadException("The workbook is too large.");
            var sheet = archive.GetEntry(SheetPath(archive)) ?? throw new XlsxReadException("The file has no worksheet.");
            var sharedStrings = archive.GetEntry("xl/sharedStrings.xml") is { } entry ? ReadSharedStrings(entry) : [];
            return ReadRows(sheet, sharedStrings);
        }
        catch (InvalidDataException exception)
        {
            throw new XlsxReadException($"The file is not a workbook: {exception.Message}");
        }
        catch (XmlException exception)
        {
            throw new XlsxReadException($"The workbook is damaged: {exception.Message}");
        }
    }

    /// <summary>The first sheet through the workbook and its relationships; the conventional <c>sheet1.xml</c> when they are missing.</summary>
    private static string SheetPath(ZipArchive archive)
    {
        if (archive.GetEntry("xl/workbook.xml") is not { } workbook || archive.GetEntry("xl/_rels/workbook.xml.rels") is not { } rels)
            return DefaultSheetPath;
        var sheet = Load(workbook).Descendants().FirstOrDefault(element => element.Name.LocalName == "sheet");
        var relationshipId = sheet?.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id")?.Value;
        var target = Load(rels).Descendants().Where(element => element.Name.LocalName == "Relationship")
            .FirstOrDefault(element => element.Attribute("Id")?.Value == relationshipId)?.Attribute("Target")?.Value;
        if (string.IsNullOrWhiteSpace(target)) return DefaultSheetPath;
        return target.StartsWith('/') ? target[1..] : "xl/" + target;
    }

    private static List<string> ReadSharedStrings(ZipArchiveEntry entry)
    {
        var strings = new List<string>();
        using var reader = Open(entry);
        reader.MoveToContent();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "si") strings.Add(TextOf((XElement)XNode.ReadFrom(reader)));
            else reader.Read();
        }
        return strings;
    }

    private static List<IReadOnlyList<string>> ReadRows(ZipArchiveEntry entry, List<string> sharedStrings)
    {
        var rows = new List<IReadOnlyList<string>>();
        using var reader = Open(entry);
        reader.MoveToContent();
        while (!reader.EOF)
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "row")
            {
                reader.Read();
                continue;
            }
            var row = (XElement)XNode.ReadFrom(reader);
            var rowNumber = int.TryParse(row.Attribute("r")?.Value, out var declared) && declared > rows.Count ? declared : rows.Count + 1;
            var cells = ReadCells(row, sharedStrings);
            if (rowNumber > MaxRows)
            {
                if (cells.Any(cell => cell.Length > 0)) throw new XlsxReadException($"The sheet has more than {MaxRows} rows.");
                continue;
            }
            while (rows.Count < rowNumber - 1) rows.Add([]);
            rows.Add(cells);
        }
        return rows;
    }

    private static List<string> ReadCells(XElement row, List<string> sharedStrings)
    {
        var cells = new List<string>();
        var column = 0;
        foreach (var cell in row.Elements().Where(element => element.Name.LocalName == "c"))
        {
            column = ColumnIndex(cell.Attribute("r")?.Value) ?? column + 1;
            if (column > MaxColumns) continue;
            while (cells.Count < column) cells.Add(string.Empty);
            cells[column - 1] = CellText(cell, sharedStrings);
        }
        return cells;
    }

    private static string CellText(XElement cell, List<string> sharedStrings)
    {
        var value = cell.Elements().FirstOrDefault(element => element.Name.LocalName == "v")?.Value ?? string.Empty;
        return (cell.Attribute("t")?.Value switch
        {
            "s" => int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count ? sharedStrings[index] : string.Empty,
            "inlineStr" => TextOf(cell.Elements().FirstOrDefault(element => element.Name.LocalName == "is")),
            "b" => value == "1" ? "TRUE" : "FALSE",
            "e" => string.Empty,
            _ => value
        }).Trim();
    }

    /// <summary>The text of a string item: its text runs, without the phonetic hints.</summary>
    private static string TextOf(XElement? item) => item is null
        ? string.Empty
        : string.Concat(item.Descendants().Where(element => element.Name.LocalName == "t" && element.Parent?.Name.LocalName != "rPh")
            .Select(element => element.Value));

    /// <summary>1-based column from a reference such as "C7" or "AB12"; null when the reference is missing or has no letters.</summary>
    private static int? ColumnIndex(string? reference)
    {
        var column = 0;
        foreach (var character in reference ?? string.Empty)
        {
            if (!char.IsAsciiLetter(character)) break;
            column = column * 26 + (char.ToUpperInvariant(character) - 'A' + 1);
            if (column > 16384) break;
        }
        return column == 0 ? null : column;
    }

    private static XElement Load(ZipArchiveEntry entry)
    {
        using var reader = Open(entry);
        return XElement.Load(reader);
    }

    /// <summary>The part as a safe XML reader: no DTD, no external resources, and never more than <see cref="MaxEntryBytes"/> once unpacked.</summary>
    private static XmlReader Open(ZipArchiveEntry entry)
    {
        var buffer = new MemoryStream();
        using (var source = entry.Open())
        {
            var chunk = new byte[81920];
            int read;
            while ((read = source.Read(chunk, 0, chunk.Length)) > 0)
            {
                if (buffer.Length + read > MaxEntryBytes) throw new XlsxReadException("The workbook is too large.");
                buffer.Write(chunk, 0, read);
            }
        }
        buffer.Position = 0;
        return XmlReader.Create(buffer, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
    }
}

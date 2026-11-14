using System.Text;
using HardwareStore.Common.Domain;

namespace HardwareStore.Common.Infrastructure.Export;

/// <summary>
/// CSV for Excel: UTF-8 with a byte-order mark (so Arabic shows right), RFC 4180 quoting, CRLF rows, and a leading
/// apostrophe on text that could start a spreadsheet formula.
/// </summary>
public static class CsvExportWriter
{
    public static byte[] Write(ExportTable table)
    {
        var text = new StringBuilder();
        AppendRow(text, table.Columns);
        foreach (var row in table.Rows) AppendRow(text, row.Select(Cell));
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text.ToString())];
    }

    private static void AppendRow(StringBuilder text, IEnumerable<string> cells) =>
        text.Append(string.Join(',', cells.Select(Quote))).Append("\r\n");

    private static string Cell(ExportCell cell) =>
        cell is ExportCell.TextCell { Value: { Length: > 0 } value } ? Neutralize(value) : cell.AsText();

    private static string Neutralize(string value) => value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;

    private static string Quote(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}

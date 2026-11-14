using System.Text;

namespace HardwareStore.Modules.Retail;

/// <summary>CSV text for the expense export; cells are quoted when needed and cannot start a spreadsheet formula.</summary>
internal static class ExpenseCsv
{
    public static readonly IReadOnlyList<string> Header =
        ["date", "type", "classification", "amount", "paid from", "order reference", "unit code", "build code", "note"];

    public static string Build(IEnumerable<IReadOnlyList<string?>> rows)
    {
        var text = new StringBuilder();
        foreach (var row in rows.Prepend(Header)) text.Append(string.Join(',', row.Select(Cell))).Append("\r\n");
        return text.ToString();
    }

    private static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r') value = "'" + value;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}

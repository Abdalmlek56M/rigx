namespace HardwareStore.Common.Infrastructure.Export;

internal static class ExportText
{
    /// <summary>Removes the control characters XML cannot carry (everything below space except tab, line feed and carriage return).</summary>
    public static string ForXml(string value) =>
        new(value.Where(character => character >= ' ' || character is '\t' or '\n' or '\r').ToArray());
}

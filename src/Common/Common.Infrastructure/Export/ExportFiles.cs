using HardwareStore.Common.Domain;

namespace HardwareStore.Common.Infrastructure.Export;

public enum ExportFormat { Csv, Xlsx, Xml }

/// <summary>A written export: the bytes, the media type and the file extension (without the dot).</summary>
public sealed record ExportFile(byte[] Content, string ContentType, string Extension);

public static class ExportFiles
{
    public static ExportFormat ParseFormat(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "csv" => ExportFormat.Csv,
        "xlsx" => ExportFormat.Xlsx,
        "xml" => ExportFormat.Xml,
        _ => throw new ExportException(400, "The format must be csv, xlsx or xml.")
    };

    public static ExportFile Write(ExportTable table, ExportFormat format, string dataset, DateTimeOffset generatedAtUtc) => format switch
    {
        ExportFormat.Csv => new ExportFile(CsvExportWriter.Write(table), "text/csv; charset=utf-8", "csv"),
        ExportFormat.Xlsx => new ExportFile(XlsxExportWriter.Write(table),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
        _ => new ExportFile(XmlExportWriter.Write(table, dataset, generatedAtUtc), "application/xml; charset=utf-8", "xml")
    };
}

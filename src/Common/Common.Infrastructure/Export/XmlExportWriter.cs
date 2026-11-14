using System.Globalization;
using System.Text;
using System.Xml;
using HardwareStore.Common.Domain;

namespace HardwareStore.Common.Infrastructure.Export;

/// <summary>An <c>export</c> element (dataset, generatedAtUtc) holding one <c>row</c> per record with a <c>col</c> per column.</summary>
public static class XmlExportWriter
{
    public static byte[] Write(ExportTable table, string dataset, DateTimeOffset generatedAtUtc)
    {
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("export");
            writer.WriteAttributeString("dataset", dataset);
            writer.WriteAttributeString("generatedAtUtc",
                generatedAtUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            foreach (var row in table.Rows)
            {
                writer.WriteStartElement("row");
                for (var index = 0; index < table.Columns.Count; index++)
                {
                    writer.WriteStartElement("col");
                    writer.WriteAttributeString("name", ExportText.ForXml(table.Columns[index]));
                    writer.WriteString(ExportText.ForXml(row[index].AsText()));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        return stream.ToArray();
    }
}

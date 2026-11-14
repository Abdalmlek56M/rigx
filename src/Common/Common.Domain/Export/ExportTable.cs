namespace HardwareStore.Common.Domain;

/// <summary>A dataset ready to be written as CSV, Excel or XML: the column names and the rows of typed cells.</summary>
public sealed record ExportTable(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<ExportCell>> Rows);

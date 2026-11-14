using System.Text.Json;
using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Export;
using HardwareStore.Modules.Auditing;

/// <summary>A finished export with the file name the browser should save it as.</summary>
public sealed record ExportResult(ExportFile File, string FileName);

/// <summary>
/// Runs one export: checks the format and the date range (Cairo business days, last 30 days by default), builds the
/// dataset, enforces the row cap, writes the file and audits it.
/// </summary>
public sealed class ExportRunner(IServiceProvider services, ISecurityAuditWriter audit, TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    public const string ExportedEvent = "System.Exported";

    public async Task<ExportResult> RunAsync(ExportDataset dataset, string? formatText, DateOnly? from, DateOnly? to, bool includeCosts,
        bool includeContact, Guid actorId, CancellationToken cancellationToken)
    {
        var format = ExportFiles.ParseFormat(formatText);
        var (start, end) = ExportRules.Range(from, to, BusinessDay.Today(timeProvider, businessZone));
        var table = await dataset.Build(services, new ExportQuery(start, end, includeCosts, includeContact), cancellationToken);
        ExportRules.EnsureWithinLimit(table.Rows.Count);
        var file = ExportFiles.Write(table, format, dataset.Name, timeProvider.GetUtcNow());
        await audit.WriteAsync(new SecurityAuditEntry(ExportedEvent, "Succeeded", "User", actorId, SafeMetadata: JsonSerializer.Serialize(new
        {
            dataset = dataset.Name, format = file.Extension, from = start.ToString("yyyy-MM-dd"), to = end.ToString("yyyy-MM-dd"), rows = table.Rows.Count
        })), cancellationToken);
        return new ExportResult(file, $"rigx-{dataset.Name}-{start:yyyy-MM-dd}-{end:yyyy-MM-dd}.{file.Extension}");
    }
}

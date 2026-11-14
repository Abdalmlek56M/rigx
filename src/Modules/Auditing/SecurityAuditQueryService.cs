using System.Text.Json;
using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Auditing;

public sealed record SecurityAuditItemView(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string EventType,
    string Outcome,
    string ActorKind,
    Guid? ActorUserAccountId,
    Guid? SubjectUserAccountId,
    string? CorrelationId,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record SecurityAuditQuery(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    string? EventType = null,
    string? Outcome = null,
    Guid? ActorUserAccountId = null,
    Guid? SubjectUserAccountId = null,
    string? CorrelationId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 25);

/// <summary>
/// Read-only projection over the append-only security audit log. Only whitelisted,
/// non-sensitive metadata keys are ever returned; session hashes, tickets, raw IP
/// addresses, tokens and credentials are not part of the projection at all.
/// </summary>
public sealed class SecurityAuditQueryService(IDbContextFactory<SecurityAuditDbContext> contextFactory)
{
    private static readonly HashSet<string> AllowedMetadataKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "kind", "emailKind", "attempts", "deadLetter", "roles", "roleCode", "permissionCode",
        "addressId", "label", "isDefault", "reason", "code", "previousStatus", "status",
        "count", "targetType", "targetId", "channel", "provider", "from", "to", "days",
        "productId", "categoryId", "imageId", "storageKey", "warehouseId", "supplierId",
        "sessionId", "deviceName", "browser", "operatingSystem", "emailQueued"
    };

    private static readonly string[] SensitiveKeyFragments =
    [
        "token", "secret", "password", "hash", "ip", "email", "url", "cookie", "ticket", "sessionkey"
    ];

    public async Task<PagedResult<SecurityAuditItemView>> QueryAsync(
        SecurityAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FromUtc is not null && query.ToUtc is not null && query.FromUtc > query.ToUtc)
            throw new ArgumentException("The start of the range must precede the end.");

        var (page, pageSize) = PageRequest.Of(query.Page, query.PageSize);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.SecurityAuditEvents.AsNoTracking();

        if (query.FromUtc is not null)
            rows = rows.Where(entry => entry.OccurredAtUtc >= query.FromUtc.Value);
        if (query.ToUtc is not null)
            rows = rows.Where(entry => entry.OccurredAtUtc <= query.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            var pattern = $"%{EscapeLike(query.EventType.Trim())}%";
            rows = rows.Where(entry => EF.Functions.ILike(entry.EventType, pattern));
        }
        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            var outcome = query.Outcome.Trim();
            rows = rows.Where(entry => entry.Outcome == outcome);
        }
        if (query.ActorUserAccountId is not null)
            rows = rows.Where(entry => entry.ActorUserAccountId == query.ActorUserAccountId);
        if (query.SubjectUserAccountId is not null)
            rows = rows.Where(entry => entry.SubjectUserAccountId == query.SubjectUserAccountId);
        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            var correlation = query.CorrelationId.Trim();
            rows = rows.Where(entry => entry.CorrelationId == correlation);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            rows = rows.Where(entry =>
                EF.Functions.ILike(entry.EventType, pattern) ||
                (entry.CorrelationId != null && EF.Functions.ILike(entry.CorrelationId, pattern)));
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new
            {
                entry.Id,
                entry.OccurredAtUtc,
                entry.EventType,
                entry.Outcome,
                entry.ActorKind,
                entry.ActorUserAccountId,
                entry.SubjectUserAccountId,
                entry.CorrelationId,
                entry.SafeMetadata
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SecurityAuditItemView>(
            items.Select(entry => new SecurityAuditItemView(
                entry.Id,
                entry.OccurredAtUtc,
                entry.EventType,
                entry.Outcome,
                entry.ActorKind,
                entry.ActorUserAccountId,
                entry.SubjectUserAccountId,
                entry.CorrelationId,
                SafeMetadata(entry.SafeMetadata))).ToList(),
            page,
            pageSize,
            total);
    }

    private static IReadOnlyDictionary<string, string> SafeMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return new Dictionary<string, string>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(metadata);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, string>();
            var safe = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!AllowedMetadataKeys.Contains(property.Name))
                    continue;
                if (SensitiveKeyFragments.Any(fragment =>
                        property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var value = Render(property.Value);
                if (value is not null)
                    safe[property.Name] = value;
            }
            return safe;
        }
    }

    private static string? Render(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => Truncate(value.GetString()),
        JsonValueKind.Number => Truncate(value.GetRawText()),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Array => Truncate(string.Join(", ",
            value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item)))),
        _ => null
    };

    private static string? Truncate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null :
        value.Length <= 256 ? value : value[..256];

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}

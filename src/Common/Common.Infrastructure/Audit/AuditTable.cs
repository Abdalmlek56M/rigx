namespace HardwareStore.Common.Infrastructure.Audit;

/// <summary>The names of the append-only <c>audit_entries</c> table: the interceptor inserts into it and the Auditing module maps it.</summary>
public static class AuditTable
{
    public const string Name = "audit_entries";
    public const string Id = "id";
    public const string OccurredAtUtc = "occurred_at_utc";
    public const string ActorKind = "actor_kind";
    public const string ActorUserId = "actor_user_id";
    public const string ActorIp = "actor_ip";
    public const string ActorUserAgent = "actor_user_agent";
    public const string CorrelationId = "correlation_id";
    public const string Module = "module";
    public const string Entity = "entity";
    public const string EntityId = "entity_id";
    public const string Action = "action";
    public const string Changes = "changes";
    public const string RestoredFromEntryId = "restored_from_entry_id";
}

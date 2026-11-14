using HardwareStore.Common.Domain;

/// <summary>The audit actor read from the current request; without a request (background jobs) the change is the system's.</summary>
public sealed class HttpAuditActor(IHttpContextAccessor accessor) : IAuditActor
{
    private HttpContext? Context => accessor.HttpContext;

    public AuditActorKind Kind => Context is null ? AuditActorKind.System : UserId is null ? AuditActorKind.Anonymous : AuditActorKind.User;

    public Guid? UserId => Context is { } context && context.TryGetUserId(out var id) ? id : null;

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => AuditRules.ShortenUserAgent(Context?.Request.Headers.UserAgent.ToString());

    public string? CorrelationId => Context?.TraceIdentifier;
}

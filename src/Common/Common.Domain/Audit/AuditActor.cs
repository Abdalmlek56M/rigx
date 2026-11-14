namespace HardwareStore.Common.Domain;

public enum AuditActorKind
{
    Anonymous,
    User,
    System
}

/// <summary>Who is making the change being saved: read once per save by the audit interceptor. Host implements it from the request.</summary>
public interface IAuditActor
{
    AuditActorKind Kind { get; }
    Guid? UserId { get; }
    string? IpAddress { get; }

    /// <summary>The browser or device string, already shortened to <see cref="AuditRules.MaxUserAgentLength"/>.</summary>
    string? UserAgent { get; }

    string? CorrelationId { get; }
}

/// <summary>The actor when no request is running (background jobs, startup work).</summary>
public sealed class SystemAuditActor : IAuditActor
{
    public AuditActorKind Kind => AuditActorKind.System;
    public Guid? UserId => null;
    public string? IpAddress => null;
    public string? UserAgent => null;
    public string? CorrelationId => null;
}

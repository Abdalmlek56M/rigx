using System.Collections.Concurrent;

namespace HardwareStore.Modules.Identity;

/// <summary>A sign-in or step-up code (see <see cref="Authentication.EmailCodePurpose"/>).</summary>
public sealed record EmailCodeMessage(string Email, string FullName, string Code, DateTimeOffset ExpiresAtUtc, string Purpose);

/// <summary>Tells an invited staff member to sign in with this email (code or Google). No password, no link token.</summary>
public sealed record StaffInvitationMessage(string Email, string FullName, IReadOnlyList<string> Roles, Uri SignInUrl);

/// <summary>Link sent to the new address to confirm an email change.</summary>
public sealed record EmailChangeMessage(string NewEmail, string FullName, Uri ConfirmUrl);

/// <summary>Delivers Identity emails. Called only by the outbox processor, never inside a request.</summary>
public interface IEmailProvider
{
    Task SendEmailCodeAsync(EmailCodeMessage message, CancellationToken cancellationToken = default);
    Task SendStaffInvitationAsync(StaffInvitationMessage message, CancellationToken cancellationToken = default);
    Task SendEmailChangeVerificationAsync(EmailChangeMessage message, CancellationToken cancellationToken = default);
}

/// <summary>Development/Testing provider. Messages stay in process memory and are never logged or sent.</summary>
public sealed class InMemoryTestEmailProvider : IEmailProvider
{
    private readonly ConcurrentQueue<EmailCodeMessage> _emailCodes = new();
    private readonly ConcurrentQueue<StaffInvitationMessage> _staffInvitations = new();
    private readonly ConcurrentQueue<EmailChangeMessage> _emailChanges = new();

    public IReadOnlyCollection<EmailCodeMessage> EmailCodes => _emailCodes.ToArray();
    public IReadOnlyCollection<StaffInvitationMessage> StaffInvitations => _staffInvitations.ToArray();
    public IReadOnlyCollection<EmailChangeMessage> EmailChanges => _emailChanges.ToArray();

    public Task SendEmailCodeAsync(EmailCodeMessage message, CancellationToken cancellationToken = default) =>
        Capture(_emailCodes, message, cancellationToken);

    public Task SendStaffInvitationAsync(StaffInvitationMessage message, CancellationToken cancellationToken = default) =>
        Capture(_staffInvitations, message, cancellationToken);

    public Task SendEmailChangeVerificationAsync(EmailChangeMessage message, CancellationToken cancellationToken = default) =>
        Capture(_emailChanges, message, cancellationToken);

    private static Task Capture<T>(ConcurrentQueue<T> queue, T message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queue.Enqueue(message);
        return Task.CompletedTask;
    }
}

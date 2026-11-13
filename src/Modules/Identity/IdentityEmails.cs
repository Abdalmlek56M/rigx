using System.Collections.Concurrent;

namespace HardwareStore.Modules.Identity;

/// <summary>A sign-in or step-up code (see <see cref="Authentication.EmailCodePurpose"/>).</summary>
public sealed record EmailCodeMessage(string Email, string FullName, string Code, DateTimeOffset ExpiresAtUtc, string Purpose);

/// <summary>Tells an invited staff member to sign in with this email (code or Google). No password, no link token.</summary>
public sealed record StaffInvitationMessage(string Email, string FullName, IReadOnlyList<string> Roles, Uri SignInUrl);

/// <summary>Link sent to the new address to confirm an email change.</summary>
public sealed record EmailChangeMessage(string NewEmail, string FullName, Uri ConfirmUrl);

/// <summary>Tells the account owner about a sign-in from a device it has not used before.</summary>
public sealed record NewDeviceSignInMessage(string Email, string FullName, string Device, string Browser, DateTimeOffset SignedInAtUtc);

/// <summary>Delivers Identity emails. Called only by the outbox processor, never inside a request.</summary>
public interface IEmailProvider
{
    Task SendEmailCodeAsync(EmailCodeMessage message, CancellationToken cancellationToken = default);
    Task SendStaffInvitationAsync(StaffInvitationMessage message, CancellationToken cancellationToken = default);
    Task SendEmailChangeVerificationAsync(EmailChangeMessage message, CancellationToken cancellationToken = default);
    Task SendNewDeviceSignInAsync(NewDeviceSignInMessage message, CancellationToken cancellationToken = default);
    Task SendOrderUpdateAsync(OrderEmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>Order emails (business §5): <see cref="Stage"/> is Received, Confirmed, Shipped or Invoiced; the link opens the public tracking page (the invoice page for Invoiced).</summary>
public sealed record OrderEmailMessage(string Email, string FullName, string Reference, string Stage, string TrackingUrl,
    string? CourierName, string? WaybillNumber, string? InvoiceNumber = null, string? InvoiceUrl = null)
{
    public const string Received = "Received";
    public const string Confirmed = "Confirmed";
    public const string Shipped = "Shipped";
    public const string Invoiced = "Invoiced";
}

/// <summary>Development/Testing provider. Messages stay in process memory and are never logged or sent.</summary>
public sealed class InMemoryTestEmailProvider : IEmailProvider
{
    private readonly ConcurrentQueue<EmailCodeMessage> _emailCodes = new();
    private readonly ConcurrentQueue<StaffInvitationMessage> _staffInvitations = new();
    private readonly ConcurrentQueue<EmailChangeMessage> _emailChanges = new();
    private readonly ConcurrentQueue<NewDeviceSignInMessage> _newDeviceSignIns = new();
    private readonly ConcurrentQueue<OrderEmailMessage> _orderUpdates = new();

    public IReadOnlyCollection<EmailCodeMessage> EmailCodes => _emailCodes.ToArray();
    public IReadOnlyCollection<StaffInvitationMessage> StaffInvitations => _staffInvitations.ToArray();
    public IReadOnlyCollection<EmailChangeMessage> EmailChanges => _emailChanges.ToArray();
    public IReadOnlyCollection<NewDeviceSignInMessage> NewDeviceSignIns => _newDeviceSignIns.ToArray();
    public IReadOnlyCollection<OrderEmailMessage> OrderUpdates => _orderUpdates.ToArray();

    public Task SendEmailCodeAsync(EmailCodeMessage message, CancellationToken cancellationToken = default) =>
        Capture(_emailCodes, message, cancellationToken);

    public Task SendStaffInvitationAsync(StaffInvitationMessage message, CancellationToken cancellationToken = default) =>
        Capture(_staffInvitations, message, cancellationToken);

    public Task SendEmailChangeVerificationAsync(EmailChangeMessage message, CancellationToken cancellationToken = default) =>
        Capture(_emailChanges, message, cancellationToken);

    public Task SendNewDeviceSignInAsync(NewDeviceSignInMessage message, CancellationToken cancellationToken = default) =>
        Capture(_newDeviceSignIns, message, cancellationToken);

    public Task SendOrderUpdateAsync(OrderEmailMessage message, CancellationToken cancellationToken = default) =>
        Capture(_orderUpdates, message, cancellationToken);

    private static Task Capture<T>(ConcurrentQueue<T> queue, T message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queue.Enqueue(message);
        return Task.CompletedTask;
    }
}

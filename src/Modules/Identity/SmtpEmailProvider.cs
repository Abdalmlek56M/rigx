using System.Net;
using System.Net.Mail;
using System.Text;

namespace HardwareStore.Modules.Identity;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Identity:Email:Smtp";

    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string? FromAddress { get; init; }
    public string FromName { get; init; } = "HARDWARE STORE";
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public int TimeoutSeconds { get; init; } = 15;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException(
                $"{SectionName}:Host is required outside Development and Testing.");
        if (Port is < 1 or > 65535)
            throw new InvalidOperationException($"{SectionName}:Port must be between 1 and 65535.");
        if (!MailAddress.TryCreate(FromAddress, out _))
            throw new InvalidOperationException($"{SectionName}:FromAddress must be a valid email address.");
        if (string.IsNullOrWhiteSpace(FromName))
            throw new InvalidOperationException($"{SectionName}:FromName is required.");
        if (TimeoutSeconds is < 1 or > 120)
            throw new InvalidOperationException($"{SectionName}:TimeoutSeconds must be between 1 and 120.");
        if (string.IsNullOrWhiteSpace(UserName) != string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException(
                $"{SectionName}:UserName and Password must both be configured or both be omitted.");
    }
}

/// <summary>
/// Configuration-only SMTP integration. Provider credentials are injected by the deployment
/// environment and are never stored in source or configuration committed to the repository.
/// </summary>
public sealed class SmtpEmailProvider(SmtpEmailOptions options) : IEmailProvider
{
    public Task SendEmailVerificationAsync(
        EmailVerificationMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            message.Email,
            message.FullName,
            "Confirm your HARDWARE STORE account",
            $"""
            Hello {message.FullName},

            Confirm your email address and choose a password:
            {message.VerificationUrl}

            This link expires in 24 hours and can be used once. If you did not create this account, ignore this message.
            """,
            cancellationToken);

    public Task SendPasswordResetAsync(
        PasswordResetMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            message.Email,
            message.FullName,
            "Reset your HARDWARE STORE password",
            $"""
            Hello {message.FullName},

            Reset your password:
            {message.ResetUrl}

            This link expires in 30 minutes and can be used once. If you did not request a reset, ignore this message.
            """,
            cancellationToken);

    public Task SendEmailCodeAsync(
        EmailCodeMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            message.Email,
            message.FullName,
            message.Purpose == Authentication.EmailCodePurpose.StepUp
                ? "Your RIGX confirmation code"
                : "Your RIGX sign-in code",
            $"""
            Hello {message.FullName},

            Your one-time code is: {message.Code}

            It expires at {message.ExpiresAtUtc:u} and can be used once. Never share it with anyone, including RIGX staff.
            If you did not request it, ignore this message.
            """,
            cancellationToken);

    public Task SendSecurityNotificationAsync(
        IdentitySecurityNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        var action = message.Kind == IdentitySecurityNotificationKind.PasswordResetCompleted
            ? "password reset"
            : "password change";
        return SendAsync(
            message.Email,
            message.FullName,
            "HARDWARE STORE account security notice",
            $"""
            Hello {message.FullName},

            Your HARDWARE STORE {action} completed at {message.OccurredAtUtc:O}.
            All previous sessions were signed out.

            If you did not make this change, request a password reset immediately.
            """,
            cancellationToken);
    }

    private async Task SendAsync(
        string recipient,
        string recipientName,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Timeout = checked(options.TimeoutSeconds * 1000)
        };
        if (!string.IsNullOrWhiteSpace(options.UserName))
            client.Credentials = new NetworkCredential(options.UserName, options.Password);

        using var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress!, options.FromName),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipient, recipientName));
        await client.SendMailAsync(message, cancellationToken);
    }
}

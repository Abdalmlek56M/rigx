using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace HardwareStore.Modules.Identity;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Identity:Email:Smtp";

    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string? FromAddress { get; init; }
    public string FromName { get; init; } = "RIGX";
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
    public Task SendEmailCodeAsync(
        EmailCodeMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(message.Email, message.FullName, EmailLayout.Code(message), cancellationToken);

    public Task SendStaffInvitationAsync(
        StaffInvitationMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(message.Email, message.FullName, EmailLayout.Invitation(message), cancellationToken);

    public Task SendEmailChangeVerificationAsync(
        EmailChangeMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(message.NewEmail, message.FullName, EmailLayout.EmailChange(message), cancellationToken);

    public Task SendNewDeviceSignInAsync(
        NewDeviceSignInMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(message.Email, message.FullName, EmailLayout.NewDevice(message), cancellationToken);

    public Task SendOrderUpdateAsync(OrderEmailMessage message, CancellationToken cancellationToken = default) =>
        SendAsync(message.Email, message.FullName, EmailLayout.Order(message), cancellationToken);

    public Task SendBackInStockAsync(BackInStockEmailMessage message, CancellationToken cancellationToken = default) =>
        SendAsync(message.Email, message.FullName, EmailLayout.BackInStock(message), cancellationToken);

    private async Task SendAsync(
        string recipient,
        string recipientName,
        EmailContent content,
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

        using var message = BuildMessage(options, recipient, recipientName, content);
        await client.SendMailAsync(message, cancellationToken);
    }

    /// <summary>The multipart message: the plain text as the body and the branded layout as the HTML alternate view.</summary>
    internal static MailMessage BuildMessage(
        SmtpEmailOptions options,
        string recipient,
        string recipientName,
        EmailContent content)
    {
        var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress!, options.FromName),
            Subject = content.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = content.Text,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipient, recipientName));
        var html = AlternateView.CreateAlternateViewFromString(content.Html, Encoding.UTF8, "text/html");
        html.TransferEncoding = TransferEncoding.QuotedPrintable;
        message.AlternateViews.Add(html);
        return message;
    }
}

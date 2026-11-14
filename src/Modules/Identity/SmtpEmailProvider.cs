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

    public Task SendStaffInvitationAsync(
        StaffInvitationMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            message.Email,
            message.FullName,
            "You have been added to the RIGX team",
            $"""
            Hello {message.FullName},

            You now have access to the RIGX dashboard ({string.Join(", ", message.Roles)}).
            Sign in here with this email address, using a code sent to your email or your Google account:
            {message.SignInUrl}

            There is no password to set.
            """,
            cancellationToken);

    public Task SendEmailChangeVerificationAsync(
        EmailChangeMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            message.NewEmail,
            message.FullName,
            "Confirm your new RIGX email address",
            $"""
            Hello {message.FullName},

            Confirm that this is your new sign-in email:
            {message.ConfirmUrl}

            This link expires in 30 minutes and can be used once. If you did not ask for this, ignore this message.
            """,
            cancellationToken);

    public Task SendNewDeviceSignInAsync(
        NewDeviceSignInMessage message,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            message.Email,
            message.FullName,
            "New sign-in to your RIGX account",
            $"""
            Hello {message.FullName},

            Your RIGX account was just signed in from a new device: {message.Browser} on {message.Device}, at {message.SignedInAtUtc:yyyy-MM-dd HH:mm} UTC.

            If this was you, there is nothing to do. If not, open your account, sign out of the devices you do not know, and contact us.
            """,
            cancellationToken);

    /// <summary>Customer order emails in Egyptian Arabic (business §2: short, no marketing promises).</summary>
    public Task SendOrderUpdateAsync(OrderEmailMessage message, CancellationToken cancellationToken = default)
    {
        var (subject, line) = message.Stage switch
        {
            OrderEmailMessage.Confirmed => ($"طلبك {message.Reference} اتأكد", "طلبك اتأكد وبنجهزه دلوقتي."),
            OrderEmailMessage.Shipped => ($"طلبك {message.Reference} اتشحن",
                $"طلبك اتشحن مع {message.CourierName ?? "شركة الشحن"}{(message.WaybillNumber is null ? "" : $"، رقم البوليصة {message.WaybillNumber}")}."),
            OrderEmailMessage.Invoiced => ($"فاتورة طلبك {message.InvoiceNumber}", "طلبك اتسلّم. دي فاتورتك، وفيها ضمان كل قطعة:"),
            _ => ($"استلمنا طلبك {message.Reference}", "استلمنا طلبك، وهنكلمك نأكد كل حاجة قبل ما نجهزه.")
        };
        var link = message.Stage == OrderEmailMessage.Invoiced ? message.InvoiceUrl : $"تابع طلبك من هنا: {message.TrackingUrl}";
        return SendAsync(message.Email, message.FullName, subject,
            $"""
            أهلًا {message.FullName}،

            {line}

            {link}

            RIGX
            """,
            cancellationToken);
    }

    /// <summary>Notify-me customers: the item they asked about is available, with the link to its page.</summary>
    public Task SendBackInStockAsync(BackInStockEmailMessage message, CancellationToken cancellationToken = default) =>
        SendAsync(message.Email, message.FullName, "القطعة اللي طلبتها اتوفرت",
            $"""
            أهلًا {message.FullName}،

            القطعة اللي طلبتها اتوفرت: {message.ItemName}.

            شوفها من هنا: {message.ItemUrl}

            RIGX
            """,
            cancellationToken);

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

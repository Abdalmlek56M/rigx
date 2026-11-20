using System.Globalization;
using System.Net;
using HardwareStore.Modules.Identity.Authentication;

namespace HardwareStore.Modules.Identity;

/// <summary>The subject and the two bodies of one email (plain text first, HTML second).</summary>
public sealed record EmailContent(string Subject, string Text, string Html);

/// <summary>
/// One branded HTML layout for every Identity email: a white 560 px card on the store's paper colour, a graphite header
/// with the RIGX wordmark, an orange action colour and a formal Arabic footer. Every value is HTML-encoded.
/// </summary>
public static class EmailLayout
{
    private const string SiteUrl = "https://rigxhardware.com";
    private const string Paragraph = "margin:0 0 14px;font-size:15px;line-height:1.8;color:#121317;";
    private const string Small = "margin:0 0 10px;font-size:13px;line-height:1.7;color:#4A4D53;";
    private const string CodeBox = "display:block;direction:ltr;text-align:center;font-family:'Courier New',monospace;font-size:32px;letter-spacing:8px;font-weight:bold;color:#121317;background:#F4F4F1;border:1px solid #E3E2DD;border-radius:8px;padding:16px 8px;margin:0 0 14px;";
    private const string Button = "display:inline-block;background:#FF6A2B;color:#FFFFFF;padding:12px 22px;border-radius:6px;text-decoration:none;font-weight:bold;font-size:15px;";
    private const string RawLink = "margin:0 0 14px;font-size:12px;line-height:1.7;color:#4A4D53;word-break:break-all;";
    private const string InfoBox = "margin:0 0 14px;padding:14px 16px;background:#F4F4F1;border:1px solid #E3E2DD;border-radius:8px;font-size:14px;line-height:1.8;color:#121317;";

    public static EmailContent Code(EmailCodeMessage message, DateTimeOffset? now = null)
    {
        var code = Line(message.Code);
        var stepUp = message.Purpose == EmailCodePurpose.StepUp;
        var minutes = Math.Max(1, (int)Math.Round(
            (message.ExpiresAtUtc - (now ?? DateTimeOffset.UtcNow)).TotalMinutes, MidpointRounding.AwayFromZero));
        var validity = $"صالح لمدة {Minutes(minutes)}.";
        var label = stepUp ? "كود تأكيد العملية" : "كود الدخول";
        var lead = stepUp ? "استخدم هذا الكود لتأكيد العملية:" : "استخدم هذا الكود لتسجيل الدخول إلى حسابك:";
        var subject = stepUp
            ? $"كود تأكيد العملية في RIGX: {code}"
            : $"كود الدخول إلى RIGX: {code}";
        var html = $"""
            <p style="{Paragraph}">مرحبًا {E(message.FullName)}،</p>
            <p style="{Paragraph}">{lead}</p>
            <div dir="ltr" style="{CodeBox}">{E(code)}</div>
            <p style="{Small}">{validity}</p>
            <p style="{Small}">إذا لم تطلب هذا الكود، تجاهل هذه الرسالة.</p>
            """;
        var text = $"""
            مرحبًا {message.FullName}،

            {label} الخاص بحسابك في RIGX هو: {code}
            {validity}
            إذا لم تطلب هذا الكود، تجاهل هذه الرسالة.

            RIGX
            """;
        return new EmailContent(subject, text, Wrap(subject, html));
    }

    public static EmailContent Invitation(StaffInvitationMessage message)
    {
        const string subject = "دعوة للانضمام إلى فريق RIGX";
        var url = Url(message.SignInUrl);
        var roles = message.Roles.Count == 0 ? "فريق RIGX" : string.Join("، ", message.Roles);
        var html = $"""
            <p style="{Paragraph}">مرحبًا {E(message.FullName)}،</p>
            <p style="{Paragraph}">تمت إضافتك إلى فريق RIGX. الصلاحيات: {E(roles)}.</p>
            <p style="{Paragraph}">سجّل الدخول إلى لوحة الإدارة من الزر التالي:</p>
            {LinkBlock(url, "الدخول إلى لوحة الإدارة")}
            <p style="{Small}">لا توجد كلمة مرور؛ يمكنك الدخول برمز يُرسل إلى بريدك الإلكتروني أو بحساب Google.</p>
            """;
        var text = $"""
            مرحبًا {message.FullName}،

            تمت إضافتك إلى فريق RIGX. الصلاحيات: {roles}.
            سجّل الدخول إلى لوحة الإدارة من هنا:
            {url}

            لا توجد كلمة مرور؛ يمكنك الدخول برمز يُرسل إلى بريدك الإلكتروني أو بحساب Google.

            RIGX
            """;
        return new EmailContent(subject, text, Wrap(subject, html));
    }

    public static EmailContent EmailChange(EmailChangeMessage message)
    {
        const string subject = "تأكيد تغيير البريد الإلكتروني في RIGX";
        var url = Url(message.ConfirmUrl);
        var html = $"""
            <p style="{Paragraph}">مرحبًا {E(message.FullName)}،</p>
            <p style="{Paragraph}">لتأكيد أن هذا بريدك الإلكتروني الجديد، استخدم الزر التالي:</p>
            {LinkBlock(url, "تأكيد البريد الإلكتروني")}
            <p style="{Small}">الرابط صالح لمدة 30 دقيقة ويُستخدم مرة واحدة. إذا لم تطلب هذا التغيير، تجاهل هذه الرسالة.</p>
            """;
        var text = $"""
            مرحبًا {message.FullName}،

            لتأكيد أن هذا بريدك الإلكتروني الجديد، افتح الرابط التالي:
            {url}

            الرابط صالح لمدة 30 دقيقة ويُستخدم مرة واحدة. إذا لم تطلب هذا التغيير، تجاهل هذه الرسالة.

            RIGX
            """;
        return new EmailContent(subject, text, Wrap(subject, html));
    }

    public static EmailContent NewDevice(NewDeviceSignInMessage message)
    {
        const string subject = "تسجيل دخول جديد إلى حسابك في RIGX";
        var time = message.SignedInAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var html = $"""
            <p style="{Paragraph}">مرحبًا {E(message.FullName)}،</p>
            <p style="{Paragraph}">تم تسجيل الدخول إلى حسابك في RIGX من جهاز جديد.</p>
            <div style="{InfoBox}">
              <div>المتصفح: {E(message.Browser)}</div>
              <div>الجهاز: {E(message.Device)}</div>
              <div>الوقت: <span dir="ltr">{E(time)} UTC</span></div>
            </div>
            <p style="{Paragraph}">إذا كان هذا أنت، فلا حاجة إلى أي إجراء. إذا لم يكن أنت، فافتح حسابك، وسجّل الخروج من الأجهزة غير المعروفة، وتواصل معنا.</p>
            """;
        var text = $"""
            مرحبًا {message.FullName}،

            تم تسجيل الدخول إلى حسابك في RIGX من جهاز جديد:
            {message.Browser} على {message.Device}، في {time} UTC.

            إذا كان هذا أنت، فلا حاجة إلى أي إجراء. إذا لم يكن أنت، فافتح حسابك، وسجّل الخروج من الأجهزة غير المعروفة، وتواصل معنا.

            RIGX
            """;
        return new EmailContent(subject, text, Wrap(subject, html));
    }

    public static EmailContent Order(OrderEmailMessage message)
    {
        var reference = Line(message.Reference);
        var invoiced = message.Stage == OrderEmailMessage.Invoiced;
        var url = invoiced ? Url(message.InvoiceUrl) : Url(message.TrackingUrl);
        var invoice = Line(message.InvoiceNumber);
        var subject = message.Stage switch
        {
            OrderEmailMessage.Confirmed => $"تم تأكيد طلبك {reference}",
            OrderEmailMessage.Shipped => $"تم شحن طلبك {reference}",
            OrderEmailMessage.Invoiced => $"فاتورة طلبك {(invoice.Length > 0 ? invoice : reference)}",
            OrderEmailMessage.Received => $"تم استلام طلبك {reference}",
            _ => $"تحديث طلبك {reference}"
        };
        var (lineText, lineHtml) = OrderLine(message, reference);
        var html = $"""
            <p style="{Paragraph}">مرحبًا {E(message.FullName)}،</p>
            <p style="{Paragraph}">{lineHtml}</p>
            {LinkBlock(url, invoiced ? "عرض الفاتورة" : "تتبع الطلب")}
            """;
        var text = $"""
            مرحبًا {message.FullName}،

            {lineText}

            {url}

            RIGX
            """;
        return new EmailContent(subject, text, Wrap(subject, html));
    }

    public static EmailContent BackInStock(BackInStockEmailMessage message)
    {
        const string subject = "توفر القطعة التي طلبتها";
        var url = Url(message.ItemUrl);
        var html = $"""
            <p style="{Paragraph}">مرحبًا {E(message.FullName)}،</p>
            <p style="{Paragraph}">القطعة التي طلبتها متوفرة الآن: <strong>{E(message.ItemName)}</strong>.</p>
            {LinkBlock(url, "عرض القطعة")}
            """;
        var text = $"""
            مرحبًا {message.FullName}،

            القطعة التي طلبتها متوفرة الآن: {message.ItemName}.
            شاهدها من هنا:
            {url}

            RIGX
            """;
        return new EmailContent(subject, text, Wrap(subject, html));
    }

    private static (string Text, string Html) OrderLine(OrderEmailMessage message, string reference)
    {
        if (message.Stage == OrderEmailMessage.Shipped)
        {
            var courier = message.CourierName ?? "شركة الشحن";
            var waybill = message.WaybillNumber;
            var waybillText = waybill is null ? "" : $"، رقم البوليصة {waybill}";
            var waybillHtml = waybill is null ? "" : $"، رقم البوليصة {E(waybill)}";
            return ($"تم شحن طلبك {reference} مع {courier}{waybillText}.",
                $"تم شحن طلبك {E(reference)} مع {E(courier)}{waybillHtml}.");
        }
        if (message.Stage == OrderEmailMessage.Invoiced)
            return ($"تم تسليم طلبك {reference}. هذه فاتورتك، وتتضمن ضمان كل قطعة:",
                $"تم تسليم طلبك {E(reference)}. هذه فاتورتك، وتتضمن ضمان كل قطعة:");
        if (message.Stage == OrderEmailMessage.Confirmed)
            return ($"تم تأكيد طلبك {reference}، ونعمل على تجهيزه الآن.",
                $"تم تأكيد طلبك {E(reference)}، ونعمل على تجهيزه الآن.");
        return ($"استلمنا طلبك {reference}، وسنتواصل معك لتأكيد التفاصيل قبل تجهيزه.",
            $"استلمنا طلبك {E(reference)}، وسنتواصل معك لتأكيد التفاصيل قبل تجهيزه.");
    }

    private static string LinkBlock(string url, string label) =>
        $"""<p style="margin:0 0 14px;"><a href="{E(url)}" style="{Button}">{E(label)}</a></p><p dir="ltr" style="{RawLink}">{E(url)}</p>""";

    private static string Wrap(string subject, string body) => $"""
        <!doctype html>
        <html dir="rtl" lang="ar">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{E(subject)}</title>
        </head>
        <body style="margin:0;padding:0;background:#F4F4F1;font-family:Tahoma,Arial,sans-serif;color:#121317;">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;background:#F4F4F1;">
        <tr><td align="center" style="padding:24px 12px;">
        <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="border-collapse:collapse;width:100%;max-width:560px;background:#FFFFFF;border-radius:8px;overflow:hidden;">
        <tr><td style="background:#121317;padding:18px 24px;color:#FFFFFF;font-size:22px;font-weight:bold;letter-spacing:1px;">RIGX</td></tr>
        <tr><td style="padding:24px;">{body}</td></tr>
        <tr><td style="background:#F4F4F1;padding:16px 24px;border-top:1px solid #E3E2DD;color:#4A4D53;font-size:12px;line-height:1.7;">
        <p style="margin:0 0 4px;">RIGX — قطع كمبيوتر جديدة ومستعملة</p>
        <p style="margin:0 0 4px;"><a href="{SiteUrl}" style="color:#FF6A2B;text-decoration:none;">{SiteUrl}</a></p>
        <p style="margin:0;">هذه رسالة آلية، يرجى عدم الرد عليها.</p>
        </td></tr>
        </table>
        </td></tr>
        </table>
        </body>
        </html>
        """;

    /// <summary>Arabic counted noun: after "لمدة": 1 → دقيقة واحدة, 2 → دقيقتين, 3–10 → دقائق, 11+ → دقيقة.</summary>
    private static string Minutes(int count) => count switch
    {
        1 => "دقيقة واحدة",
        2 => "دقيقتين",
        <= 10 => $"{count.ToString(CultureInfo.InvariantCulture)} دقائق",
        _ => $"{count.ToString(CultureInfo.InvariantCulture)} دقيقة"
    };

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Line(string? value) => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();

    private static string Url(Uri? value) => value is not null && IsSafe(value) ? value.ToString() : SiteUrl;

    private static string Url(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && IsSafe(uri) ? uri.ToString() : SiteUrl;

    private static bool IsSafe(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps ||
        (uri.Scheme == Uri.UriSchemeHttp &&
            (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1"));
}

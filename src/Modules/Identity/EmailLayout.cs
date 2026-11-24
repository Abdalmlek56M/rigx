using System.Globalization;
using System.Net;
using System.Text;
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
    private const string Ar = "dir=\"rtl\" lang=\"ar\"";
    private const string Rtl = "direction:rtl;text-align:right;";
    private const string Paragraph = "direction:rtl;text-align:right;margin:0 0 14px;font-size:15px;line-height:1.8;color:#121317;";
    private const string Small = "direction:rtl;text-align:right;margin:0 0 10px;font-size:13px;line-height:1.7;color:#4A4D53;";
    private const string CodeBox = "display:block;direction:ltr;text-align:center;font-family:'Courier New',monospace;font-size:32px;letter-spacing:8px;font-weight:bold;color:#121317;background:#F4F4F1;border:1px solid #E3E2DD;border-radius:8px;padding:16px 8px;margin:0 0 14px;";
    private const string Button = "display:inline-block;background:#FF6A2B;color:#FFFFFF;padding:12px 22px;border-radius:6px;text-decoration:none;font-weight:bold;font-size:15px;";
    private const string RawLink = "margin:0 0 14px;font-size:12px;line-height:1.7;color:#4A4D53;word-break:break-all;";
    private const string InfoBox = "direction:rtl;text-align:right;margin:0 0 14px;padding:14px 16px;background:#F4F4F1;border:1px solid #E3E2DD;border-radius:8px;font-size:14px;line-height:1.8;color:#121317;";

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
            <p {Ar} style="{Paragraph}">{E(Greeting(message.FullName))}</p>
            <p {Ar} style="{Paragraph}">{lead}</p>
            <div dir="ltr" style="{CodeBox}">{E(code)}</div>
            <p {Ar} style="{Small}">{validity}</p>
            <p {Ar} style="{Small}">إذا لم تطلب هذا الكود، تجاهل هذه الرسالة.</p>
            """;
        var text = $"""
            {Greeting(message.FullName)}

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
            <p {Ar} style="{Paragraph}">{E(Greeting(message.FullName))}</p>
            <p {Ar} style="{Paragraph}">تمت إضافتك إلى فريق RIGX. الصلاحيات: {E(roles)}.</p>
            <p {Ar} style="{Paragraph}">سجّل الدخول إلى لوحة الإدارة من الزر التالي:</p>
            {LinkBlock(url, "الدخول إلى لوحة الإدارة")}
            <p {Ar} style="{Small}">لا توجد كلمة مرور؛ يمكنك الدخول برمز يُرسل إلى بريدك الإلكتروني أو بحساب Google.</p>
            """;
        var text = $"""
            {Greeting(message.FullName)}

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
            <p {Ar} style="{Paragraph}">{E(Greeting(message.FullName))}</p>
            <p {Ar} style="{Paragraph}">لتأكيد أن هذا بريدك الإلكتروني الجديد، استخدم الزر التالي:</p>
            {LinkBlock(url, "تأكيد البريد الإلكتروني")}
            <p {Ar} style="{Small}">الرابط صالح لمدة 30 دقيقة ويُستخدم مرة واحدة. إذا لم تطلب هذا التغيير، تجاهل هذه الرسالة.</p>
            """;
        var text = $"""
            {Greeting(message.FullName)}

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
            <p {Ar} style="{Paragraph}">{E(Greeting(message.FullName))}</p>
            <p {Ar} style="{Paragraph}">تم تسجيل الدخول إلى حسابك في RIGX من جهاز جديد.</p>
            <div {Ar} style="{InfoBox}">
              <div>المتصفح: {E(message.Browser)}</div>
              <div>الجهاز: {E(message.Device)}</div>
              <div>الوقت: <span dir="ltr">{E(time)} UTC</span></div>
            </div>
            <p {Ar} style="{Paragraph}">إذا كان هذا أنت، فلا حاجة إلى أي إجراء. إذا لم يكن أنت، فافتح حسابك، وسجّل الخروج من الأجهزة غير المعروفة، وتواصل معنا.</p>
            """;
        var text = $"""
            {Greeting(message.FullName)}

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
            OrderEmailMessage.Invoiced => $"تم تسليم طلبك {reference} وهذه فاتورتك{(invoice.Length > 0 ? $" {invoice}" : "")}",
            OrderEmailMessage.Received => $"تم استلام طلبك {reference}",
            OrderEmailMessage.Unreachable => $"تعذّر التواصل معك بخصوص طلبك {reference}",
            _ => $"تحديث طلبك {reference}"
        };
        var (lineText, lineHtml) = OrderLine(message, reference);
        var (summaryText, summaryHtml) = OrderSummary(message.Summary);
        var (paymentText, paymentHtml) = PaymentInstructions(message.PaymentInstructions);
        var trackingUrl = Url(message.TrackingUrl);
        // The invoice email links the invoice first and the tracking page second; every other one links tracking only.
        var secondLinkHtml = invoiced ? LinkBlock(trackingUrl, "تتبع الطلب") : "";
        var secondLinkText = invoiced ? $"\nتتبع الطلب:\n{trackingUrl}\n" : "";
        var html = $"""
            <p {Ar} style="{Paragraph}">{E(Greeting(message.FullName))}</p>
            <p {Ar} style="{Paragraph}">{lineHtml}</p>
            {summaryHtml}
            {paymentHtml}
            {LinkBlock(url, invoiced ? "عرض الفاتورة" : "تتبع الطلب")}
            {secondLinkHtml}
            """;
        var text = $"""
            {Greeting(message.FullName)}

            {lineText}
            {summaryText}{paymentText}
            {url}
            {secondLinkText}
            RIGX
            """;
        return new EmailContent(subject, text, Wrap(subject, html, lineText));
    }

    /// <summary>The ordered lines and the totals as a small table (HTML) and as plain lines (text); nothing when the message carries no summary.</summary>
    private static (string Text, string Html) OrderSummary(OrderEmailSummary? summary)
    {
        if (summary is null || summary.Lines.Count == 0) return ("", "");
        const string Cell = "direction:rtl;text-align:right;padding:6px 0;border-bottom:1px solid #E3E2DD;font-size:14px;line-height:1.6;";
        var rows = new StringBuilder();
        var text = new StringBuilder("\nتفاصيل الطلب:\n");
        foreach (var line in summary.Lines)
        {
            var name = Line(line.Name);
            rows.Append($"""<tr><td style="{Cell}">{E(name)} × {line.Quantity.ToString(CultureInfo.InvariantCulture)}</td><td style="{Cell}text-align:left;white-space:nowrap;" dir="ltr">{E(Money(line.LineTotal))}</td></tr>""");
            text.Append($"- {name} × {line.Quantity.ToString(CultureInfo.InvariantCulture)}: {Money(line.LineTotal)}\n");
        }
        void Total(string label, decimal amount, bool strong = false)
        {
            var style = strong ? "font-weight:bold;" : "";
            rows.Append($"""<tr><td style="{Cell}{style}">{E(label)}</td><td style="{Cell}{style}text-align:left;white-space:nowrap;" dir="ltr">{E(Money(amount))}</td></tr>""");
            text.Append($"{label}: {Money(amount)}\n");
        }
        Total("المجموع", summary.Subtotal);
        if (summary.CouponDiscount > 0) Total("خصم القسيمة", -summary.CouponDiscount);
        if (summary.DeliveryFee > 0) Total("رسوم التوصيل", summary.DeliveryFee);
        Total("الإجمالي", summary.Total, strong: true);
        if (summary.Remaining > 0 && summary.Remaining != summary.Total) Total("المتبقي للدفع", summary.Remaining);
        var html = $"""<table {Ar} role="presentation" width="100%" cellpadding="0" cellspacing="0" style="{Rtl}border-collapse:collapse;margin:0 0 16px;">{rows}</table>""";
        return (text.ToString(), html);
    }

    /// <summary>How to pay by transfer: the owner's own plain text, escaped, one paragraph per line; nothing when there is none.</summary>
    private static (string Text, string Html) PaymentInstructions(string? instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions)) return ("", "");
        var lines = instructions.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var html = $"""<p {Ar} style="{Paragraph}"><strong>طريقة الدفع</strong><br>{string.Join("<br>", lines.Select(E))}</p>""";
        return ($"\nطريقة الدفع:\n{string.Join("\n", lines)}\n", html);
    }

    /// <summary>"1,250 ج.م" (no decimals when the amount is whole).</summary>
    private static string Money(decimal amount) =>
        $"{amount.ToString(amount % 1 == 0 ? "#,##0" : "#,##0.00", CultureInfo.InvariantCulture)} ج.م";

    public static EmailContent BackInStock(BackInStockEmailMessage message)
    {
        const string subject = "توفر القطعة التي طلبتها";
        var url = Url(message.ItemUrl);
        var html = $"""
            <p {Ar} style="{Paragraph}">{E(Greeting(message.FullName))}</p>
            <p {Ar} style="{Paragraph}">القطعة التي طلبتها متوفرة الآن: <strong>{E(message.ItemName)}</strong>.</p>
            {LinkBlock(url, "عرض القطعة")}
            """;
        var text = $"""
            {Greeting(message.FullName)}

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
        if (message.Stage == OrderEmailMessage.Unreachable)
            return ($"حاولنا التواصل معك بخصوص طلبك {reference} ولم نتمكن من الوصول إليك. يرجى الرد علينا عبر واتساب أو الاتصال بنا في أقرب وقت لنؤكد الطلب ونجهّزه.",
                $"حاولنا التواصل معك بخصوص طلبك {E(reference)} ولم نتمكن من الوصول إليك. يرجى الرد علينا عبر واتساب أو الاتصال بنا في أقرب وقت لنؤكد الطلب ونجهّزه.");
        if (message.Stage == OrderEmailMessage.Confirmed)
            return ($"تم تأكيد طلبك {reference}، ونعمل على تجهيزه الآن.",
                $"تم تأكيد طلبك {E(reference)}، ونعمل على تجهيزه الآن.");
        return ($"استلمنا طلبك {reference}، وسنتواصل معك لتأكيد التفاصيل قبل تجهيزه.",
            $"استلمنا طلبك {E(reference)}، وسنتواصل معك لتأكيد التفاصيل قبل تجهيزه.");
    }

    private static string LinkBlock(string url, string label) =>
        $"""<p {Ar} style="margin:0 0 14px;text-align:right;"><a href="{E(url)}" style="{Button}">{E(label)}</a></p><p dir="ltr" style="{RawLink}">{E(url)}</p>""";

    /// <summary>The language of an email decides its direction and its greeting, in one place.</summary>
    public enum EmailLanguage { Arabic, English }

    public static string Direction(EmailLanguage language) => language == EmailLanguage.Arabic ? "rtl" : "ltr";

    /// <summary>"مرحباً فلان،" — or a neutral greeting when there is no real saved name (blank, or the sign-in email address used as a stand-in).</summary>
    public static string Greeting(string? name, EmailLanguage language = EmailLanguage.Arabic)
    {
        var known = !string.IsNullOrWhiteSpace(name) && !name.Contains('@');
        var arabic = language == EmailLanguage.Arabic;
        if (!known) return arabic ? "مرحباً بك،" : "Hello,";
        return arabic ? $"مرحباً {Line(name)}،" : $"Hello {Line(name)},";
    }

    /// <summary><paramref name="preheader"/> is the line mail apps show beside the subject; it is hidden in the message itself.</summary>
    private static string Wrap(string subject, string body, string? preheader = null, EmailLanguage language = EmailLanguage.Arabic)
    {
        var dir = Direction(language);
        var align = language == EmailLanguage.Arabic ? "right" : "left";
        var lang = language == EmailLanguage.Arabic ? "ar" : "en";
        var rtl = $"""dir="{dir}" lang="{lang}" """;
        var direction = $"direction:{dir};text-align:{align};";
        return $"""
        <!doctype html>
        <html dir="{dir}" lang="{lang}">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{E(subject)}</title>
        </head>
        <body {rtl}style="margin:0;padding:0;background:#F4F4F1;font-family:Tahoma,Arial,sans-serif;color:#121317;{direction}">
        {Preheader(preheader)}
        <table {rtl}role="presentation" width="100%" cellpadding="0" cellspacing="0" style="{direction}border-collapse:collapse;background:#F4F4F1;">
        <tr><td align="center" style="padding:24px 12px;">
        <table {rtl}role="presentation" width="560" cellpadding="0" cellspacing="0" style="{direction}border-collapse:collapse;width:100%;max-width:560px;background:#FFFFFF;border-radius:8px;overflow:hidden;">
        <tr><td dir="ltr" align="left" style="background:#121317;padding:18px 24px;color:#FFFFFF;font-family:'Courier New',monospace;font-size:22px;font-weight:bold;letter-spacing:3px;text-align:left;">RIGX</td></tr>
        <tr><td style="background:#f97316;height:4px;line-height:4px;font-size:0;padding:0;">&nbsp;</td></tr>
        <tr><td {rtl}style="{direction}padding:24px;">{body}</td></tr>
        <tr><td {rtl}style="{direction}background:#F4F4F1;padding:16px 24px;border-top:1px solid #E3E2DD;color:#4A4D53;font-size:12px;line-height:1.7;">
        <p {rtl}style="{direction}margin:0 0 4px;">RIGX — قطع كمبيوتر جديدة ومستعملة</p>
        <p {rtl}style="{direction}margin:0 0 4px;"><a href="{SiteUrl}" style="color:#FF6A2B;text-decoration:none;">{SiteUrl}</a></p>
        <p {rtl}style="{direction}margin:0;">هذه رسالة آلية، يرجى عدم الرد عليها.</p>
        </td></tr>
        </table>
        </td></tr>
        </table>
        </body>
        </html>
        """;
    }

    /// <summary>Arabic counted noun: after "لمدة": 1 → دقيقة واحدة, 2 → دقيقتين, 3–10 → دقائق, 11+ → دقيقة.</summary>
    private static string Minutes(int count) => count switch
    {
        1 => "دقيقة واحدة",
        2 => "دقيقتين",
        <= 10 => $"{count.ToString(CultureInfo.InvariantCulture)} دقائق",
        _ => $"{count.ToString(CultureInfo.InvariantCulture)} دقيقة"
    };

    private static string Preheader(string? text) => string.IsNullOrWhiteSpace(text)
        ? ""
        : $"""<div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;mso-hide:all;">{E(Line(text))}</div>""";

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

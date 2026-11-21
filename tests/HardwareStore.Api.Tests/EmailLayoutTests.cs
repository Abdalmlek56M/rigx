using System.Net.Mail;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The branded email layout: encoded values, the code box, safe links and a text + HTML part for every message.</summary>
public sealed class EmailLayoutTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-20T10:00:00Z");
    private static readonly SmtpEmailOptions Options = new()
    {
        FromAddress = "no-reply@rigxhardware.com",
        FromName = "RIGX"
    };

    [Fact]
    public void The_code_email_has_the_code_in_rtl_html_and_plain_text()
    {
        var content = EmailLayout.Code(
            new EmailCodeMessage("to@example.com", "سارة", "123456", Now.AddMinutes(10), EmailCodePurpose.SignIn), Now);

        Assert.Contains("123456", content.Subject);
        Assert.Contains("123456", content.Text);
        Assert.Contains("123456", content.Html);
        Assert.Contains("dir=\"rtl\"", content.Html);
        Assert.Contains("dir=\"ltr\"", content.Html);
        Assert.Contains("صالح لمدة 10 دقائق", content.Html);
    }

    [Theory]
    [InlineData(1, "صالح لمدة دقيقة واحدة")]
    [InlineData(15, "صالح لمدة 15 دقيقة")]
    public void The_code_validity_uses_the_right_arabic_count(int minutes, string expected)
    {
        var content = EmailLayout.Code(
            new EmailCodeMessage("to@example.com", "سارة", "123456", Now.AddMinutes(minutes), EmailCodePurpose.StepUp), Now);

        Assert.Contains(expected, content.Text);
    }

    [Fact]
    public void The_layout_encodes_untrusted_values()
    {
        var content = EmailLayout.NewDevice(new NewDeviceSignInMessage(
            "to@example.com", "<script>alert(1)</script>", "لابتوب <b>", "متصفح & جديد", Now));

        Assert.DoesNotContain("<script>", content.Html);
        Assert.Contains("&lt;script&gt;", content.Html);
        Assert.Contains("&amp;", content.Html);
        Assert.Contains("<script>", content.Text);
    }

    [Fact]
    public void Link_emails_use_only_safe_urls()
    {
        var bad = EmailLayout.BackInStock(new BackInStockEmailMessage(
            "to@example.com", "سارة", "قطعة", "javascript:alert(1)"));
        Assert.DoesNotContain("javascript:", bad.Html);
        Assert.Contains("https://rigxhardware.com", bad.Html);

        var good = EmailLayout.Invitation(new StaffInvitationMessage(
            "to@example.com", "سارة", ["Owner"], new Uri("https://rigxhardware.com/admin")));
        Assert.Contains("https://rigxhardware.com/admin", good.Html);
    }

    private static readonly string[] OrderStages =
    [
        OrderEmailMessage.Received, OrderEmailMessage.Confirmed, OrderEmailMessage.Shipped, OrderEmailMessage.Invoiced, OrderEmailMessage.Unreachable
    ];

    private static readonly OrderEmailSummary Summary = new(
        [new OrderEmailLine("كرت شاشة <RTX>", 2, 2500m), new OrderEmailLine("كيبورد", 1, 349.5m)], 2849.5m, 100m, 60m, 2809.5m, 1000m);

    [Fact]
    public void Order_emails_list_the_lines_and_totals_with_the_tracking_link_and_encode_names()
    {
        foreach (var stage in OrderStages)
        {
            var content = EmailLayout.Order(new OrderEmailMessage("to@example.com", "سارة", "RX-1001", stage,
                "https://rigxhardware.com/t/token", null, null, "INV-1", "https://rigxhardware.com/i/token", Summary));

            Assert.Contains("كرت شاشة &lt;RTX&gt; × 2", content.Html);
            Assert.DoesNotContain("<RTX>", content.Html);
            Assert.Contains("2,500 ج.م", content.Html);
            Assert.Contains("349.50 ج.م", content.Text);
            Assert.Contains("خصم القسيمة: -100 ج.م", content.Text);
            Assert.Contains("رسوم التوصيل: 60 ج.م", content.Text);
            Assert.Contains("الإجمالي: 2,809.50 ج.م", content.Text);
            Assert.Contains("المتبقي للدفع: 1,000 ج.م", content.Text);
            Assert.Contains("https://rigxhardware.com/t/token", content.Text);
        }
    }

    [Fact]
    public void The_invoice_email_links_the_invoice_and_the_tracking_page_and_the_unreachable_one_asks_for_a_reply()
    {
        var invoiced = EmailLayout.Order(new OrderEmailMessage("to@example.com", "سارة", "RX-1001", OrderEmailMessage.Invoiced,
            "https://rigxhardware.com/t/token", null, null, "INV-1", "https://rigxhardware.com/i/token", Summary));
        Assert.Contains("https://rigxhardware.com/i/token", invoiced.Text);
        Assert.Contains("https://rigxhardware.com/t/token", invoiced.Text);

        var unreachable = EmailLayout.Order(new OrderEmailMessage("to@example.com", "سارة", "RX-1001", OrderEmailMessage.Unreachable,
            "https://rigxhardware.com/t/token", null, null));
        Assert.Contains("تعذّر التواصل", unreachable.Subject);
        Assert.Contains("نتمكن من الوصول إليك", unreachable.Text);
        Assert.DoesNotContain("تفاصيل الطلب", unreachable.Text);
    }

    [Fact]
    public void Every_message_has_a_plain_text_body_and_an_html_view()
    {
        foreach (var content in Everything())
        {
            using var message = SmtpEmailProvider.BuildMessage(Options, "to@example.com", "سارة", content);
            Assert.False(message.IsBodyHtml, content.Subject);
            Assert.False(string.IsNullOrWhiteSpace(message.Body), content.Subject);
            var view = Assert.Single(message.AlternateViews);
            Assert.Equal("text/html", view.ContentType.MediaType);
            Assert.Equal("utf-8", view.ContentType.CharSet?.ToLowerInvariant());
        }
    }

    private static IEnumerable<EmailContent> Everything()
    {
        yield return EmailLayout.Code(
            new EmailCodeMessage("to@example.com", "سارة", "123456", Now.AddMinutes(10), EmailCodePurpose.SignIn), Now);
        yield return EmailLayout.Invitation(new StaffInvitationMessage(
            "to@example.com", "سارة", ["Owner"], new Uri("https://rigxhardware.com/admin")));
        yield return EmailLayout.EmailChange(new EmailChangeMessage(
            "new@example.com", "سارة", new Uri("https://rigxhardware.com/account/email")));
        yield return EmailLayout.NewDevice(new NewDeviceSignInMessage(
            "to@example.com", "سارة", "لابتوب", "Chrome", Now));

        foreach (var stage in OrderStages)
        {
            yield return EmailLayout.Order(new OrderEmailMessage(
                "to@example.com", "سارة", "RX-1001", stage, "https://rigxhardware.com/t/token",
                "أرامكس", "WB-1", "INV-1", "https://rigxhardware.com/i/token", Summary));
        }

        yield return EmailLayout.BackInStock(new BackInStockEmailMessage(
            "to@example.com", "سارة", "كرت شاشة", "https://rigxhardware.com/p/card"));
    }
}

using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class OrderWhatsAppRulesTests
{
    [Fact]
    public void A_phone_that_is_on_whatsapp_needs_no_other_number()
    {
        Assert.Null(OrderRules.WhatsAppNumber(new OrderCustomer("Mona", "01007771234", true, null, "01112223333")));
        Assert.Null(OrderRules.WhatsAppNumber(null));
    }

    [Fact]
    public void Another_whatsapp_number_is_kept_in_its_local_form()
    {
        Assert.Equal("01112223333", OrderRules.WhatsAppNumber(new OrderCustomer("Mona", "01007771234", false, null, "+20 111 222 3333")));
        Assert.Null(OrderRules.WhatsAppNumber(new OrderCustomer("Mona", "01007771234", false, null, "  ")));
    }

    [Fact]
    public void A_whatsapp_number_that_is_not_a_number_is_refused()
    {
        var error = Assert.Throws<RetailException>(() => OrderRules.WhatsAppNumber(new OrderCustomer("Mona", "01007771234", false, null, "abc")));
        Assert.Equal(400, error.StatusCode);
    }
}

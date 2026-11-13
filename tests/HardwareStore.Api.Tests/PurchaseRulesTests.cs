using HardwareStore.Modules.Purchasing;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class PurchaseRulesTests
{
    [Theory]
    [InlineData("+20 101 234 5678")]
    [InlineData("00201012345678")]
    [InlineData("1012345678")]
    [InlineData("010-1234-5678")]
    [InlineData("01012345678")]
    public void Phone_forms_become_one_mobile_number(string typed) =>
        Assert.Equal("01012345678", PurchaseRules.NormalizePhone(typed));

    [Theory]
    [InlineData("12345")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("0212345678")]
    public void Phone_that_is_not_an_egyptian_mobile_is_rejected(string? typed) =>
        Assert.Equal(400, Assert.Throws<PurchasingException>(() => PurchaseRules.NormalizePhone(typed)).StatusCode);

    [Fact]
    public void Full_pc_needs_more_than_one_part()
    {
        Assert.Equal(400, Assert.Throws<PurchasingException>(() => PurchaseRules.CheckShape(PurchaseKind.FullPc, 1, false)).StatusCode);
        PurchaseRules.CheckShape(PurchaseKind.FullPc, 3, true);
    }

    [Fact]
    public void Only_a_full_pc_can_be_kept_as_a_build() =>
        Assert.Equal(400, Assert.Throws<PurchasingException>(() => PurchaseRules.CheckShape(PurchaseKind.Parts, 2, true)).StatusCode);

    [Theory]
    [InlineData(SellerPaymentMethod.Cash, 400, 0, 600)]
    [InlineData(SellerPaymentMethod.InstaPay, 1000, 0, 0)]
    [InlineData(SellerPaymentMethod.TradeIn, 0, 1000, 0)]
    [InlineData(SellerPaymentMethod.CashAndTradeIn, 300, 500, 200)]
    public void Remaining_is_what_is_left_after_money_and_trade_in(string method, decimal paid, decimal tradeIn, decimal expected) =>
        Assert.Equal(expected, PurchaseRules.Remaining(1000m, new SellerPayment(method, paid, tradeIn, tradeIn > 0 ? "RTX 2060" : null)));

    [Theory]
    [InlineData(SellerPaymentMethod.TradeIn, 100, 900, "RTX 2060")]
    [InlineData(SellerPaymentMethod.TradeIn, 0, 0, null)]
    [InlineData(SellerPaymentMethod.Cash, 500, 500, "RTX 2060")]
    [InlineData(SellerPaymentMethod.CashAndTradeIn, 500, 0, null)]
    [InlineData(SellerPaymentMethod.CashAndTradeIn, 0, 500, "RTX 2060")]
    [InlineData(SellerPaymentMethod.Cash, 1500, 0, null)]
    [InlineData(SellerPaymentMethod.CashAndTradeIn, 600, 600, "RTX 2060")]
    [InlineData(SellerPaymentMethod.TradeIn, 0, 1000, " ")]
    [InlineData("Barter", 0, 0, null)]
    public void Payment_that_does_not_fit_the_method_or_the_total_is_rejected(string method, decimal paid, decimal tradeIn, string? note) =>
        Assert.Equal(400, Assert.Throws<PurchasingException>(() =>
            PurchaseRules.Remaining(1000m, new SellerPayment(method, paid, tradeIn, note))).StatusCode);
}

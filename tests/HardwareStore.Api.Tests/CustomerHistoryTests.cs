using HardwareStore.Modules.Retail;
using Xunit;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>The coupons a customer used (account and admin 360) and the public tracking link's masked phone and 90-day rule (roadmap 4.11 E2).</summary>
[Trait("Category", "RequiresDocker")]
public sealed class CustomerHistoryPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private async Task<(RetailTestServices Retail, MutableTimeProvider Clock)> StartAsync()
    {
        var database = await fixture.CreateDatabaseAsync();
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        return (database.CreateRetailServices(clock), clock);
    }

    [Fact]
    public async Task The_coupons_an_account_used_are_listed_newest_first_with_their_discount_and_order_and_other_accounts_see_none()
    {
        var (retail, clock) = await StartAsync();
        var account = Guid.NewGuid();
        var item = await retail.PublishedItemAsync("History GPU", 6, 1000m);
        await retail.Coupons.CreateAsync(new CouponInput("FIRST10", "Influencer one", CouponDiscountKind.Percent, 10m, null, null, null, false));
        await retail.Coupons.CreateAsync(new CouponInput("SECOND5", "Influencer two", CouponDiscountKind.Amount, 50m, null, null, null, false));
        var first = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with { CouponCode = "FIRST10" }, Policy(account));
        clock.Advance(TimeSpan.FromHours(1));
        var second = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with { CouponCode = "SECOND5" }, Policy(account));
        await retail.CheckoutAsync(Checkout([StockLine(item.Id)]), Policy(account));
        await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01000000009") with { CouponCode = "FIRST10" }, Policy(Guid.NewGuid()));
        await retail.StatusAsync(first, OrderStatus.Cancelled);

        var uses = await retail.Orders.ListMyCouponUsesAsync(account);

        Assert.Equal(["SECOND5", "FIRST10"], uses.Select(use => use.Code));
        Assert.Equal((50m, second.Id, second.Reference), (uses[0].Discount, uses[0].OrderId, uses[0].Reference));
        Assert.Equal((100m, OrderStatus.Cancelled), (uses[1].Discount, uses[1].OrderStatus));
        Assert.Empty(await retail.Orders.ListMyCouponUsesAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task The_tracking_link_masks_the_phone_and_after_ninety_days_from_delivery_shows_only_the_status()
    {
        var (retail, clock) = await StartAsync();
        var item = await retail.PublishedItemAsync("Tracking GPU", 3, 1000m);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], fulfillment: OrderFulfillment.Pickup, phone: "01012345678"));

        var open = await retail.Tracking.GetAsync(order.TrackingToken);
        var delivered = await retail.StatusesAsync(order, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivered);
        var recent = await retail.Tracking.GetAsync(delivered.TrackingToken);
        clock.Advance(TimeSpan.FromDays(89));
        var stillOpen = await retail.Tracking.GetAsync(delivered.TrackingToken);
        clock.Advance(TimeSpan.FromDays(2));
        var old = await retail.Tracking.GetAsync(delivered.TrackingToken);

        Assert.Equal(("010••••••78", false, OrderStatus.New), (open.MaskedPhone, open.DetailsHidden, open.Status));
        Assert.DoesNotContain("01012345678", open.MaskedPhone);
        Assert.Equal((false, 1, OrderStatus.Delivered), (recent.DetailsHidden, recent.Items.Count, recent.Status));
        Assert.False(stillOpen.DetailsHidden);
        Assert.Equal((true, 0, OrderStatus.Delivered, null), (old.DetailsHidden, old.Items.Count, old.Status, old.MaskedPhone));
        Assert.Equal(order.Reference, old.Reference);
        Assert.NotEmpty(old.Timeline);
        Assert.False(old.CanRate);
    }
}

using HardwareStore.Modules.Retail;
using Xunit;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>
/// Free shipping (4.11 A2): off unless the owner turns it on; the server decides it from the prices it worked out (after the coupon, before
/// shipping), never from anything the browser sent; pickup has no shipping to waive.
/// </summary>
[Trait("Category", "RequiresDocker")]
public sealed class FreeShippingPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private async Task<RetailTestServices> StartAsync()
    {
        var database = await fixture.CreateDatabaseAsync();
        return database.CreateRetailServices(new MutableTimeProvider(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task With_the_rule_off_the_fee_is_charged_at_any_total()
    {
        var retail = await StartAsync();
        var item = await retail.PublishedItemAsync("Rule off GPU", 3, 9_000m);

        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id, 2)]));

        Assert.Equal((18_000m, 50m, 18_050m), (order.Subtotal, order.DeliveryFee, order.Total));
    }

    [Fact]
    public async Task At_or_above_the_minimum_the_fee_is_zero_and_below_it_the_normal_fee_is_charged()
    {
        var retail = await StartAsync();
        var item = await retail.PublishedItemAsync("Rule on GPU", 5, 1_000m);

        var below = await retail.CheckoutAsync(Checkout([StockLine(item.Id, 2)]), Policy(freeShippingFrom: 2_500m));
        var exactly = await retail.CheckoutAsync(Checkout([StockLine(item.Id, 3)]), Policy(freeShippingFrom: 3_000m));

        Assert.Equal((2_000m, 50m, 2_050m), (below.Subtotal, below.DeliveryFee, below.Total));
        Assert.Equal((3_000m, 0m, 3_000m), (exactly.Subtotal, exactly.DeliveryFee, exactly.Total));
        // The upfront part follows the fee that was really charged (shipping is paid upfront in the store's rule, here 20 % of the subtotal).
        Assert.Equal(600m, exactly.UpfrontAmount);
    }

    [Fact]
    public async Task The_total_is_taken_after_the_coupon_so_a_coupon_can_bring_an_order_under_the_minimum()
    {
        var retail = await StartAsync();
        var item = await retail.PublishedItemAsync("Coupon rule GPU", 5, 1_000m);
        await retail.Coupons.CreateAsync(new CouponInput("HALF", "Test", CouponDiscountKind.Percent, 50m, null, null, null, true));

        var withCoupon = await retail.CheckoutAsync(Checkout([StockLine(item.Id, 3)], phone: "01000000001") with { CouponCode = "HALF" },
            Policy(freeShippingFrom: 2_000m));
        var withoutCoupon = await retail.CheckoutAsync(Checkout([StockLine(item.Id, 3)], phone: "01000000002"), Policy(freeShippingFrom: 2_000m));

        Assert.Equal((1_500m, 50m), (withCoupon.Subtotal, withCoupon.DeliveryFee));
        Assert.Equal((3_000m, 0m), (withoutCoupon.Subtotal, withoutCoupon.DeliveryFee));
    }

    [Fact]
    public async Task Pickup_has_no_shipping_either_way()
    {
        var retail = await StartAsync();
        var item = await retail.PublishedItemAsync("Pickup rule GPU", 3, 1_000m);

        var pickup = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup), Policy(freeShippingFrom: 500m));

        Assert.Equal((0m, 1_000m), (pickup.DeliveryFee, pickup.Total));
    }
}

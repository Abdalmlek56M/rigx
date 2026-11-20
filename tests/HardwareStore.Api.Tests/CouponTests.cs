using HardwareStore.Modules.Retail;
using Xunit;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>Coupons at checkout and in POS, the per-code report, and the admin rules (roadmap Phase 5, slice K).</summary>
[Trait("Category", "RequiresDocker")]
public sealed class CouponPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private async Task<(RetailTestServices Retail, MutableTimeProvider Clock)> StartAsync()
    {
        var database = await fixture.CreateDatabaseAsync();
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        return (database.CreateRetailServices(clock), clock);
    }

    private static Task<CouponView> CreateAsync(RetailTestServices retail, string code, string kind = CouponDiscountKind.Percent,
        decimal value = 10m, decimal? cap = null, int? maxUses = null, bool once = true) =>
        retail.Coupons.CreateAsync(new CouponInput(code, "Influencer " + code, kind, value, cap, null, maxUses, once));

    private static async Task<CouponView> ReportAsync(RetailTestServices retail, string code, bool money = true) =>
        (await retail.Coupons.ListAsync(money)).Single(coupon => coupon.Code == code);

    private static async Task<string> RejectedAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<RetailException>(action);
        Assert.Equal(400, exception.StatusCode);
        return Assert.Single(exception.Errors!["couponCode"]);
    }

    [Fact]
    public async Task Checkout_coupon_takes_its_discount_off_the_lines_and_shows_in_the_report()
    {
        var (retail, _) = await StartAsync();
        var gpu = await retail.PublishedItemAsync("Coupon GPU", 3, 1000m);
        var ram = await retail.PublishedItemAsync("Coupon RAM", 3, 500m);
        await CreateAsync(retail, "AHMED10", cap: 120m);

        var order = await retail.CheckoutAsync(Checkout([StockLine(gpu.Id), StockLine(ram.Id)]) with { CouponCode = " ahmed10 " });

        Assert.Equal(("AHMED10", 120m), (order.CouponCode, order.CouponDiscount));
        Assert.Equal((1380m, 50m, 1430m), (order.Subtotal, order.DeliveryFee, order.Total));
        Assert.Equal(120m, order.Items.Sum(item => item.DiscountAmount));
        Assert.Equal(80m, order.Items.Single(item => item.StockItemId == gpu.Id).DiscountAmount);
        Assert.Equal(decimal.Round(1380m * 0.2m, 2), order.UpfrontAmount);

        var report = await ReportAsync(retail, "AHMED10");
        Assert.Equal((1, 0, 1380m, 120m), (report.Uses, report.CompletedUses, report.Revenue, report.DiscountGiven));
        Assert.NotNull(report.LastUsedAtUtc);
        var hidden = await ReportAsync(retail, "AHMED10", money: false);
        Assert.Equal((1, (decimal?)null, (decimal?)null), (hidden.Uses, hidden.Revenue, hidden.DiscountGiven));
    }

    [Fact]
    public async Task A_customer_uses_a_code_once_the_limit_counts_all_customers_and_a_cancelled_order_gives_its_use_back()
    {
        var (retail, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Limit GPU", 5, 1000m);
        await CreateAsync(retail, "FB-NOV", CouponDiscountKind.Amount, 100m, maxUses: 2);

        var first = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01000000001") with { CouponCode = "FB-NOV" });
        Assert.Equal(CouponProblem.AlreadyUsed, await RejectedAsync(() =>
            retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "+20 100 000 0001") with { CouponCode = "FB-NOV" })));
        await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01000000002") with { CouponCode = "fb-nov" });
        Assert.Equal(CouponProblem.UsedUp, await RejectedAsync(() =>
            retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01000000003") with { CouponCode = "FB-NOV" })));

        await retail.StatusAsync(first, OrderStatus.Cancelled);
        var again = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01000000001") with { CouponCode = "FB-NOV" });
        Assert.Equal(100m, again.CouponDiscount);
        Assert.Equal(2, (await ReportAsync(retail, "FB-NOV")).Uses);
    }

    [Fact]
    public async Task Unknown_or_stopped_codes_are_rejected_and_an_empty_code_is_no_coupon()
    {
        var (retail, clock) = await StartAsync();
        var item = await retail.PublishedItemAsync("Reject GPU", 3, 1000m);
        var coupon = await CreateAsync(retail, "OFF20", value: 20m);

        Assert.Equal(CouponProblem.NotFound, await RejectedAsync(() =>
            retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with { CouponCode = "NOPE" })));
        Assert.Equal(CouponProblem.NotFound, await RejectedAsync(() =>
            retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with { CouponCode = "!" })));
        var plain = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with { CouponCode = "  " });
        Assert.Equal((null, 0m, 1000m), (plain.CouponCode, plain.CouponDiscount, plain.Subtotal));

        await retail.Coupons.UpdateAsync(coupon.Id, new CouponInput("OFF20", "Ad", CouponDiscountKind.Percent, 20m,
            EndsAtUtc: clock.GetUtcNow().AddHours(1), RowVersion: coupon.RowVersion));
        clock.Advance(TimeSpan.FromHours(2));
        Assert.Equal(CouponProblem.Expired, await RejectedAsync(() =>
            retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with { CouponCode = "OFF20" })));

        var check = await retail.Coupons.CheckAsync(new CouponCheckRequest("off20", 1000m));
        Assert.Equal(("OFF20", 0m, CouponProblem.Expired), (check.Code, check.Discount, check.Problem));
    }

    [Fact]
    public async Task Check_gives_the_discount_without_saving_and_knows_the_customer()
    {
        var (retail, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Check GPU", 3, 1000m);
        await CreateAsync(retail, "TRY15", value: 15m);

        Assert.Equal(new CouponCheckView("TRY15", 150m, null), await retail.Coupons.CheckAsync(new CouponCheckRequest("try15", 1000m)));
        await retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with { CouponCode = "TRY15" });
        Assert.Equal(CouponProblem.AlreadyUsed,
            (await retail.Coupons.CheckAsync(new CouponCheckRequest("TRY15", 1000m, Customer.Phone))).Problem);
        Assert.Null((await retail.Coupons.CheckAsync(new CouponCheckRequest("TRY15", 1000m, "01200000000"))).Problem);
    }

    [Fact]
    public async Task Pos_coupon_comes_after_the_staff_discount_and_is_outside_its_limit()
    {
        var (retail, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Pos Coupon GPU", "New", 3, 500m, 1000m));
        await CreateAsync(retail, "STORE20", value: 20m);
        var sale = Sale([StockLine(item.Id)], discount: 100m) with { CouponCode = "store20" };

        var preview = await retail.Pos.PreviewAsync(sale, PosLimited, true);
        Assert.Equal((100m, 720m, 720m), (preview.Discount, preview.Subtotal, preview.Total));
        Assert.Equal(new CouponCheckView("STORE20", 180m, null), preview.Coupon);
        Assert.Empty(preview.Warnings);

        var order = await retail.PosAsync(sale, PosLimited);
        Assert.Equal(("STORE20", 180m, 720m), (order.CouponCode, order.CouponDiscount, order.Total));
        Assert.Equal(280m, Assert.Single(order.Items).DiscountAmount);

        var again = await retail.Pos.PreviewAsync(sale, PosLimited, false);
        Assert.Equal(CouponProblem.AlreadyUsed, again.Coupon!.Problem);
        Assert.Equal(900m, again.Subtotal);
        Assert.Equal(CouponProblem.AlreadyUsed, await RejectedAsync(() => retail.PosAsync(sale, PosLimited)));
    }

    [Fact]
    public async Task Admin_codes_are_unique_a_used_code_keeps_its_name_and_is_turned_off_not_deleted()
    {
        var (retail, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Admin GPU", 3, 1000m);
        var used = await CreateAsync(retail, "USED5", value: 5m);
        var unused = await CreateAsync(retail, "SPARE5", value: 5m);

        var duplicate = await Assert.ThrowsAsync<RetailException>(() => CreateAsync(retail, "used5"));
        Assert.Equal("code", Assert.Single(duplicate.Errors!).Key);
        await retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with { CouponCode = "USED5" });

        var rename = await Assert.ThrowsAsync<RetailException>(() => retail.Coupons.UpdateAsync(used.Id,
            new CouponInput("USED6", used.SourceName, used.DiscountKind, 5m, RowVersion: used.RowVersion)));
        Assert.Equal("code", Assert.Single(rename.Errors!).Key);
        Assert.Equal(409, await StatusCodeAsync(() => retail.Coupons.UpdateAsync(used.Id,
            new CouponInput("USED5", used.SourceName, used.DiscountKind, 5m, RowVersion: used.RowVersion + 7))));
        Assert.Equal(409, await StatusCodeAsync(() => retail.Coupons.DeleteAsync(used.Id)));

        var off = await retail.Coupons.UpdateAsync(used.Id,
            new CouponInput("USED5", "New source", used.DiscountKind, 5m, IsActive: false, RowVersion: used.RowVersion));
        Assert.Equal((false, false, 1, "New source"), (off.IsActive, off.IsRunning, off.Uses, off.SourceName));

        await retail.Coupons.DeleteAsync(unused.Id);
        Assert.DoesNotContain(await retail.Coupons.ListAsync(true), coupon => coupon.Id == unused.Id);
    }
}

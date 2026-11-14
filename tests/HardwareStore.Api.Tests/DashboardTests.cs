using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The owner's dashboard (roadmap 2.15) over one small shop history: a September sale and two October sales.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class DashboardPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateOnly OctoberStart = new(2026, 10, 1);
    private static readonly DateOnly OctoberEnd = new(2026, 10, 31);

    /// <summary>
    /// 1 Sep: an item that never sells (cost 500) is received, and one item (cost 50) sells for 100 in cash.
    /// 10 Oct: a financed item (partner 10%, cost 100), a used GPU (cost 200) and a new CPU (cost 100) are received and built into one PC.
    /// 15 Oct: the financed item sells for 300 in cash (Facebook, Gaming) and the PC for 1000 on InstaPay (Instagram); a return of the
    /// first one waits for inspection, one order is waiting unpaid, and a rent template has not been posted yet.
    /// </summary>
    private async Task<(RetailTestServices Retail, Guid PartnerId, DashboardView View)> BuildShopAsync()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices(clock);
        var partner = await retail.CreateAsync("Partner", 10m);
        var aged = await retail.ReceiveItemAsync(new ReceiptLine("Old stock", "New", 1, 500m, 900m));
        var september = await retail.ReceiveItemAsync(new ReceiptLine("September item", "New", 1, 50m, 100m));
        await retail.CompletedSaleAsync(RetailMoney.PaidSale(september, 100m));

        clock.Advance(TimeSpan.FromDays(39));
        var financed = await retail.ReceiveItemAsync(new ReceiptLine("Financed GPU", "New", 1, 100m, 300m, PartnerId: partner.Id));
        var parts = await retail.ReceiveUnitsAsync(RetailReceipts.Receipt(Guid.NewGuid(), null,
            [new ReceiptLine("Used GPU", "Used", 1, 200m, 0m), new ReceiptLine("New CPU", "New", 1, 100m, 0m)]), Actor);
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("PC", parts.Select(unit => unit.Id).ToList()));
        var card = await retail.Assemblies.UpdateAssemblyAsync(build.Id,
            new AssemblyUpdateRequest("PC", "عنوان", "Title", 1000m, build.RowVersion, SellMode: AssemblySellMode.Full));
        await retail.Assemblies.SetAssemblyPublishedAsync(card.Id, true, card.RowVersion);

        clock.Advance(TimeSpan.FromDays(5));
        var single = await retail.CompletedSaleAsync(RetailMoney.PaidSale(financed, 300m) with { LeadSource = "Facebook", Usage = "Gaming" });
        await retail.CompletedSaleAsync(RetailSales.Sale([RetailSales.BuildLine(build.Id)], payments: [new PosPaymentRequest("InstaPay", 1000m)])
            with { LeadSource = "Instagram" });
        var line = single.Items.Single();
        await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(single.Id, line.Id, line.Components.Single().UnitId, 300m, "Defect"), Actor);
        await retail.PosAsync(RetailSales.Sale([RetailSales.StockLine(aged.Id, 1, 900m)]));
        await retail.Recurring.CreateAsync(new RecurringExpenseRequest("إيجار", 500m, 5, null, null, true, OctoberStart), Actor, RetailSales.BusinessZone);

        return (retail, partner.Id, await retail.Dashboard.BuildAsync(OctoberStart, OctoberEnd, RetailSales.BusinessZone));
    }

    [Fact]
    public async Task The_kpis_compare_the_period_with_the_previous_one()
    {
        var (_, _, view) = await BuildShopAsync();

        Assert.Equal((new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 30)), (view.PreviousFrom, view.PreviousTo));
        Assert.Equal((1300m, 2, 650m), (view.Current.GrossSales, view.Current.Orders, view.Current.AverageOrder));
        Assert.Equal(880m, view.Current.NetProfit);
        Assert.Equal((100m, 1, 100m, 50m), (view.Previous.GrossSales, view.Previous.Orders, view.Previous.AverageOrder, view.Previous.NetProfit));
        Assert.Equal((1200m, 1660m, 100m, 550m), (view.Changes.GrossSales, view.Changes.NetProfit, view.Changes.Orders, view.Changes.AverageOrder));
    }

    [Fact]
    public async Task The_change_is_empty_when_the_previous_period_had_nothing()
    {
        var (retail, _, _) = await BuildShopAsync();

        var view = await retail.Dashboard.BuildAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), RetailSales.BusinessZone);

        Assert.Equal((0m, 0, 0m), (view.Current.GrossSales, view.Current.Orders, view.Current.AverageOrder));
        Assert.Equal((1300m, 2), (view.Previous.GrossSales, view.Previous.Orders));
        Assert.Equal((-100m, -100m), (view.Changes.GrossSales!.Value, view.Changes.Orders!.Value));
        var first = await retail.Dashboard.BuildAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), RetailSales.BusinessZone);
        Assert.Null(first.Changes.GrossSales);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() =>
            retail.Dashboard.BuildAsync(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 1), RetailSales.BusinessZone)));
    }

    [Fact]
    public async Task The_shop_state_shows_the_money_in_the_accounts_stock_at_cost_and_the_partner_dues()
    {
        var (_, _, view) = await BuildShopAsync();

        Assert.Equal(1400m, view.Snapshot.CashAvailable);
        Assert.Equal([(MoneyAccountKind.Cash, 400m), (MoneyAccountKind.InstaPay, 1000m), (MoneyAccountKind.Wallet, 0m), (MoneyAccountKind.BankTransfer, 0m)],
            view.Snapshot.Accounts.Select(account => (account.Kind, account.Balance)).ToList());
        Assert.Equal(500m, view.Snapshot.StockValueAtCost);
        Assert.Equal(20m, view.Snapshot.PartnerDues);
    }

    [Fact]
    public async Task The_todo_counts_what_waits_for_the_owner()
    {
        var (_, _, view) = await BuildShopAsync();

        Assert.Equal((1, 0, 0, 1, 1), (view.Todo.NewOrders, view.Todo.AwaitingPayment, view.Todo.ExpiredReservations,
            view.Todo.PendingReturns, view.Todo.RecurringExpensesDue));
    }

    [Fact]
    public async Task Sales_are_split_by_lead_source_usage_condition_and_how_they_were_sold()
    {
        var (_, _, view) = await BuildShopAsync();

        Assert.Equal([("Instagram", 1, 1000m), ("Facebook", 1, 300m)], view.LeadSources.Select(row => (row.Label, row.Orders, row.Sales)).ToList());
        Assert.Equal([("Unknown", 1, 1000m), ("Gaming", 1, 300m)], view.Usages.Select(row => (row.Label, row.Orders, row.Sales)).ToList());
        Assert.Equal(["New", "Used"], view.NewVsUsed.Select(row => row.Condition).Order().ToList());
        Assert.Equal((1300m, 900m), (view.NewVsUsed.Sum(row => row.Sales), view.NewVsUsed.Sum(row => row.Margin)));
        Assert.Equal(1300m, Assert.Single(view.TopCategories).Sales);
        Assert.Equal(new DashboardBuildSales(1, 1000m, 0, 0m, 1, 300m), view.BuildSales);
        Assert.Equal(5m, view.AvgDaysToSellUsed);
    }

    [Fact]
    public async Task Stock_aging_lists_the_units_waiting_more_than_thirty_days()
    {
        var (_, _, view) = await BuildShopAsync();

        Assert.Equal((1, 500m), (view.StockAging.Count, view.StockAging.CostTotal));
        var unit = Assert.Single(view.StockAging.Oldest);
        Assert.Equal(("Old stock", 500m, 44), (unit.Name, unit.Cost, unit.DaysInStock));
    }

    [Fact]
    public async Task The_daily_chart_and_this_months_partner_profit_follow_the_sales()
    {
        var (_, partnerId, view) = await BuildShopAsync();

        Assert.Equal(30, view.Days.Count);
        Assert.Equal(OctoberEnd, view.Days[^1].Date);
        var day = view.Days.Single(item => item.GrossSales > 0);
        Assert.Equal((new DateOnly(2026, 10, 15), 1300m, 900m), (day.Date, day.GrossSales, day.NetItemProfit));
        var profit = Assert.Single(view.PartnerProfitsThisMonth);
        Assert.Equal((partnerId, "Partner", 20m), (profit.PartnerId, profit.Name, profit.Earned));
    }
}

using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Partners v2 (roadmap 2.14): cards, capital, financed units, earnings, month close, payouts, statement, POS preview.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class PartnerPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset MidSeptember = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private async Task<(RetailTestServices Retail, OwnerBootstrapDatabase Database, MutableTimeProvider Clock)> StartAsync(DateTimeOffset? now = null)
    {
        var database = await fixture.CreateDatabaseAsync();
        var clock = new MutableTimeProvider(now ?? DateTimeOffset.UtcNow);
        return (database.CreateRetailServices(clock), database, clock);
    }

    private static ReceiptLine Line(string model, decimal cost, decimal price, Guid? partnerId = null, decimal? percent = null) =>
        new(model, "New", 1, cost, price, PartnerId: partnerId, PartnerPercentOverride: percent);

    /// <summary>Sells the items in one store order and moves it to Completed (posts the partner earnings).</summary>
    private static async Task<OrderView> SellAsync(RetailTestServices retail, params (StockItemAdminView Item, decimal Price)[] lines)
    {
        var order = await retail.PosAsync(RetailSales.Sale(lines.Select(line => RetailSales.StockLine(line.Item.Id, 1, line.Price)).ToList()));
        order = await retail.StatusesAsync(order, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivered);
        order = await retail.Payments.UpdatePaymentAsync(order.Id, new PaymentStatusRequest(PaymentStatus.Paid, "cash", order.RowVersion), Actor);
        return await retail.OrderStatus.CompleteOrderAsync(order.Id, new CompleteOrderRequest(order.RowVersion, "done"), Actor);
    }

    private static async Task<long> UnitVersionAsync(OwnerBootstrapDatabase database, Guid unitId)
    {
        await using var db = database.CreateRetailContext();
        return (await db.Units.AsNoTracking().SingleAsync(unit => unit.Id == unitId)).RowVersion;
    }

    private static PartnerRequest Card(string name, decimal percent, string basis = PartnerBasis.FinancedParts, long? rowVersion = null,
        bool lossSharing = false, DateOnly? joinDate = null) =>
        new(name, null, joinDate ?? new DateOnly(2020, 1, 1), percent, basis, lossSharing, true, null, rowVersion);

    [Fact]
    public async Task Partner_card_enforces_unique_name_row_version_and_whole_shop_total()
    {
        var (retail, _, _) = await StartAsync();
        var first = await retail.Partners.CreateAsync(new PartnerRequest(" Mona ", "+20 101 234 5678", null, 15m, PartnerBasis.FinancedParts));
        Assert.Equal(("Mona", "01012345678", 15m, 1L), (first.Name, first.Phone, first.DefaultPercent, first.RowVersion));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Partners.CreateAsync(Card("Mona", 5m))));

        var updated = await retail.Partners.UpdateAsync(first.Id, Card("Mona A", 20m, rowVersion: first.RowVersion));
        Assert.Equal(("Mona A", 20m, 2L), (updated.Name, updated.DefaultPercent, updated.RowVersion));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Partners.UpdateAsync(first.Id, Card("Mona B", 20m, rowVersion: first.RowVersion))));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Partners.UpdateAsync(first.Id, Card("Mona B", 20m))));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Partners.UpdateAsync(Guid.NewGuid(), Card("Ghost", 1m, rowVersion: 1))));
        var second = await retail.Partners.CreateAsync(Card("Omar", 5m));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Partners.UpdateAsync(second.Id, Card("Mona A", 5m, rowVersion: second.RowVersion))));

        var shopA = await retail.Partners.CreateAsync(Card("Shop A", 60m, PartnerBasis.WholeShop));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Partners.CreateAsync(Card("Shop B", 50m, PartnerBasis.WholeShop))));
        var shopB = await retail.Partners.CreateAsync(Card("Shop B", 40m, PartnerBasis.WholeShop));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Partners.UpdateAsync(shopB.Id, Card("Shop B", 41m, PartnerBasis.WholeShop, shopB.RowVersion))));
        var inactive = await retail.Partners.UpdateAsync(shopA.Id, Card("Shop A", 60m, PartnerBasis.WholeShop, shopA.RowVersion) with { IsActive = false });
        Assert.False(inactive.IsActive);
        await retail.Partners.UpdateAsync(shopB.Id, Card("Shop B", 90m, PartnerBasis.WholeShop, shopB.RowVersion));
        Assert.Equal(["Mona A", "Omar", "Shop A", "Shop B"], (await retail.Partners.ListAsync()).Select(partner => partner.Name));
    }

    [Fact]
    public async Task Switching_to_whole_shop_needs_the_financed_units_moved_first()
    {
        var (retail, database, _) = await StartAsync();
        var partner = await retail.CreateAsync("Financer", 10m);
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 300m, partner.Id));
        var unit = item.Units.Single();
        Assert.Equal(1, (await retail.Partners.ListAsync()).Single().UnsoldFinancedUnits);
        var detail = await retail.Partners.GetAsync(partner.Id);
        var part = Assert.Single(detail.FinancedParts);
        Assert.Equal((unit.Id, item.Code, 10m, false), (part.UnitId, part.StockItemCode, part.Percent, part.PercentOverridden));

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Partners.UpdateAsync(partner.Id,
            Card("Financer", 10m, PartnerBasis.WholeShop, partner.RowVersion))));
        await retail.UnitPartners.AssignAsync(unit.Id, new UnitPartnerRequest(null, null, await UnitVersionAsync(database, unit.Id)));
        var switched = await retail.Partners.UpdateAsync(partner.Id, Card("Financer", 10m, PartnerBasis.WholeShop, partner.RowVersion));
        Assert.Equal((PartnerBasis.WholeShop, 0), (switched.Basis, switched.UnsoldFinancedUnits));
    }

    [Fact]
    public async Task Capital_deposits_and_withdrawals_keep_the_balance_and_reject_overdraw()
    {
        var (retail, _, _) = await StartAsync();
        var partner = await retail.CreateAsync("Investor", 10m);
        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Deposit", 1000m, new DateOnly(2026, 10, 1), "Cash", "start"), Actor);
        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Withdrawal", 400m), Actor);
        Assert.Equal(600m, (await retail.Partners.ListAsync()).Single().CapitalBalance);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Withdrawal", 600.01m), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Deposit", 0m), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Gift", 5m), Actor)));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Capital.AddAsync(Guid.NewGuid(), new PartnerCapitalRequest("Deposit", 5m), Actor)));
        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Withdrawal", 600m), Actor);

        var entries = await retail.Capital.ListAsync(partner.Id);
        Assert.Equal(3, entries.Count);
        Assert.Equal(0m, (await retail.Partners.ListAsync()).Single().CapitalBalance);
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Capital.ListAsync(Guid.NewGuid())));
    }

    [Fact]
    public async Task Unit_assignment_checks_partner_percent_stock_state_and_version()
    {
        var (retail, database, _) = await StartAsync();
        var financer = await retail.CreateAsync("Financer", 10m);
        var shop = await retail.CreateAsync("Shop", 10m, basis: PartnerBasis.WholeShop);
        var idle = await retail.Partners.CreateAsync(Card("Idle", 10m) with { IsActive = false });
        var item = await retail.ReceiveItemAsync(Line("CPU", 100m, 300m));
        var unit = item.Units.Single();
        var version = await UnitVersionAsync(database, unit.Id);

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.UnitPartners.AssignAsync(unit.Id, new UnitPartnerRequest(shop.Id, null, version))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.UnitPartners.AssignAsync(unit.Id, new UnitPartnerRequest(idle.Id, null, version))));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.UnitPartners.AssignAsync(unit.Id, new UnitPartnerRequest(Guid.NewGuid(), null, version))));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.UnitPartners.AssignAsync(Guid.NewGuid(), new UnitPartnerRequest(null, null, 1))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.UnitPartners.AssignAsync(unit.Id, new UnitPartnerRequest(financer.Id, 100.5m, version))));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.UnitPartners.AssignAsync(unit.Id, new UnitPartnerRequest(financer.Id, null, version - 1))));

        var assigned = await retail.UnitPartners.AssignAsync(unit.Id, new UnitPartnerRequest(financer.Id, 25m, version));
        Assert.Equal((financer.Id, 25m), (assigned.PartnerId, assigned.PartnerPercentOverride));
        // A percent without a partner is rejected; clearing the partner clears the percent too.
        var next = await UnitVersionAsync(database, unit.Id);
        Assert.Equal(version + 1, next);
        var cleared = await retail.UnitPartners.AssignAsync(unit.Id, new UnitPartnerRequest(null, 30m, next));
        Assert.Equal((null, null), (cleared.PartnerId, cleared.PartnerPercentOverride));

        await SellAsync(retail, (item, 300m));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.UnitPartners.AssignAsync(unit.Id,
            new UnitPartnerRequest(financer.Id, null, UnitVersionAsync(database, unit.Id).Result))));
    }

    [Fact]
    public async Task Only_units_with_a_partner_earn_using_the_unit_percent_or_the_partner_default()
    {
        var (retail, _, _) = await StartAsync();
        var partner = await retail.CreateAsync("Financer", 10m);
        var defaultItem = await retail.ReceiveItemAsync(Line("Default", 100m, 300m, partner.Id));
        var overrideItem = await retail.ReceiveItemAsync(Line("Override", 100m, 300m, partner.Id, 50m));
        var ownItem = await retail.ReceiveItemAsync(Line("Own", 100m, 300m));

        await SellAsync(retail, (defaultItem, 300m), (overrideItem, 300m), (ownItem, 300m));

        var earnings = await retail.PartnerLedger.ListEarningsAsync(partner.Id);
        Assert.Equal(2, earnings.Count);
        Assert.Equal([10m, 50m], earnings.OrderBy(item => item.Percent).Select(item => item.Percent));
        Assert.Equal([20m, 100m], earnings.OrderBy(item => item.Percent).Select(item => item.Amount));
        Assert.Equal(2, (await retail.PartnerLedger.ListEarningsAsync()).Count);
        Assert.All(earnings, item => Assert.Null(item.PeriodMonth));
        var card = (await retail.Partners.ListAsync()).Single();
        Assert.Equal((120m, 0), (card.Due, card.UnsoldFinancedUnits));
    }

    [Fact]
    public async Task Month_close_posts_whole_shop_shares_once_after_the_month_ended()
    {
        var (retail, _, clock) = await StartAsync(MidSeptember);
        await retail.FundAsync(1000m);
        var financer = await retail.CreateAsync("Financer", 10m);
        var shop = await retail.CreateAsync("Shop", 20m, basis: PartnerBasis.WholeShop);
        var late = await retail.CreateAsync("Late Joiner", 10m, basis: PartnerBasis.WholeShop, joinDate: new DateOnly(2026, 10, 1));
        var financed = await retail.ReceiveItemAsync(Line("Financed", 100m, 300m, financer.Id));
        var own = await retail.ReceiveItemAsync(Line("Own", 100m, 200m));
        await SellAsync(retail, (financed, 300m), (own, 200m));
        await retail.Expenses.CreateExpenseAsync(new ExpenseCreateRequest(new DateOnly(2026, 9, 10), 30m, "إيجار", "GeneralOverhead", null, null, null), Actor);

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.MonthClose.CloseMonthAsync(new PartnerMonthCloseRequest(2026, 9), RetailSales.BusinessZone)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.MonthClose.CloseMonthAsync(new PartnerMonthCloseRequest(2026, 13), RetailSales.BusinessZone)));
        clock.Advance(TimeSpan.FromDays(20));

        var closed = await retail.MonthClose.CloseMonthAsync(new PartnerMonthCloseRequest(2026, 9), RetailSales.BusinessZone);
        Assert.Equal((new DateOnly(2026, 9, 1), 270m, 20m, 250m), (closed.Month, closed.StoreNetResult, closed.FinancedShares, closed.Base));
        var share = Assert.Single(closed.Posted);
        Assert.Equal((shop.Id, 50m, 250m, 20m, new DateOnly(2026, 9, 1)), (share.PartnerId, share.Amount, share.GrossProfit, share.Percent, share.PeriodMonth));
        Assert.Null(share.OrderId);

        var again = await retail.MonthClose.CloseMonthAsync(new PartnerMonthCloseRequest(2026, 9), RetailSales.BusinessZone);
        Assert.Empty(again.Posted);
        Assert.Single(await retail.PartnerLedger.ListEarningsAsync(shop.Id));
        Assert.Empty(await retail.PartnerLedger.ListEarningsAsync(late.Id));
        Assert.Equal(50m, (await retail.Partners.ListAsync()).Single(partner => partner.Id == shop.Id).Due);

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.MonthClose.CloseMonthAsync(new PartnerMonthCloseRequest(2026, 10), RetailSales.BusinessZone)));
    }

    [Fact]
    public async Task A_loss_month_posts_zero_without_loss_sharing_and_a_recoverable_balance_with_it()
    {
        var (retail, _, clock) = await StartAsync(MidSeptember);
        var plain = await retail.CreateAsync("Plain", 20m, basis: PartnerBasis.WholeShop);
        var sharing = await retail.CreateAsync("Sharing", 30m, lossSharing: true, basis: PartnerBasis.WholeShop);
        var item = await retail.ReceiveItemAsync(Line("Loss", 200m, 150m));
        await SellAsync(retail, (item, 150m));
        clock.Advance(TimeSpan.FromDays(20));

        var closed = await retail.MonthClose.CloseMonthAsync(new PartnerMonthCloseRequest(2026, 9), RetailSales.BusinessZone);

        Assert.Equal((-50m, 0m, -50m), (closed.StoreNetResult, closed.FinancedShares, closed.Base));
        Assert.Equal(0m, closed.Posted.Single(entry => entry.PartnerId == plain.Id).Amount);
        var loss = closed.Posted.Single(entry => entry.PartnerId == sharing.Id);
        Assert.Equal((-15m, EarningStatus.Reversed), (loss.Amount, loss.Status));
        Assert.Empty(await retail.PartnerLedger.ListAdjustmentsAsync(plain.Id));
        var adjustment = Assert.Single(await retail.PartnerLedger.ListAdjustmentsAsync(sharing.Id));
        Assert.Equal(-15m, adjustment.Amount);
        Assert.Equal(15m, (await retail.Partners.ListAsync()).Single(partner => partner.Id == sharing.Id).Recoverable);
    }

    [Fact]
    public async Task Payouts_reduce_the_due_and_cannot_exceed_it()
    {
        var (retail, _, _) = await StartAsync();
        await retail.FundAsync(1000m);
        var first = await retail.CreateAsync("First", 10m);
        var second = await retail.CreateAsync("Second", 10m);
        var one = await retail.ReceiveItemAsync(Line("One", 100m, 300m, first.Id));
        var two = await retail.ReceiveItemAsync(Line("Two", 100m, 400m, first.Id));
        await SellAsync(retail, (one, 300m));
        await SellAsync(retail, (two, 400m));

        Assert.Equal(50m, (await retail.Partners.ListAsync()).Single(partner => partner.Id == first.Id).Due);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PartnerLedger.PayoutAsync(first.Id, new PartnerPayoutRequest(50.01m, null, null, null), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PartnerLedger.PayoutAsync(first.Id, new PartnerPayoutRequest(0m, null, null, null), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PartnerLedger.PayoutAsync(second.Id, new PartnerPayoutRequest(1m, null, null, null), Actor)));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.PartnerLedger.PayoutAsync(Guid.NewGuid(), new PartnerPayoutRequest(1m, null, null, null), Actor)));

        var payout = await retail.PartnerLedger.PayoutAsync(first.Id, new PartnerPayoutRequest(25m, null, "REF", "part"), Actor);
        Assert.Equal(("First", "Cash", 25m), (payout.PartnerName, payout.Method, payout.Amount));
        Assert.Equal(25m, (await retail.Partners.ListAsync()).Single(partner => partner.Id == first.Id).Due);
        // Oldest earning first: the 20 earning is paid, 5 of the 30 earning.
        var earnings = (await retail.PartnerLedger.ListEarningsAsync(first.Id)).OrderBy(item => item.CreatedAtUtc).ToList();
        Assert.Equal([EarningStatus.Paid, EarningStatus.Outstanding], earnings.Select(item => item.Status));
        Assert.Equal([20m, 5m], earnings.Select(item => item.PaidAmount));
        Assert.Single(await retail.PartnerLedger.ListPayoutsAsync(first.Id));
        Assert.Empty(await retail.PartnerLedger.ListPayoutsAsync(second.Id));
    }

    [Fact]
    public async Task Statement_lists_capital_earnings_payouts_reversals_and_recoveries_for_the_period()
    {
        var (retail, _, clock) = await StartAsync(new DateTimeOffset(2026, 11, 10, 10, 0, 0, TimeSpan.Zero));
        var partner = await retail.CreateAsync("Statement", 10m);
        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Deposit", 1000m, new DateOnly(2026, 10, 20)), Actor);
        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Deposit", 200m, new DateOnly(2026, 11, 5)), Actor);
        await retail.Capital.AddAsync(partner.Id, new PartnerCapitalRequest("Withdrawal", 100m, new DateOnly(2026, 11, 6)), Actor);
        var item = await retail.ReceiveItemAsync(Line("Sold", 100m, 300m, partner.Id));
        var order = await SellAsync(retail, (item, 300m));
        await retail.PartnerLedger.PayoutAsync(partner.Id, new PartnerPayoutRequest(5m, "Cash", "R1", null), Actor);
        clock.Advance(TimeSpan.FromHours(1));
        var sold = order.Items.Single();
        var entity = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(order.Id, sold.Id, sold.Components.Single().UnitId, 300m, "Defect"), Actor);
        await retail.Returns.InspectReturnAsync(entity.Id, new ReturnInspectRequest("New", "Resellable", entity.RowVersion), Actor);

        var statement = await retail.Statements.GetAsync(partner.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        Assert.Equal((1000m, 1100m, 20m, 5m, 5m), (statement.CapitalOpening, statement.CapitalClosing, statement.Earned, statement.Paid, statement.Recovered));
        Assert.Equal(["CapitalDeposit", "CapitalWithdrawal", "Earning", "Payout", "Reversal", "Recovery"],
            statement.Lines.Select(line => line.Kind).ToList());
        Assert.Equal([200m, -100m, 20m, -5m, -20m, -5m], statement.Lines.Select(line => line.Amount).ToList());
        Assert.Equal(statement.Lines.OrderBy(line => line.At).Select(line => line.Kind), statement.Lines.Select(line => line.Kind));
        var october = await retail.Statements.GetAsync(partner.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        Assert.Equal((0m, 1000m, 0m), (october.CapitalOpening, october.CapitalClosing, october.Earned));
        Assert.Equal(["CapitalDeposit"], october.Lines.Select(line => line.Kind).ToList());

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Statements.GetAsync(partner.Id, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 1))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Statements.GetAsync(partner.Id, new DateOnly(2025, 11, 1), new DateOnly(2026, 11, 2))));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Statements.GetAsync(Guid.NewGuid(), new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2))));
    }

    [Fact]
    public async Task Pos_preview_shows_the_estimated_partner_shares_only_with_the_cost_permission()
    {
        var (retail, _, _) = await StartAsync();
        var first = await retail.CreateAsync("First", 10m);
        var second = await retail.CreateAsync("Second", 10m);
        var defaultItem = await retail.ReceiveItemAsync(Line("Default", 100m, 300m, first.Id));
        var overrideItem = await retail.ReceiveItemAsync(Line("Override", 100m, 300m, second.Id, 50m));
        var ownItem = await retail.ReceiveItemAsync(Line("Own", 100m, 300m));
        var sale = RetailSales.Sale([RetailSales.StockLine(defaultItem.Id, 1, 300m), RetailSales.StockLine(overrideItem.Id, 1, 200m),
            RetailSales.StockLine(ownItem.Id, 1, 300m)]);

        var preview = await retail.Pos.PreviewAsync(sale, RetailSales.PosUnlimited, true);

        Assert.NotNull(preview.PartnerShares);
        Assert.Equal([("First", 20m), ("Second", 50m)], preview.PartnerShares.Select(share => (share.PartnerName, share.Amount)).ToList());
        Assert.Null((await retail.Pos.PreviewAsync(sale, RetailSales.PosUnlimited, false)).PartnerShares);
        var plain = await retail.Pos.PreviewAsync(RetailSales.Sale([RetailSales.StockLine(ownItem.Id, 1, 300m)]), RetailSales.PosUnlimited, true);
        Assert.Empty(plain.PartnerShares!);
    }

    private sealed class PurchaseHarness(OwnerBootstrapDatabase database)
    {
        public SellerService Sellers { get; } = new(Factory<PurchasingDbContext>(database), TimeProvider.System);
        public PurchaseService Purchases { get; } = new(Factory<PurchasingDbContext>(database), TimeProvider.System, RetailSales.BusinessZone);
        public RetailTestServices Retail { get; } = database.CreateRetailServices();
        public PurchaseReceiptService Receipts => new PurchaseReceiptTesting(database, Purchases, Retail).Create();

        private static Factory<T> Factory<T>(OwnerBootstrapDatabase db) where T : DbContext => new(db.ConnectionString);
    }

    private sealed class Factory<T>(string connectionString) : IDbContextFactory<T> where T : DbContext
    {
        public T CreateDbContext() =>
            (T)Activator.CreateInstance(typeof(T), new DbContextOptionsBuilder<T>().UseNpgsql(connectionString).Options)!;
    }

    [Fact]
    public async Task A_purchase_line_partner_reaches_the_received_unit_and_a_whole_shop_partner_is_refused()
    {
        var harness = new PurchaseHarness(await fixture.CreateDatabaseAsync());
        var financer = await harness.Retail.CreateAsync("Financer", 10m);
        var shop = await harness.Retail.CreateAsync("Shop", 10m, basis: PartnerBasis.WholeShop);
        var seller = await harness.Sellers.CreateAsync(new SellerRequest("Person", "Ahmed", "01012345678", null, null, null, null, null));
        PurchaseRequest Draft(Guid partnerId) => new(seller.Id, PurchaseKind.Parts, false, null, new DateOnly(2026, 11, 10), null,
            SellerPaymentMethod.Cash, 0, 0, null,
            [new PurchaseLineRequest("RTX 3060", "Used", 1, 2000m, 3000m, PartnerPercent: 25m, PartnerId: partnerId)]);

        var financed = await harness.Purchases.CreateAsync(Draft(financer.Id), Actor);
        Assert.Equal(financer.Id, financed.Lines.Single().PartnerId);
        await harness.Receipts.ReceiveAsync(financed.Id, Actor, default);
        var unit = Assert.Single(await harness.Retail.Units.ListUnitsAsync(null, null));
        Assert.Equal((financer.Id, 25m), (unit.PartnerId, unit.PartnerPercentOverride));

        var refused = await harness.Purchases.CreateAsync(Draft(shop.Id), Actor);
        Assert.Equal(400, (await Assert.ThrowsAsync<RetailException>(() => harness.Receipts.ReceiveAsync(refused.Id, Actor, default))).StatusCode);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => harness.Retail.Partners.EnsureCanFinanceAsync(shop.Id)));
    }
}

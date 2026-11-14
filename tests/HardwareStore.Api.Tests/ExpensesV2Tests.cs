using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Expenses v2 (roadmap 2.15): expense types, how an expense is paid, recurring monthly expenses, summary and CSV export.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class ExpensesV2PostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();
    private const string Rent = "إيجار";
    private const string Internet = "إنترنت";
    private static readonly DateTimeOffset MidOctober = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private async Task<(RetailTestServices Retail, MutableTimeProvider Clock)> StartAsync(DateTimeOffset? now = null)
    {
        var clock = new MutableTimeProvider(now ?? MidOctober);
        return ((await fixture.CreateDatabaseAsync()).CreateRetailServices(clock), clock);
    }

    private static ExpenseCreateRequest Overhead(decimal amount, string category = Rent, DateOnly? date = null, string? note = null,
        Guid? accountId = null, Guid? partnerId = null) =>
        new(date ?? new DateOnly(2026, 10, 15), amount, category, ExpenseClassification.GeneralOverhead, note, null, null, null,
            MoneyAccountId: accountId, PaidByPartnerId: partnerId);

    private static ReceiptLine Line(string model, decimal cost, decimal price, Guid? partnerId = null) =>
        new(model, "New", 1, cost, price, PartnerId: partnerId);

    [Fact]
    public async Task The_category_must_be_an_active_expense_type()
    {
        var (retail, _) = await StartAsync();
        await retail.FundAsync(1000m);
        var inactive = await retail.ExpenseTypes.CreateAsync(new ExpenseTypeRequest("قيد التجربة", false));

        Assert.Equal(10, (await retail.ExpenseTypes.ListAsync()).Count);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Overhead(10m, "غير موجود"), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Overhead(10m, inactive.Name), Actor)));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.ExpenseTypes.CreateAsync(new ExpenseTypeRequest(Rent))));

        await retail.ExpenseTypes.UpdateAsync(inactive.Id, new ExpenseTypeRequest(inactive.Name, true, 0, inactive.RowVersion));

        Assert.Equal(inactive.Name, (await retail.Expenses.CreateExpenseAsync(Overhead(10m, inactive.Name), Actor)).Category);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() =>
            retail.ExpenseTypes.UpdateAsync(inactive.Id, new ExpenseTypeRequest(inactive.Name, false, 0, inactive.RowVersion))));
    }

    [Fact]
    public async Task Classification_is_validated_and_a_direct_expense_needs_a_link_that_exists()
    {
        var (retail, _) = await StartAsync();
        await retail.FundAsync(1000m);
        var units = await retail.ReceiveUnitsAsync(RetailReceipts.Receipt(Guid.NewGuid(), null, [new ReceiptLine("GPU", "New", 1, 100m, 300m)]), Actor);
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("PC", [units.Single().Id]));
        ExpenseCreateRequest Direct(Guid? assemblyId = null) =>
            new(new DateOnly(2026, 10, 15), 40m, "تغليف", ExpenseClassification.Direct, null, null, null, null, assemblyId);

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Direct(), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Overhead(10m) with { Classification = "Weird" }, Actor)));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Direct(Guid.NewGuid()), Actor)));

        var saved = await retail.Expenses.CreateExpenseAsync(Direct(build.Id), Actor);
        var defaulted = await retail.Expenses.CreateExpenseAsync(Overhead(10m) with { Classification = null }, Actor);

        Assert.Equal((build.Id, ExpenseClassification.Direct), (saved.AssemblyId, saved.Classification));
        Assert.Equal(ExpenseClassification.GeneralOverhead, defaulted.Classification);
        Assert.Equal(build.Id, (await retail.Expenses.ListExpensesAsync(null, null)).Single(item => item.Id == saved.Id).AssemblyId);
    }

    [Fact]
    public async Task An_expense_is_paid_from_the_chosen_account_or_the_cash_account_and_never_below_zero()
    {
        var (retail, _) = await StartAsync();
        await retail.FundAsync(100m, MoneyAccountKind.Wallet);
        await retail.FundAsync(30m);
        var wallet = await retail.AccountAsync(MoneyAccountKind.Wallet);

        var fromWallet = await retail.Expenses.CreateExpenseAsync(Overhead(40m, accountId: wallet.Id), Actor);
        var fromCash = await retail.Expenses.CreateExpenseAsync(Overhead(10m), Actor);

        Assert.Equal(wallet.Id, fromWallet.MoneyAccountId);
        Assert.Equal((await retail.AccountAsync()).Id, fromCash.MoneyAccountId);
        Assert.Equal((60m, 20m), (await retail.BalanceAsync(MoneyAccountKind.Wallet), await retail.BalanceAsync()));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Overhead(60.01m, accountId: wallet.Id), Actor)));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Overhead(21m), Actor)));
        Assert.Equal(2, (await retail.Expenses.ListExpensesAsync(null, null)).Count);
        Assert.Equal((60m, 20m), (await retail.BalanceAsync(MoneyAccountKind.Wallet), await retail.BalanceAsync()));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Overhead(1m, accountId: Guid.NewGuid()), Actor)));
    }

    [Fact]
    public async Task An_expense_paid_by_a_partner_comes_out_of_his_dues_and_moves_no_account()
    {
        var (retail, _) = await StartAsync();
        var partner = await retail.CreateAsync("Partner", 10m);
        var item = await retail.ReceiveItemAsync(Line("GPU", 100m, 300m, partner.Id));
        await retail.CompletedSaleAsync(RetailMoney.PaidSale(item, 300m));
        var cash = await retail.BalanceAsync();
        var cashAccountId = (await retail.AccountAsync()).Id;

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(Overhead(20.01m, partnerId: partner.Id), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Expenses.CreateExpenseAsync(
            Overhead(5m, accountId: cashAccountId, partnerId: partner.Id), Actor)));
        var expense = await retail.Expenses.CreateExpenseAsync(Overhead(15m, partnerId: partner.Id), Actor);

        Assert.Equal((partner.Id, null), (expense.PaidByPartnerId, expense.MoneyAccountId));
        Assert.Equal(5m, (await retail.Partners.ListAsync()).Single().Due);
        Assert.Equal(cash, await retail.BalanceAsync());
        var settlement = Assert.Single(await retail.PartnerLedger.ListPayoutsAsync(partner.Id));
        Assert.Equal((PartnerSettlementMethod.PartnerExpense, 15m, expense.Id), (settlement.Method, settlement.Amount, settlement.ExpenseId));
        Assert.Single(await retail.Expenses.ListExpensesAsync(null, null));
    }

    private static Task<RecurringExpenseView> Template(RetailTestServices retail, decimal amount = 1000m, int day = 5, bool active = true) =>
        retail.Recurring.CreateAsync(new RecurringExpenseRequest(Rent, amount, day, null, "إيجار المحل", active, new DateOnly(2026, 8, 17)),
            Actor, RetailSales.BusinessZone);

    [Fact]
    public async Task Recurring_expenses_post_every_missed_month_once()
    {
        var (retail, _) = await StartAsync();
        await retail.FundAsync(5000m);
        var template = await Template(retail);

        Assert.Equal(new DateOnly(2026, 8, 1), template.StartMonth);
        Assert.Equal(1, await retail.Recurring.CountDueAsync(RetailSales.BusinessZone));
        var first = await retail.Recurring.PostDueAsync(RetailSales.BusinessZone);
        var again = await retail.Recurring.PostDueAsync(RetailSales.BusinessZone);

        Assert.Equal((3, 0, 0, 0), (first.Posted, first.Blocked, again.Posted, again.Blocked));
        Assert.Equal(0, await retail.Recurring.CountDueAsync(RetailSales.BusinessZone));
        var posted = (await retail.Expenses.ListExpensesAsync(null, null)).OrderBy(item => item.ExpenseDate).ToList();
        Assert.Equal([new DateOnly(2026, 8, 5), new DateOnly(2026, 9, 5), new DateOnly(2026, 10, 5)], posted.Select(item => item.ExpenseDate).ToList());
        Assert.All(posted, item => Assert.Equal((template.Id, Rent, ExpenseClassification.GeneralOverhead, 1000m),
            (item.RecurringExpenseId, item.Category, item.Classification, item.Amount)));
        Assert.Equal(2000m, await retail.BalanceAsync());
        Assert.Equal(new DateOnly(2026, 10, 1), Assert.Single(await retail.Recurring.ListAsync()).LastPostedMonth);
    }

    [Fact]
    public async Task A_template_that_cannot_pay_stays_due_and_posts_once_the_account_is_funded()
    {
        var (retail, clock) = await StartAsync();
        await retail.FundAsync(1000m);
        await Template(retail);

        var blocked = await retail.Recurring.PostDueAsync(RetailSales.BusinessZone);

        Assert.Equal((1, 1), (blocked.Posted, blocked.Blocked));
        Assert.Equal(1, await retail.Recurring.CountDueAsync(RetailSales.BusinessZone));
        Assert.Equal(new DateOnly(2026, 8, 1), Assert.Single(await retail.Recurring.ListAsync()).LastPostedMonth);
        var retry = await retail.Recurring.PostDueAsync(RetailSales.BusinessZone);
        Assert.Equal((0, 1), (retry.Posted, retry.Blocked));

        await retail.FundAsync(2000m);
        var done = await retail.Recurring.PostDueAsync(RetailSales.BusinessZone);

        Assert.Equal((2, 0), (done.Posted, done.Blocked));
        Assert.Equal(0, await retail.Recurring.CountDueAsync(RetailSales.BusinessZone));
        Assert.Equal(3, (await retail.Expenses.ListExpensesAsync(null, null)).Count);

        clock.Advance(TimeSpan.FromDays(25));
        Assert.Equal(1, await retail.Recurring.CountDueAsync(RetailSales.BusinessZone));
    }

    [Fact]
    public async Task Templates_validate_their_input_and_an_inactive_one_is_never_posted()
    {
        var (retail, _) = await StartAsync();
        await retail.FundAsync(5000m);
        var template = await Template(retail, active: false);

        var none = await retail.Recurring.PostDueAsync(RetailSales.BusinessZone);
        Assert.Equal((0, 0), (none.Posted, none.Blocked));
        Assert.Equal(0, await retail.Recurring.CountDueAsync(RetailSales.BusinessZone));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => Template(retail, day: 29)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => Template(retail, amount: 0m)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Recurring.CreateAsync(
            new RecurringExpenseRequest("غير موجود", 10m, 5), Actor, RetailSales.BusinessZone)));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Recurring.CreateAsync(
            new RecurringExpenseRequest(Rent, 10m, 5, Guid.NewGuid()), Actor, RetailSales.BusinessZone)));

        var updated = await retail.Recurring.UpdateAsync(template.Id,
            new RecurringExpenseRequest(Rent, 1000m, 5, null, null, true, null, template.RowVersion));

        Assert.Equal(template.RowVersion + 1, updated.RowVersion);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Recurring.UpdateAsync(template.Id,
            new RecurringExpenseRequest(Rent, 1m, 5, null, null, true, null, template.RowVersion))));
        Assert.Equal(1, await retail.Recurring.CountDueAsync(RetailSales.BusinessZone));
    }

    [Fact]
    public async Task The_summary_totals_by_type_against_the_previous_period_and_the_share_of_sales()
    {
        var (retail, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(Line("GPU", 400m, 1000m));
        await retail.CompletedSaleAsync(RetailMoney.PaidSale(item, 1000m));
        await Template(retail, 700m);
        foreach (var expense in new[] { Overhead(100m, date: new DateOnly(2026, 10, 5)), Overhead(50m, Internet, new DateOnly(2026, 10, 10)),
                     Overhead(50m, date: new DateOnly(2026, 10, 12)), Overhead(100m, date: new DateOnly(2026, 9, 15)) })
            await retail.Expenses.CreateExpenseAsync(expense, Actor);

        var summary = await retail.Expenses.ExpenseSummaryAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Equal((200m, 100m, 100m), (summary.Total, summary.PreviousTotal, summary.ChangePercent));
        Assert.Equal([(Rent, 150m, 2), (Internet, 50m, 1)], summary.ByType.Select(row => (row.Type, row.Amount, row.Count)).ToList());
        Assert.Equal((1000m, 20m, 700m), (summary.GrossSales, summary.PercentOfSales, summary.FixedMonthly));
        var empty = await retail.Expenses.ExpenseSummaryAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        Assert.Equal((0m, 0m, null, null), (empty.Total, empty.GrossSales, empty.ChangePercent, empty.PercentOfSales));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Expenses.ExpenseSummaryAsync(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1))));
    }

    [Fact]
    public async Task The_csv_export_has_a_header_an_arabic_row_and_guards_against_spreadsheet_formulas()
    {
        var (retail, _) = await StartAsync();
        await retail.FundAsync(1000m);
        await retail.Expenses.CreateExpenseAsync(Overhead(100m, date: new DateOnly(2026, 10, 5), note: "إيجار, شهر أكتوبر"), Actor);
        await retail.Expenses.CreateExpenseAsync(Overhead(25.5m, Internet, new DateOnly(2026, 10, 6), "=HYPERLINK(\"x\")"), Actor);
        await retail.Expenses.CreateExpenseAsync(Overhead(5m, date: new DateOnly(2026, 11, 6)), Actor);

        var lines = (await retail.Expenses.ExportCsvAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)))
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("date,type,classification,amount,paid from,order reference,unit code,build code,note", lines[0]);
        Assert.Equal("2026-10-05,إيجار,GeneralOverhead,100.00,الخزنة,,,,\"إيجار, شهر أكتوبر\"", lines[1]);
        Assert.Equal("2026-10-06,إنترنت,GeneralOverhead,25.50,الخزنة,,,,\"'=HYPERLINK(\"\"x\"\")\"", lines[2]);
        Assert.Equal(3, lines.Length);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Expenses.ExportCsvAsync(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1))));
    }
}

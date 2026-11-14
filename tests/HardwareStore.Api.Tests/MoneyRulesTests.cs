using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Money account rules and expense rules (roadmap 2.15): pure functions, no database.</summary>
public sealed class MoneyRulesTests
{
    [Theory]
    [InlineData(null, MoneyAccountKind.Cash)]
    [InlineData("", MoneyAccountKind.Cash)]
    [InlineData("Cash", MoneyAccountKind.Cash)]
    [InlineData("CashOnDelivery", MoneyAccountKind.Cash)]
    [InlineData(" InstaPay ", MoneyAccountKind.InstaPay)]
    [InlineData("Wallet", MoneyAccountKind.Wallet)]
    [InlineData("BankTransfer", MoneyAccountKind.BankTransfer)]
    public void A_payment_method_lands_in_the_account_kind_of_its_name(string? method, string kind) =>
        Assert.Equal(kind, MoneyRules.KindForMethod(method));

    [Theory]
    [InlineData("Visa")]
    [InlineData("Split")]
    public void An_unknown_payment_method_is_refused(string method) =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => MoneyRules.KindForMethod(method)).StatusCode);

    [Fact]
    public void An_account_can_pay_up_to_its_balance_and_not_one_piaster_more()
    {
        MoneyRules.EnsureCanPay(100m, 100m);
        MoneyRules.EnsureCanPay(100m, 0.01m);
        Assert.Equal(409, Assert.Throws<RetailException>(() => MoneyRules.EnsureCanPay(100m, 100.01m)).StatusCode);
        Assert.Equal(409, Assert.Throws<RetailException>(() => MoneyRules.EnsureCanPay(0m, 1m)).StatusCode);
    }

    [Fact]
    public void Account_kind_opening_balance_and_amount_are_validated()
    {
        Assert.Equal(MoneyAccountKind.Wallet, MoneyRules.Kind(" Wallet "));
        Assert.Equal(400, Assert.Throws<RetailException>(() => MoneyRules.Kind("Cheque")).StatusCode);
        Assert.Equal(0m, MoneyRules.Opening(0m));
        Assert.Equal(10.12m, MoneyRules.Opening(10.123m));
        Assert.Equal(400, Assert.Throws<RetailException>(() => MoneyRules.Opening(-0.01m)).StatusCode);
        Assert.Equal(5m, MoneyRules.Amount(5m));
        Assert.Equal(400, Assert.Throws<RetailException>(() => MoneyRules.Amount(0m)).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => MoneyRules.Amount(0.004m)).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => MoneyRules.Name(" ")).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => MoneyRules.Name(new string('x', MoneyRules.MaxNameLength + 1))).StatusCode);
    }

    private static readonly IReadOnlySet<string> ActiveTypes = new HashSet<string> { "إيجار", "إنترنت" };

    [Fact]
    public void An_expense_category_must_be_an_active_type()
    {
        Assert.Equal("إيجار", ExpenseRules.Category(" إيجار ", ActiveTypes));
        Assert.Equal(400, Assert.Throws<RetailException>(() => ExpenseRules.Category("غير موجود", ActiveTypes)).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => ExpenseRules.Category(" ", ActiveTypes)).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => ExpenseRules.Category(null, ActiveTypes)).StatusCode);
    }

    [Fact]
    public void Classification_defaults_to_overhead_and_a_direct_expense_needs_a_link()
    {
        Assert.Equal(ExpenseClassification.GeneralOverhead, ExpenseRules.Classification(null));
        Assert.Equal(ExpenseClassification.GeneralOverhead, ExpenseRules.Classification("  "));
        Assert.Equal(ExpenseClassification.Direct, ExpenseRules.Classification("Direct"));
        Assert.Equal(400, Assert.Throws<RetailException>(() => ExpenseRules.Classification("Weird")).StatusCode);

        ExpenseRules.EnsureDirectLink(ExpenseClassification.GeneralOverhead, null, null, null);
        ExpenseRules.EnsureDirectLink(ExpenseClassification.Direct, Guid.NewGuid(), null, null);
        ExpenseRules.EnsureDirectLink(ExpenseClassification.Direct, null, Guid.NewGuid(), null);
        ExpenseRules.EnsureDirectLink(ExpenseClassification.Direct, null, null, Guid.NewGuid());
        Assert.Equal(400, Assert.Throws<RetailException>(() =>
            ExpenseRules.EnsureDirectLink(ExpenseClassification.Direct, null, null, null)).StatusCode);
    }

    [Fact]
    public void An_expense_is_paid_by_one_payer_and_has_a_positive_amount_and_a_day_inside_every_month()
    {
        ExpenseRules.EnsureSinglePayer(null, null);
        ExpenseRules.EnsureSinglePayer(Guid.NewGuid(), null);
        ExpenseRules.EnsureSinglePayer(null, Guid.NewGuid());
        Assert.Equal(400, Assert.Throws<RetailException>(() => ExpenseRules.EnsureSinglePayer(Guid.NewGuid(), Guid.NewGuid())).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => ExpenseRules.Amount(0m)).StatusCode);
        Assert.Equal(1, ExpenseRules.DayOfMonth(1));
        Assert.Equal(28, ExpenseRules.DayOfMonth(28));
        Assert.Equal(400, Assert.Throws<RetailException>(() => ExpenseRules.DayOfMonth(0)).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => ExpenseRules.DayOfMonth(29)).StatusCode);
    }

    [Fact]
    public void A_template_is_due_in_its_start_month_once_the_day_is_reached()
    {
        Assert.Equal([new DateOnly(2026, 9, 5)], ExpenseRules.DueDates(new DateOnly(2026, 9, 17), null, 5, new DateOnly(2026, 9, 20)));
        Assert.Equal([new DateOnly(2026, 9, 20)], ExpenseRules.DueDates(new DateOnly(2026, 9, 1), null, 20, new DateOnly(2026, 9, 20)));
    }

    [Fact]
    public void A_template_is_not_due_before_its_day_or_once_the_month_is_posted()
    {
        Assert.Empty(ExpenseRules.DueDates(new DateOnly(2026, 9, 1), null, 25, new DateOnly(2026, 9, 20)));
        Assert.Empty(ExpenseRules.DueDates(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), 5, new DateOnly(2026, 9, 20)));
        Assert.Empty(ExpenseRules.DueDates(new DateOnly(2026, 10, 1), null, 5, new DateOnly(2026, 9, 20)));
    }

    [Fact]
    public void Every_missed_month_is_due_in_order_up_to_this_month()
    {
        var start = new DateOnly(2026, 7, 1);
        var posted = new DateOnly(2026, 7, 1);
        Assert.Equal([new DateOnly(2026, 8, 10), new DateOnly(2026, 9, 10), new DateOnly(2026, 10, 10)],
            ExpenseRules.DueDates(start, posted, 10, new DateOnly(2026, 10, 15)));
        Assert.Equal([new DateOnly(2026, 8, 10), new DateOnly(2026, 9, 10)], ExpenseRules.DueDates(start, posted, 10, new DateOnly(2026, 10, 5)));
        Assert.Equal([new DateOnly(2026, 7, 10), new DateOnly(2026, 8, 10)], ExpenseRules.DueDates(start, null, 10, new DateOnly(2026, 8, 10)));
    }

    [Fact]
    public void Due_dates_cross_the_year_and_never_start_before_the_start_month()
    {
        Assert.Equal([new DateOnly(2026, 11, 28), new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 28), new DateOnly(2027, 2, 28)],
            ExpenseRules.DueDates(new DateOnly(2026, 11, 1), null, 28, new DateOnly(2027, 2, 28)));
        Assert.Equal([new DateOnly(2026, 7, 5)],
            ExpenseRules.DueDates(new DateOnly(2026, 7, 1), new DateOnly(2026, 5, 1), 5, new DateOnly(2026, 7, 6)));
    }
}

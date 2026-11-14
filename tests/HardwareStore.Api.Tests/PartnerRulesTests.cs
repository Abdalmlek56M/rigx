using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Partner rules (no database): card checks, shares, whole-shop base, basis and capital rules.</summary>
public sealed class PartnerRulesTests
{
    private static int StatusOf(Action action) => Assert.Throws<RetailException>(action).StatusCode;

    private static Partner Partner(string basis = PartnerBasis.FinancedParts, bool active = true) =>
        new() { Id = Guid.NewGuid(), Name = "P", Basis = basis, IsActive = active };

    private static PartnerCapitalEntry Entry(string kind, decimal amount) =>
        new() { Id = Guid.NewGuid(), PartnerId = Guid.NewGuid(), Kind = kind, Amount = amount };

    [Fact]
    public void CleanCard_trims_name_normalizes_phone_and_empties_blank_notes()
    {
        var (name, phone, notes) = PartnerRules.CleanCard("  Ahmed  ", "+20 101 234 5678", 10m, PartnerBasis.WholeShop, "   ");
        Assert.Equal("Ahmed", name);
        Assert.Equal("01012345678", phone);
        Assert.Null(notes);
        Assert.Null(PartnerRules.CleanCard("A", null, 0m, PartnerBasis.FinancedParts, " note ").Phone);
        Assert.Equal("note", PartnerRules.CleanCard("A", "", 0m, PartnerBasis.FinancedParts, " note ").Notes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void CleanCard_requires_a_name(string? name) =>
        Assert.Equal(400, StatusOf(() => PartnerRules.CleanCard(name, null, 10m, PartnerBasis.FinancedParts, null)));

    [Fact]
    public void CleanCard_rejects_long_name_bad_phone_bad_basis_bad_percent_and_long_notes()
    {
        Assert.Equal(400, StatusOf(() => PartnerRules.CleanCard(new string('a', PartnerRules.NameMaxLength + 1), null, 10m, PartnerBasis.FinancedParts, null)));
        Assert.Equal(400, StatusOf(() => PartnerRules.CleanCard("A", "12345", 10m, PartnerBasis.FinancedParts, null)));
        Assert.Equal(400, StatusOf(() => PartnerRules.CleanCard("A", null, 10m, "Other", null)));
        Assert.Equal(400, StatusOf(() => PartnerRules.CleanCard("A", null, 10m, null, null)));
        Assert.Equal(400, StatusOf(() => PartnerRules.CleanCard("A", null, 100.01m, PartnerBasis.FinancedParts, null)));
        Assert.Equal(400, StatusOf(() => PartnerRules.CleanCard("A", null, -1m, PartnerBasis.FinancedParts, null)));
        Assert.Equal(400, StatusOf(() => PartnerRules.CleanCard("A", null, 10m, PartnerBasis.FinancedParts, new string('n', PartnerRules.NotesMaxLength + 1))));
        PartnerRules.CleanCard("A", null, 10m, PartnerBasis.FinancedParts, new string('n', PartnerRules.NotesMaxLength));
    }

    [Theory]
    [InlineData(200, 10, 20)]
    [InlineData(33.33, 10, 3.33)]
    [InlineData(10.01, 33.33, 3.34)]
    public void Share_is_the_rounded_percent_of_the_profit(double profit, double percent, double expected) =>
        Assert.Equal((decimal)expected, PartnerRules.Share((decimal)profit, (decimal)percent, false));

    [Fact]
    public void Share_of_a_loss_is_zero_unless_loss_sharing_is_on()
    {
        Assert.Equal(0m, PartnerRules.Share(-50m, 10m, false));
        Assert.Equal(-5m, PartnerRules.Share(-50m, 10m, true));
    }

    [Fact]
    public void EffectivePercent_prefers_the_unit_percent() =>
        Assert.Equal((25m, 10m, 0m), (PartnerRules.EffectivePercent(25m, 10m), PartnerRules.EffectivePercent(null, 10m), PartnerRules.EffectivePercent(0m, 10m)));

    [Fact]
    public void WholeShopBase_is_store_net_minus_financed_shares()
    {
        Assert.Equal(250m, PartnerRules.WholeShopBase(270m, 20m));
        Assert.Equal(-70m, PartnerRules.WholeShopBase(-50m, 20m));
    }

    [Fact]
    public void Whole_shop_percents_together_cannot_pass_100()
    {
        PartnerRules.EnsureWholeShopTotal(60m, 40m);
        Assert.Equal(400, StatusOf(() => PartnerRules.EnsureWholeShopTotal(60m, 40.01m)));
    }

    [Theory]
    [InlineData(PartnerBasis.FinancedParts, PartnerBasis.WholeShop, 1, true)]
    [InlineData(PartnerBasis.FinancedParts, PartnerBasis.WholeShop, 0, false)]
    [InlineData(PartnerBasis.WholeShop, PartnerBasis.FinancedParts, 0, false)]
    [InlineData(PartnerBasis.FinancedParts, PartnerBasis.FinancedParts, 3, false)]
    public void Switching_to_whole_shop_needs_no_unsold_financed_units(string current, string next, int unsold, bool rejected)
    {
        if (rejected) Assert.Equal(409, StatusOf(() => PartnerRules.EnsureBasisChange(current, next, unsold)));
        else PartnerRules.EnsureBasisChange(current, next, unsold);
    }

    [Fact]
    public void Only_active_financed_parts_partners_can_finance()
    {
        PartnerRules.EnsureCanFinance(Partner());
        Assert.Equal(400, StatusOf(() => PartnerRules.EnsureCanFinance(Partner(active: false))));
        Assert.Equal(400, StatusOf(() => PartnerRules.EnsureCanFinance(Partner(PartnerBasis.WholeShop))));
    }

    [Fact]
    public void A_month_is_closed_only_after_it_ended()
    {
        var month = PartnerRules.MonthStart(2026, 9);
        Assert.Equal(new DateOnly(2026, 9, 1), month);
        PartnerRules.EnsureMonthEnded(month, new DateOnly(2026, 10, 1));
        Assert.Equal(409, StatusOf(() => PartnerRules.EnsureMonthEnded(month, new DateOnly(2026, 9, 30))));
        Assert.Equal(400, StatusOf(() => PartnerRules.MonthStart(2026, 13)));
        Assert.Equal(400, StatusOf(() => PartnerRules.MonthStart(1999, 1)));
    }

    [Fact]
    public void Capital_entry_checks_kind_amount_and_balance()
    {
        PartnerRules.EnsureCapitalEntry(PartnerCapitalKind.Deposit, 10m, 0m);
        PartnerRules.EnsureCapitalEntry(PartnerCapitalKind.Withdrawal, 100m, 100m);
        Assert.Equal(400, StatusOf(() => PartnerRules.EnsureCapitalEntry("Gift", 10m, 0m)));
        Assert.Equal(400, StatusOf(() => PartnerRules.EnsureCapitalEntry(null, 10m, 0m)));
        Assert.Equal(400, StatusOf(() => PartnerRules.EnsureCapitalEntry(PartnerCapitalKind.Deposit, 0m, 0m)));
        Assert.Equal(409, StatusOf(() => PartnerRules.EnsureCapitalEntry(PartnerCapitalKind.Withdrawal, 100.01m, 100m)));
    }

    [Fact]
    public void Capital_balance_is_deposits_minus_withdrawals()
    {
        Assert.Equal(0m, PartnerRules.CapitalBalance([]));
        Assert.Equal(650m, PartnerRules.CapitalBalance([Entry("Deposit", 1000m), Entry("Withdrawal", 400m), Entry("Deposit", 50m)]));
    }

    [Fact]
    public void SplitProfitByCost_is_proportional_with_the_remainder_on_the_last_unit()
    {
        var split = PartnerRules.SplitProfitByCost(100m, [100m, 100m, 100m]);
        Assert.Equal([33.33m, 33.33m, 33.34m], split);
        var weighted = PartnerRules.SplitProfitByCost(300m, [100m, 200m]);
        Assert.Equal([100m, 200m], weighted);
        Assert.Equal(300m, PartnerRules.SplitProfitByCost(300m, [10m, 20m, 33m]).Sum());
    }

    [Fact]
    public void SplitProfitByCost_splits_equally_when_everything_is_free()
    {
        Assert.Equal([30m, 30m], PartnerRules.SplitProfitByCost(60m, [0m, 0m]));
        Assert.Empty(PartnerRules.SplitProfitByCost(60m, []));
        Assert.Equal([-50m], PartnerRules.SplitProfitByCost(-50m, [200m]));
    }
}

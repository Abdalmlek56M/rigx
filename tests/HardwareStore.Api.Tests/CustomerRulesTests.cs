using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Customer ownership of sales records, customer statistics and the merge rules (roadmap 2.18).</summary>
public sealed class CustomerRulesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.NewGuid();

    private static StoreOrder Order(Guid? account, string phone) => new()
    {
        Reference = "RIGX-1", TrackingToken = "t", CustomerName = "N", PhoneNumber = phone, Status = OrderStatus.New, PaymentStatus = PaymentStatus.Unpaid,
        UserAccountId = account
    };

    private static CustomerOrderFacts Facts(string status, decimal total, int day, decimal refunded = 0m, string? lead = null, string? usage = null, string? governorate = null) =>
        new(status, total, refunded, T0.AddDays(day), lead, usage, governorate);

    [Theory]
    [InlineData("+20 101 234 5678", "01012345678")]
    [InlineData("0020 101 234 5678", "01012345678")]
    [InlineData("01012345678", "01012345678")]
    [InlineData("123", null)]
    [InlineData(null, null)]
    public void PhoneKey_uses_the_stored_order_form_and_ignores_unusable_numbers(string? phone, string? expected) =>
        Assert.Equal(expected, CustomerOwnership.PhoneKey(phone));

    [Fact]
    public void An_order_is_owned_through_the_account_or_by_phone_only_when_it_has_no_account()
    {
        var owns = CustomerOwnership.OrdersOf([Account], ["01012345678"]).Compile();

        Assert.True(owns(Order(Account, "01099999999")));
        Assert.True(owns(Order(null, "01012345678")));
        Assert.False(owns(Order(Guid.NewGuid(), "01012345678")));
        Assert.False(owns(Order(null, "01011111111")));
        Assert.False(CustomerOwnership.OrdersOf([Account], []).Compile()(Order(null, "01012345678")));
    }

    [Fact]
    public void A_warranty_follows_the_same_ownership_rule()
    {
        Warranty Row(Guid? account, string phone) => new()
        {
            Code = "RX-1", Name = "GPU", Condition = "New", Label = "L", PhoneNumber = phone, UserAccountId = account
        };
        var owns = CustomerOwnership.WarrantiesOf(Account, "01012345678").Compile();

        Assert.True(owns(Row(Account, "01099999999")));
        Assert.True(owns(Row(null, "01012345678")));
        Assert.False(owns(Row(Guid.NewGuid(), "01012345678")));
        Assert.False(CustomerOwnership.WarrantiesOf(Account, null).Compile()(Row(null, "01012345678")));
    }

    [Fact]
    public void Owns_matches_the_expression_rule()
    {
        Assert.True(CustomerOwnership.Owns(Account, null, Account, "x"));
        Assert.True(CustomerOwnership.Owns(Account, "0101", null, "0101"));
        Assert.False(CustomerOwnership.Owns(Account, "0101", Guid.NewGuid(), "0101"));
        Assert.False(CustomerOwnership.Owns(Account, null, null, "0101"));
    }

    [Fact]
    public void Stats_count_orders_that_are_not_cancelled_and_spend_only_delivered_or_completed_net_of_refunds()
    {
        var stats = CustomerStatsRules.Summarize(
        [
            Facts(OrderStatus.Delivered, 1000m, 1, refunded: 100m),
            Facts(OrderStatus.Completed, 500m, 2),
            Facts(OrderStatus.Confirmed, 300m, 3),
            Facts(OrderStatus.Cancelled, 900m, 4)
        ]);

        Assert.Equal((3, 1400m, T0.AddDays(3)), (stats.OrderCount, stats.TotalSpent, stats.LastOrderAtUtc));
    }

    [Fact]
    public void Stats_of_no_orders_are_empty()
    {
        var stats = CustomerStatsRules.Summarize([]);

        Assert.Equal((0, 0m, null, null, null, null), (stats.OrderCount, stats.TotalSpent, stats.LastOrderAtUtc, stats.LeadSource, stats.Usage, stats.Governorate));
    }

    [Fact]
    public void Lead_source_and_usage_come_from_the_earliest_order_that_has_them_and_the_governorate_from_the_latest()
    {
        var stats = CustomerStatsRules.Summarize(
        [
            Facts(OrderStatus.Delivered, 1m, 5, lead: "Google", usage: "Work", governorate: "GZ"),
            Facts(OrderStatus.Delivered, 1m, 1, lead: null, usage: "Gaming", governorate: "CAI"),
            Facts(OrderStatus.Delivered, 1m, 2, lead: "Facebook", usage: null, governorate: null),
            Facts(OrderStatus.Delivered, 1m, 3, lead: "Friend", usage: null, governorate: "ALX")
        ]);

        Assert.Equal(("Facebook", "Gaming", "GZ"), (stats.LeadSource, stats.Usage, stats.Governorate));
    }

    // ---------------------------------------------------------------- merge rules

    private static MergeParty Party(Guid? id = null, bool customer = true, bool active = true, Guid? mergedInto = null) =>
        new(id ?? Guid.NewGuid(), customer, active, mergedInto);

    [Fact]
    public void Two_active_unmerged_customers_can_be_merged() =>
        CustomerMergeRules.EnsureCanMerge(Party(), Party());

    [Fact]
    public void An_account_cannot_be_merged_into_itself()
    {
        var id = Guid.NewGuid();

        Assert.Equal(400, Assert.Throws<IdentityException>(() => CustomerMergeRules.EnsureCanMerge(Party(id), Party(id))).StatusCode);
    }

    [Fact]
    public void Staff_accounts_already_merged_accounts_and_an_inactive_target_are_refused_with_a_conflict()
    {
        Assert.Equal(409, Assert.Throws<IdentityException>(() => CustomerMergeRules.EnsureCanMerge(Party(customer: false), Party())).StatusCode);
        Assert.Equal(409, Assert.Throws<IdentityException>(() => CustomerMergeRules.EnsureCanMerge(Party(), Party(customer: false))).StatusCode);
        Assert.Equal(409, Assert.Throws<IdentityException>(() => CustomerMergeRules.EnsureCanMerge(Party(mergedInto: Guid.NewGuid()), Party())).StatusCode);
        Assert.Equal(409, Assert.Throws<IdentityException>(() => CustomerMergeRules.EnsureCanMerge(Party(), Party(mergedInto: Guid.NewGuid()))).StatusCode);
        Assert.Equal(409, Assert.Throws<IdentityException>(() => CustomerMergeRules.EnsureCanMerge(Party(active: false), Party())).StatusCode);
    }

    [Fact]
    public void An_inactive_source_can_still_be_merged() =>
        CustomerMergeRules.EnsureCanMerge(Party(), Party(active: false));

    [Fact]
    public void Merged_notes_keep_the_targets_note_first_and_name_the_sources_person_code()
    {
        Assert.Equal("keep", CustomerMergeRules.CombineNotes("keep", null, "PER-1"));
        Assert.Equal("keep", CustomerMergeRules.CombineNotes("keep", "  ", "PER-1"));
        Assert.Equal("from source", CustomerMergeRules.CombineNotes(null, "from source", "PER-1"));
        Assert.Null(CustomerMergeRules.CombineNotes(null, null, "PER-1"));
        Assert.Equal("keep\n--- PER-1 ---\nfrom source", CustomerMergeRules.CombineNotes("keep", "from source", "PER-1"));
        Assert.Equal(CustomerAccounts.MaxNoteLength, CustomerMergeRules.CombineNotes(new string('a', 1990), new string('b', 100), "PER-1")!.Length);
    }

    [Fact]
    public void Only_accounts_without_staff_roles_are_customers()
    {
        Assert.True(CustomerAccounts.IsCustomer(["Customer"]));
        Assert.True(CustomerAccounts.IsCustomer([]));
        Assert.False(CustomerAccounts.IsCustomer(["Customer", "Sales"]));
        Assert.False(CustomerAccounts.IsCustomer(["Owner"]));
    }

    [Theory]
    [InlineData("0100 777", "0100777", null)]
    [InlineData("+20 100 777 1234", "01007771234", null)]
    [InlineData("مني", null, "%مني%")]
    [InlineData(@"  50%_off\ ", null, @"%50\%\_off\\%")]
    public void Pos_customer_search_reads_phone_digits_or_part_of_a_name(string search, string? digits, string? pattern)
    {
        var query = PosCustomerQuery.Parse(search);

        Assert.Equal((digits, pattern), (query.PhoneDigits, query.NamePattern));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("010")]
    [InlineData("م")]
    public void Pos_customer_search_needs_four_digits_or_two_letters(string? search) =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => PosCustomerQuery.Parse(search)).StatusCode);
}

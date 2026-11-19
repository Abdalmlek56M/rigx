using HardwareStore.Modules.Storefront;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class NotifyMeRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Gpus = Guid.NewGuid();

    private static CustomerRequest Request(string status = CustomerRequestStatus.New, string? modelKey = "rx6600", string? availableItemCode = null,
        string type = CustomerRequestType.NotifyMe) => new()
    {
        Id = Guid.NewGuid(), Code = "REQ-0001", Type = type, Status = status, Name = "Ahmed", Phone = "01012345678", ModelKey = modelKey,
        AvailableItemCode = availableItemCode, AvailableAtUtc = availableItemCode is null ? null : Now
    };

    private static NotifyMeEntry Entry(string key, string name, DateTimeOffset createdAt, string status = CustomerRequestStatus.New,
        string? availableItemCode = null, DateTimeOffset? notifiedAt = null, Guid? categoryId = null) =>
        new(key, name, categoryId, status, availableItemCode, notifiedAt, createdAt);

    private static void AssertStatus(int expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<StorefrontException>(action).StatusCode);

    [Theory]
    [InlineData("rx 142", "RX-0142")]
    [InlineData("RX-0001", "RX-0001")]
    public void A_typed_code_is_normalized(string text, string expected) => Assert.Equal(expected, NotifyMeRules.ItemCodeOf(text));

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void No_code_typed_means_no_code(string? text) => Assert.Null(NotifyMeRules.ItemCodeOf(text));

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", null)]
    [InlineData("Any", null)]
    [InlineData("New", "New")]
    [InlineData(" Used ", "Used")]
    public void The_condition_is_new_used_or_any(string? text, string? expected) => Assert.Equal(expected, NotifyMeRules.CheckCondition(text));

    [Theory]
    [InlineData("Open Box")]
    [InlineData("used")]
    public void Other_conditions_are_refused(string text) => AssertStatus(400, () => NotifyMeRules.CheckCondition(text));

    [Fact]
    public void Pick_takes_the_cheapest_item_in_the_asked_condition_or_the_cheapest_of_any()
    {
        IReadOnlyList<AvailableStockItem> available = [new("RX-0001", "RX 6600", "Used"), new("RX-0002", "RX 6600", "New")];

        Assert.Equal("RX-0001", NotifyMeRules.Pick(available, null)?.Code);
        Assert.Equal("RX-0002", NotifyMeRules.Pick(available, "New")?.Code);
        Assert.Null(NotifyMeRules.Pick([available[1]], "Used"));
        Assert.Null(NotifyMeRules.Pick(null, null));
    }

    [Fact]
    public void Text_that_is_not_an_rx_code_is_an_unknown_item() => AssertStatus(404, () => NotifyMeRules.ItemCodeOf("BLD-07"));

    [Fact]
    public void A_code_targets_the_model_of_that_item_and_ignores_the_typed_model()
    {
        var target = NotifyMeRules.Target("RX-0042", "Something else", new StockModel("RX 6600", "rx6600", Gpus));

        Assert.Equal(new NotifyMeTarget("RX-0042", "RX 6600", "rx6600", Gpus), target);
    }

    [Fact]
    public void A_code_that_no_item_has_is_not_found() => AssertStatus(404, () => NotifyMeRules.Target("RX-0042", null, null));

    [Fact]
    public void A_typed_model_is_cleaned_and_keyed_like_the_stock_so_spellings_match()
    {
        var spaced = NotifyMeRules.Target(null, "  RX   6600 ", null);
        var dashed = NotifyMeRules.Target(null, "rx-6600", null);

        Assert.Equal(new NotifyMeTarget(null, "RX 6600", "rx6600", null), spaced);
        Assert.Equal(spaced.ModelKey, dashed.ModelKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("---")]
    public void A_model_without_letters_or_digits_is_a_bad_request(string? model) => AssertStatus(400, () => NotifyMeRules.Target(null, model, null));

    [Fact]
    public void Only_a_matched_open_notify_me_request_can_be_marked_as_sent_and_it_closes()
    {
        var matched = Request(CustomerRequestStatus.Following, availableItemCode: "RX-0042");

        NotifyMeRules.MarkSent(matched, Now);

        Assert.Equal((CustomerRequestStatus.Closed, Now, Now), (matched.Status, matched.NotifiedAtUtc, matched.ClosedAtUtc));
    }

    [Fact]
    public void Marking_sent_is_refused_without_a_match_after_closing_twice_or_on_another_type()
    {
        AssertStatus(409, () => NotifyMeRules.MarkSent(Request(), Now));
        AssertStatus(409, () => NotifyMeRules.MarkSent(Request(CustomerRequestStatus.Closed, availableItemCode: "RX-0042"), Now));
        AssertStatus(400, () => NotifyMeRules.MarkSent(Request(type: CustomerRequestType.Contact, availableItemCode: "RX-0042"), Now));
        var sent = Request(availableItemCode: "RX-0042");
        NotifyMeRules.MarkSent(sent, Now);
        sent.Status = CustomerRequestStatus.Following;
        AssertStatus(409, () => NotifyMeRules.MarkSent(sent, Now));
    }

    [Fact]
    public void The_demand_groups_by_model_most_asked_first_with_the_latest_name_and_counts()
    {
        var entries = new[]
        {
            Entry("rx6600", "rx6600", Now.AddDays(-3), categoryId: Gpus),
            Entry("rx6600", "RX 6600", Now.AddDays(-1), CustomerRequestStatus.Following, "RX-0042"),
            Entry("rx6600", "RX-6600", Now.AddDays(-2), CustomerRequestStatus.Closed, "RX-0042", Now.AddDays(-1)),
            Entry("ryzen55600", "Ryzen 5 5600", Now.AddHours(-1))
        };

        var rows = NotifyMeRules.Demand(entries, new HashSet<string> { "ryzen55600" });

        Assert.Equal(["rx6600", "ryzen55600"], rows.Select(row => row.ModelKey));
        Assert.Equal(("RX 6600", Gpus, 3, 1, 1, Now.AddDays(-1), false),
            (rows[0].ModelName, rows[0].CategoryId, rows[0].Total, rows[0].Open, rows[0].Notified, rows[0].LastRequestedAtUtc, rows[0].AvailableNow));
        Assert.Equal((1, 1, 0, true), (rows[1].Total, rows[1].Open, rows[1].Notified, rows[1].AvailableNow));
    }

    [Fact]
    public void Equal_demand_shows_the_most_recent_model_first_and_nothing_gives_no_rows()
    {
        var rows = NotifyMeRules.Demand([Entry("a", "A", Now.AddDays(-2)), Entry("b", "B", Now.AddDays(-1))], new HashSet<string>());

        Assert.Equal(["b", "a"], rows.Select(row => row.ModelKey));
        Assert.Empty(NotifyMeRules.Demand([], new HashSet<string>()));
    }
}

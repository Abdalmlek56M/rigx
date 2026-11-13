using HardwareStore.Common.Domain;
using HardwareStore.Modules.Storefront;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class CustomerRequestRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 13);
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);
    private const string LongMessage = "This message is long enough.";

    private static CustomerRequestSubmission Submission(string type, Func<CustomerRequestSubmission, CustomerRequestSubmission>? change = null)
    {
        var request = new CustomerRequestSubmission(type, "Ahmed", "01012345678", null, null, null, null, null, null, null, null, null,
            null, null, null);
        return change?.Invoke(request) ?? request;
    }

    private static CustomerRequest Request(string type, string status, decimal? offer = null) => new()
    {
        Id = Guid.NewGuid(), Code = "REQ-0001", Type = type, Status = status, Name = "Ahmed", Phone = "01012345678", OfferAmount = offer
    };

    private static void AssertStatus(int expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<StorefrontException>(action).StatusCode);

    [Theory]
    [InlineData(CustomerRequestType.Sell)]
    [InlineData(CustomerRequestType.TradeIn)]
    public void Sell_and_trade_in_need_a_model_and_keep_their_optional_details(string type)
    {
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(Submission(type), Today));

        var draft = CustomerRequestRules.CheckSubmission(
            Submission(type, request => request with { ModelName = " RTX 3060 ", PartType = "GPU", OwnedFor = "2 years", Message = "Boxed" }), Today);

        Assert.Equal(("RTX 3060", "GPU", "2 years", "Boxed"), (draft.ModelName, draft.PartType, draft.OwnedFor, draft.Message));
    }

    [Theory]
    [InlineData(CustomerRequestType.Upgrade, null)]
    [InlineData(CustomerRequestType.Upgrade, "too short")]
    [InlineData(CustomerRequestType.Support, "123456789")]
    [InlineData(CustomerRequestType.Contact, "   ")]
    public void Upgrade_support_and_contact_need_a_message_of_at_least_ten_characters(string type, string? message) =>
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(Submission(type, request => request with { Message = message }), Today));

    [Theory]
    [InlineData(CustomerRequestType.Upgrade)]
    [InlineData(CustomerRequestType.Support)]
    [InlineData(CustomerRequestType.Contact)]
    public void A_message_of_exactly_ten_characters_is_accepted(string type) =>
        Assert.Equal("1234567890",
            CustomerRequestRules.CheckSubmission(Submission(type, request => request with { Message = "1234567890" }), Today).Message);

    [Fact]
    public void Notify_me_needs_an_item_code_or_a_model()
    {
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(Submission(CustomerRequestType.NotifyMe), Today));

        var byCode = CustomerRequestRules.CheckSubmission(Submission(CustomerRequestType.NotifyMe, r => r with { ItemCode = "RX-0142" }), Today);
        var byModel = CustomerRequestRules.CheckSubmission(Submission(CustomerRequestType.NotifyMe, r => r with { ModelName = "RTX 3060" }), Today);

        Assert.Equal("RX-0142", byCode.ItemCode);
        Assert.Equal("RTX 3060", byModel.ModelName);
    }

    [Fact]
    public void Service_needs_a_known_kind_and_a_date_that_is_not_in_the_past()
    {
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(Submission(CustomerRequestType.Service), Today));
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(
            Submission(CustomerRequestType.Service, r => r with { ServiceKind = "Cooking" }), Today));
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(
            Submission(CustomerRequestType.Service, r => r with { ServiceKind = "Build", PreferredDate = Today.AddDays(-1) }), Today));

        var draft = CustomerRequestRules.CheckSubmission(
            Submission(CustomerRequestType.Service, r => r with { ServiceKind = "build", PreferredDate = Today }), Today);

        Assert.Equal(("Build", (DateOnly?)Today), (draft.ServiceKind, draft.PreferredDate));
    }

    [Fact]
    public void Fields_that_do_not_belong_to_the_type_are_dropped()
    {
        var everything = Submission(CustomerRequestType.Contact, r => r with
        {
            Message = LongMessage, Subject = "Hello", PartType = "GPU", ModelName = "X", OwnedFor = "1y", ItemCode = "RX-1",
            ServiceKind = "Build", PreferredDate = Today
        });

        var draft = CustomerRequestRules.CheckSubmission(everything, Today);

        Assert.Equal("Hello", draft.Subject);
        Assert.Null(draft.PartType);
        Assert.Null(draft.ModelName);
        Assert.Null(draft.OwnedFor);
        Assert.Null(draft.ItemCode);
        Assert.Null(draft.ServiceKind);
        Assert.Null(draft.PreferredDate);
    }

    [Fact]
    public void Common_fields_are_cleaned_and_the_type_is_matched_ignoring_case()
    {
        var draft = CustomerRequestRules.CheckSubmission(
            Submission("contact", r => r with
            {
                Name = "  Ahmed  ", Phone = "+20 101 234 5678", Email = "a@b.test", Governorate = "gz", Source = "Facebook", Message = LongMessage
            }), Today);

        Assert.Equal(CustomerRequestType.Contact, draft.Type);
        Assert.Equal(("Ahmed", "01012345678", "a@b.test", "GZ", "Facebook"), (draft.Name, draft.Phone, draft.Email, draft.Governorate, draft.Source));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("02112345678")]
    [InlineData(null)]
    public void A_phone_that_is_not_an_egyptian_mobile_is_a_bad_request(string? phone) =>
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(
            Submission(CustomerRequestType.Contact, r => r with { Phone = phone, Message = LongMessage }), Today));

    [Theory]
    [InlineData("Wizard")]
    [InlineData("")]
    [InlineData(null)]
    public void An_unknown_type_is_a_bad_request(string? type) =>
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(Submission(type!, r => r with { Message = LongMessage }), Today));

    [Theory]
    [InlineData("A")]
    [InlineData(null)]
    public void A_name_shorter_than_two_characters_is_a_bad_request(string? name) =>
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(
            Submission(CustomerRequestType.Contact, r => r with { Name = name, Message = LongMessage }), Today));

    [Theory]
    [InlineData("not-an-email", null)]
    [InlineData(null, "XX")]
    public void A_bad_email_or_governorate_is_a_bad_request(string? email, string? governorate) =>
        AssertStatus(400, () => CustomerRequestRules.CheckSubmission(
            Submission(CustomerRequestType.Contact, r => r with { Email = email, Governorate = governorate, Message = LongMessage }), Today));

    [Theory]
    [InlineData(CustomerRequestStatus.New, CustomerRequestStatus.Following)]
    [InlineData(CustomerRequestStatus.Closed, CustomerRequestStatus.Following)]
    [InlineData(CustomerRequestStatus.New, CustomerRequestStatus.Closed)]
    [InlineData(CustomerRequestStatus.Offered, CustomerRequestStatus.Closed)]
    [InlineData(CustomerRequestStatus.Following, CustomerRequestStatus.Closed)]
    public void Following_and_closing_are_allowed_from_the_right_statuses(string from, string to) =>
        CustomerRequestRules.EnsureCanSetStatus(Request(CustomerRequestType.Sell, from), to);

    [Theory]
    [InlineData(CustomerRequestStatus.Following, CustomerRequestStatus.Following)]
    [InlineData(CustomerRequestStatus.Offered, CustomerRequestStatus.Following)]
    [InlineData(CustomerRequestStatus.Converted, CustomerRequestStatus.Closed)]
    [InlineData(CustomerRequestStatus.Closed, CustomerRequestStatus.Closed)]
    public void Other_status_moves_are_conflicts(string from, string to) =>
        AssertStatus(409, () => CustomerRequestRules.EnsureCanSetStatus(Request(CustomerRequestType.Sell, from), to));

    [Theory]
    [InlineData(CustomerRequestStatus.Offered)]
    [InlineData(CustomerRequestStatus.Converted)]
    [InlineData("Whatever")]
    [InlineData(null)]
    public void The_manual_status_only_accepts_following_or_closed(string? status) =>
        AssertStatus(400, () => CustomerRequestRules.EnsureCanSetStatus(Request(CustomerRequestType.Sell, CustomerRequestStatus.New), status));

    [Theory]
    [InlineData(CustomerRequestType.Sell, true, true)]
    [InlineData(CustomerRequestType.TradeIn, true, true)]
    [InlineData(CustomerRequestType.Upgrade, true, false)]
    [InlineData(CustomerRequestType.NotifyMe, false, false)]
    [InlineData(CustomerRequestType.Support, false, false)]
    [InlineData(CustomerRequestType.Contact, false, false)]
    [InlineData(CustomerRequestType.Service, false, false)]
    public void Only_some_types_take_an_offer_and_only_sell_and_trade_in_convert(string type, bool offers, bool converts)
    {
        Assert.Equal(offers, CustomerRequestRules.OffersAllowed(type));
        Assert.Equal(converts, CustomerRequestRules.ConvertAllowed(type));
    }

    [Theory]
    [InlineData(CustomerRequestStatus.New)]
    [InlineData(CustomerRequestStatus.Following)]
    [InlineData(CustomerRequestStatus.Offered)]
    [InlineData(CustomerRequestStatus.Rejected)]
    public void An_offer_can_be_made_or_revised_before_it_is_accepted(string status) =>
        CustomerRequestRules.EnsureCanOffer(Request(CustomerRequestType.Upgrade, status));

    [Theory]
    [InlineData(CustomerRequestStatus.Accepted)]
    [InlineData(CustomerRequestStatus.Converted)]
    [InlineData(CustomerRequestStatus.Closed)]
    public void An_offer_after_acceptance_or_closing_is_a_conflict(string status) =>
        AssertStatus(409, () => CustomerRequestRules.EnsureCanOffer(Request(CustomerRequestType.Sell, status)));

    [Fact]
    public void An_offer_on_a_type_that_takes_none_is_a_bad_request() =>
        AssertStatus(400, () => CustomerRequestRules.EnsureCanOffer(Request(CustomerRequestType.Support, CustomerRequestStatus.New)));

    [Theory]
    [InlineData(0, null)]
    [InlineData(-5, null)]
    [InlineData(10_000_000.01, null)]
    [InlineData(100.123, null)]
    [InlineData(100, -1.0)]
    [InlineData(100, 10_000_001.0)]
    [InlineData(100, 50.555)]
    public void Offer_amounts_must_be_positive_limited_and_have_two_decimals(double offer, double? resale) =>
        AssertStatus(400, () => CustomerRequestRules.CheckOfferAmounts((decimal)offer, (decimal?)resale));

    [Theory]
    [InlineData(100, null)]
    [InlineData(100.5, 0.0)]
    [InlineData(10_000_000.0, 10_000_000.0)]
    public void Valid_offer_amounts_pass(double offer, double? resale) =>
        CustomerRequestRules.CheckOfferAmounts((decimal)offer, (decimal?)resale);

    [Theory]
    [InlineData(CustomerRequestStatus.Accepted)]
    [InlineData(CustomerRequestStatus.Rejected)]
    public void A_decision_is_allowed_only_on_an_offered_request(string decision)
    {
        CustomerRequestRules.EnsureCanDecide(Request(CustomerRequestType.Sell, CustomerRequestStatus.Offered, 100m), decision);

        AssertStatus(409, () => CustomerRequestRules.EnsureCanDecide(Request(CustomerRequestType.Sell, CustomerRequestStatus.Following), decision));
    }

    [Theory]
    [InlineData(CustomerRequestStatus.Converted)]
    [InlineData(CustomerRequestStatus.Closed)]
    [InlineData(null)]
    public void A_decision_other_than_accepted_or_rejected_is_a_bad_request(string? decision) =>
        AssertStatus(400, () => CustomerRequestRules.EnsureCanDecide(Request(CustomerRequestType.Sell, CustomerRequestStatus.Offered, 100m), decision));

    [Fact]
    public void Only_an_accepted_sell_or_trade_in_with_an_offer_converts()
    {
        CustomerRequestRules.EnsureCanConvert(Request(CustomerRequestType.Sell, CustomerRequestStatus.Accepted, 100m));
        CustomerRequestRules.EnsureCanConvert(Request(CustomerRequestType.TradeIn, CustomerRequestStatus.Accepted, 100m));

        AssertStatus(400, () => CustomerRequestRules.EnsureCanConvert(Request(CustomerRequestType.Upgrade, CustomerRequestStatus.Accepted, 100m)));
        AssertStatus(409, () => CustomerRequestRules.EnsureCanConvert(Request(CustomerRequestType.Sell, CustomerRequestStatus.Offered, 100m)));
        AssertStatus(409, () => CustomerRequestRules.EnsureCanConvert(Request(CustomerRequestType.Sell, CustomerRequestStatus.Converted, 100m)));
        AssertStatus(409, () => CustomerRequestRules.EnsureCanConvert(Request(CustomerRequestType.Sell, CustomerRequestStatus.Accepted)));
    }

    [Fact]
    public void Closing_and_converting_stamp_the_close_time_and_other_moves_clear_it()
    {
        var request = Request(CustomerRequestType.Sell, CustomerRequestStatus.New);

        CustomerRequestRules.SetStatus(request, CustomerRequestStatus.Closed, Now);
        Assert.Equal(Now, request.ClosedAtUtc);

        CustomerRequestRules.SetStatus(request, CustomerRequestStatus.Following, Now.AddHours(1));
        Assert.Null(request.ClosedAtUtc);

        CustomerRequestRules.SetStatus(request, CustomerRequestStatus.Converted, Now.AddHours(2));
        Assert.Equal(Now.AddHours(2), request.ClosedAtUtc);
    }

    [Fact]
    public void A_note_is_trimmed_cleared_when_blank_and_limited_to_2000_characters()
    {
        Assert.Equal("call back", CustomerRequestRules.CheckNote("  call back "));
        Assert.Null(CustomerRequestRules.CheckNote("   "));
        Assert.Null(CustomerRequestRules.CheckNote(null));
        AssertStatus(400, () => CustomerRequestRules.CheckNote(new string('x', 2001)));
    }
}

public sealed class EgyptMobileTests
{
    [Theory]
    [InlineData("01012345678", "01012345678")]
    [InlineData("010 1234 5678", "01012345678")]
    [InlineData("010-1234-5678", "01012345678")]
    [InlineData("+20 101 234 5678", "01012345678")]
    [InlineData("201012345678", "01012345678")]
    [InlineData("00201012345678", "01012345678")]
    [InlineData("1012345678", "01012345678")]
    public void Common_spellings_normalize_to_one_stored_form(string typed, string expected)
    {
        Assert.True(EgyptMobile.TryNormalize(typed, out var mobile));
        Assert.Equal(expected, mobile);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0101234567")]
    [InlineData("02012345678")]
    [InlineData("010123456789")]
    public void Anything_else_is_not_a_mobile_number(string? typed)
    {
        Assert.False(EgyptMobile.TryNormalize(typed, out var mobile));
        Assert.Equal(string.Empty, mobile);
    }

    [Fact]
    public void The_international_form_drops_the_leading_zero() =>
        Assert.Equal("201012345678", EgyptMobile.International("01012345678"));
}

public sealed class WhatsAppReplyTests
{
    private static CustomerRequest Request(string? model = "RTX 3060", string? item = null, decimal? offer = null) => new()
    {
        Id = Guid.NewGuid(), Code = "REQ-0012", Type = CustomerRequestType.Sell, Status = CustomerRequestStatus.Offered, Name = "Ahmed",
        Phone = "01012345678", ModelName = model, ItemCode = item, OfferAmount = offer
    };

    [Fact]
    public void Placeholders_are_filled_and_unknown_or_missing_ones_become_empty()
    {
        var text = WhatsAppReply.Fill($"{WhatsAppPlaceholder.Name}|{WhatsAppPlaceholder.Item}|{{unknown}}|{WhatsAppPlaceholder.Price}",
            new Dictionary<string, string> { [WhatsAppPlaceholder.Name] = "Ahmed", [WhatsAppPlaceholder.Item] = "GPU" });

        Assert.Equal("Ahmed|GPU||", text);
    }

    [Fact]
    public void A_request_fills_name_phone_item_code_and_price_and_builds_a_wa_me_link()
    {
        var template = $"{WhatsAppPlaceholder.Name} {WhatsAppPlaceholder.Phone} {WhatsAppPlaceholder.Item} {WhatsAppPlaceholder.Code} {WhatsAppPlaceholder.Price}";

        var reply = WhatsAppReply.For(Request(offer: 1500.50m), template);

        Assert.Equal("Ahmed 01012345678 RTX 3060 REQ-0012 1500.5", reply.Text);
        Assert.Equal("01012345678", reply.Phone);
        Assert.Equal($"https://wa.me/201012345678?text={Uri.EscapeDataString(reply.Text)}", reply.Url);
        Assert.DoesNotContain(" ", reply.Url);
    }

    [Fact]
    public void The_item_falls_back_to_the_item_code_and_the_price_is_empty_without_an_offer()
    {
        var reply = WhatsAppReply.For(Request(model: null, item: "RX-0142"), $"{WhatsAppPlaceholder.Item}/{WhatsAppPlaceholder.Price}");

        Assert.Equal("RX-0142/", reply.Text);
    }

    [Fact]
    public void Arabic_text_is_url_encoded() =>
        Assert.StartsWith("https://wa.me/201012345678?text=%D8%A3", WhatsAppReply.Url("01012345678", "أهلًا"));
}

public sealed class RequestPublicCodeTests
{
    [Fact]
    public void Request_codes_use_four_digits_and_grow_when_needed()
    {
        Assert.Equal("REQ-0001", PublicCode.Format(PublicCode.Request, 1));
        Assert.Equal("REQ-12345", PublicCode.Format(PublicCode.Request, 12345));
    }

    [Theory]
    [InlineData("req-12", "REQ-0012")]
    [InlineData("REQ0012", "REQ-0012")]
    [InlineData("req 12", "REQ-0012")]
    public void Typed_request_codes_are_normalized(string typed, string expected)
    {
        Assert.True(PublicCode.TryNormalize(typed, PublicCode.Request, out var code));
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData("RX-0012")]
    [InlineData("Ahmed")]
    public void Other_texts_are_not_request_codes(string typed) =>
        Assert.False(PublicCode.TryNormalize(typed, PublicCode.Request, out _));
}

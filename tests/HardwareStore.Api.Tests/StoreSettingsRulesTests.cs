using HardwareStore.Common.Domain;
using HardwareStore.Modules.Storefront;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class StoreSettingsRulesTests
{
    private static void AssertBadRequest(Action action) =>
        Assert.Equal(400, Assert.Throws<StorefrontException>(action).StatusCode);

    private static ContactLinkSettings Links(params ContactLink[] links) => new() { Links = links };

    private static ContactLink Link(string platform, string value) => new() { Platform = platform, Value = value };

    private static ShippingGroup Group(string name, params string[] governorates) =>
        new() { Name = name, Fee = 50m, Governorates = governorates };

    private static PaymentSettings Payment(Func<PaymentSettings, PaymentSettings> change) => change(new PaymentSettings());

    [Fact]
    public void Every_governorate_has_an_english_name_for_the_english_site()
    {
        Assert.Equal(EgyptGovernorates.NamesByCode.Keys.Order(), EgyptGovernorates.EnglishNamesByCode.Keys.Order());
        Assert.All(EgyptGovernorates.EnglishNamesByCode.Values, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.Equal("Cairo", new ShippingSettings().Rates().Single(rate => rate.Code == "C").NameEn);
    }

    [Fact]
    public void Rates_cover_all_27_governorates_with_the_group_fee_or_the_default()
    {
        var shipping = new ShippingSettings
        {
            DefaultFee = 120m,
            DefaultDeliveryTime = "default",
            Groups = [new() { Name = "A", Fee = 50m, DeliveryTime = "fast", Governorates = ["C"] }, new() { Name = "B", Fee = 70m, Governorates = ["GZ"] }]
        };

        var rates = shipping.Rates();

        Assert.Equal(27, rates.Count);
        Assert.Equal(rates.Count, rates.Select(rate => rate.Code).Distinct().Count());
        Assert.Equal(new GovernorateShippingRate("C", "القاهرة", 50m, "fast", "Cairo"), rates.Single(rate => rate.Code == "C"));
        Assert.Equal(new GovernorateShippingRate("GZ", "الجيزة", 70m, "default", "Giza"), rates.Single(rate => rate.Code == "GZ"));
        Assert.Equal(new GovernorateShippingRate("ASN", "أسوان", 120m, "default", "Aswan"), rates.Single(rate => rate.Code == "ASN"));
    }

    [Fact]
    public void Contact_numbers_keep_digits_and_urls_must_be_absolute_http()
    {
        var normalized = Links(
            Link(ContactPlatform.WhatsApp, "+20 100 000 0000"),
            Link(ContactPlatform.Facebook, "https://facebook.com/rigx")).Normalize();

        Assert.Equal("201000000000", normalized.Links[0].Value);
        Assert.Equal("https://facebook.com/rigx", normalized.Links[1].Value);
    }

    [Theory]
    [InlineData("WhatsApp", "123")]
    [InlineData("Phone", "not a number")]
    [InlineData("Facebook", "facebook.com/rigx")]
    [InlineData("Telegram", "ftp://t.me/rigx")]
    [InlineData("Snapchat", "https://snapchat.com/rigx")]
    public void Contact_link_with_invalid_value_or_platform_is_rejected(string platform, string value) =>
        AssertBadRequest(() => Links(Link(platform, value)).Normalize());

    [Fact]
    public void Duplicate_contact_links_are_rejected_even_when_formatted_differently() =>
        AssertBadRequest(() => Links(
            Link(ContactPlatform.WhatsApp, "+20 100 000 0000"),
            Link(ContactPlatform.WhatsApp, "201000000000")).Normalize());

    [Fact]
    public void Same_value_on_different_platforms_is_allowed() =>
        Assert.Equal(2, Links(
            Link(ContactPlatform.WhatsApp, "201000000000"),
            Link(ContactPlatform.Phone, "201000000000")).Normalize().Links.Count);

    [Fact]
    public void Governorate_in_two_groups_is_rejected() =>
        AssertBadRequest(() => new ShippingSettings { Groups = [Group("A", "C", "GZ"), Group("B", "GZ")] }.Normalize());

    [Fact]
    public void Unknown_governorate_code_is_rejected() =>
        AssertBadRequest(() => new ShippingSettings { Groups = [Group("A", "XX")] }.Normalize());

    [Fact]
    public void Shipping_group_governorate_codes_are_upper_cased()
    {
        var normalized = new ShippingSettings { Groups = [Group("A", " c ", "gz")] }.Normalize();
        Assert.Equal(["C", "GZ"], normalized.Groups[0].Governorates);
    }

    [Fact]
    public void Quote_uses_group_values_and_falls_back_to_the_default_delivery_time()
    {
        var shipping = new ShippingSettings
        {
            DefaultFee = 120m,
            DefaultDeliveryTime = "3-5 أيام",
            Groups = [Group("A", "C")]
        };

        Assert.Equal(new ShippingQuote(50m, "3-5 أيام"), shipping.QuoteFor("C"));
        Assert.Equal(new ShippingQuote(120m, "3-5 أيام"), shipping.QuoteFor("ASN"));
        Assert.Equal(400, Assert.Throws<StorefrontException>(() => shipping.QuoteFor("XX")).StatusCode);
    }

    [Fact]
    public void No_deposit_is_asked_when_the_deposit_is_not_required_but_shipping_still_is()
    {
        var payment = new PaymentSettings { DepositRequired = false };
        Assert.Equal(0m, payment.DepositFor(50_000m));
        Assert.Equal(new UpfrontPayment(80m, 0m, 80m), payment.UpfrontFor(50_000m, 80m, false));
    }

    [Fact]
    public void Deposit_applies_only_above_the_threshold()
    {
        var payment = new PaymentSettings();
        Assert.Equal(0m, payment.DepositFor(10_000m));
        Assert.Equal(1234.56m, payment.DepositFor(12_345.55m));
    }

    [Fact]
    public void Upfront_includes_shipping_only_when_shipping_is_paid_upfront()
    {
        Assert.Equal(new UpfrontPayment(80m, 2000m, 2080m), new PaymentSettings().UpfrontFor(20_000m, 80m, false));
        Assert.Equal(new UpfrontPayment(0m, 2000m, 2000m),
            new PaymentSettings { ShippingPaidUpfront = false }.UpfrontFor(20_000m, 80m, false));
        Assert.Equal(new UpfrontPayment(0m, 0m, 0m), new PaymentSettings().UpfrontFor(20_000m, 80m, true));
    }

    [Fact]
    public void Enabling_a_payment_method_without_its_address_or_number_is_rejected()
    {
        AssertBadRequest(() => Payment(p => p with { InstaPayEnabled = true }).Normalize());
        AssertBadRequest(() => Payment(p => p with { WalletEnabled = true }).Normalize());
        Assert.Equal("01000000000",
            Payment(p => p with { WalletEnabled = true, WalletNumber = "010 0000 0000" }).Normalize().WalletNumber);
    }

    [Fact]
    public void All_payment_methods_off_is_rejected() =>
        AssertBadRequest(() => Payment(p => p with { CashOnDeliveryEnabled = false }).Normalize());

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(10.123)]
    public void Deposit_percent_out_of_range_is_rejected(double percent) =>
        AssertBadRequest(() => Payment(p => p with { DepositPercent = (decimal)percent }).Normalize());

    [Fact]
    public void Homepage_sections_must_list_every_key_exactly_once()
    {
        var all = HomepageSectionKey.All.Select(key => new HomepageSection { Key = key }).ToArray();
        Assert.Equal(all.Length, new StoreControlSettings { HomepageSections = all.Reverse().ToArray() }.Normalize().HomepageSections.Count);

        AssertBadRequest(() => new StoreControlSettings { HomepageSections = all.Skip(1).ToArray() }.Normalize());
        AssertBadRequest(() => new StoreControlSettings { HomepageSections = all.Skip(1).Append(all[1]).ToArray() }.Normalize());
        AssertBadRequest(() => new StoreControlSettings { HomepageSections = all.Append(all[0]).ToArray() }.Normalize());
    }

    [Fact]
    public void WhatsApp_template_rejects_unknown_placeholders_and_accepts_known_ones()
    {
        AssertBadRequest(() => new WhatsAppTemplateSettings { General = "Hello {foo}" }.Normalize());

        var allowed = string.Join(" ", WhatsAppPlaceholder.All);
        Assert.Equal(allowed, new WhatsAppTemplateSettings { Product = allowed }.Normalize().Product);
    }

    [Theory]
    [InlineData("inv", "INV")]
    [InlineData(" rigx ", "RIGX")]
    public void Invoice_prefix_is_upper_cased(string input, string expected) =>
        Assert.Equal(expected, new InvoiceSettings { NumberPrefix = input }.Normalize().NumberPrefix);

    [Theory]
    [InlineData("IN1")]
    [InlineData("I")]
    [InlineData("ABCDEFG")]
    public void Invoice_prefix_must_be_two_to_six_letters(string prefix) =>
        AssertBadRequest(() => new InvoiceSettings { NumberPrefix = prefix }.Normalize());

    [Fact]
    public void English_default_needs_english_enabled()
    {
        AssertBadRequest(() => new LanguageSettings { DefaultLanguage = "en", EnglishEnabled = false }.Normalize());
        AssertBadRequest(() => new LanguageSettings { DefaultLanguage = "fr" }.Normalize());
        Assert.Equal("ar", new LanguageSettings { EnglishEnabled = false }.Normalize().DefaultLanguage);
    }

    [Fact]
    public void Lead_sources_must_be_non_empty_and_unique_ignoring_case()
    {
        AssertBadRequest(() => new LeadSourceSettings { Sources = [] }.Normalize());
        AssertBadRequest(() => new LeadSourceSettings { Sources = ["Facebook", "facebook"] }.Normalize());
        Assert.Equal(["Facebook", "Google"], new LeadSourceSettings { Sources = [" Facebook ", "Google"] }.Normalize().Sources);
    }

    [Fact]
    public void Warranty_months_must_be_positive_when_given()
    {
        AssertBadRequest(() => new WarrantyOptionSettings { Options = [new() { Label = "x", Months = 0 }] }.Normalize());
        AssertBadRequest(() => new WarrantyOptionSettings { Options = [new() { Label = "x", Months = 121 }] }.Normalize());
        var normalized = new WarrantyOptionSettings { Options = [new() { Label = "الوكيل" }, new() { Label = "سنة", Months = 12 }] }.Normalize();
        Assert.Null(normalized.Options[0].Months);
        Assert.Equal(12, normalized.Options[1].Months);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(720)]
    public void Sales_reservation_hours_accept_the_range_ends(int hours) =>
        Assert.Equal(hours, new SalesSettings { ReservationHours = hours }.Normalize().ReservationHours);

    [Theory]
    [InlineData(0)]
    [InlineData(721)]
    public void Sales_reservation_hours_outside_the_range_are_rejected(int hours) =>
        AssertBadRequest(() => new SalesSettings { ReservationHours = hours }.Normalize());

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(12.5)]
    public void Pos_discount_limit_accepts_zero_to_one_hundred_percent(double percent) =>
        Assert.Equal((decimal)percent, new SalesSettings { PosMaxDiscountPercent = (decimal)percent }.Normalize().PosMaxDiscountPercent);

    [Fact]
    public void Pos_discount_limit_outside_zero_to_one_hundred_or_with_extra_decimals_is_rejected()
    {
        AssertBadRequest(() => new SalesSettings { PosMaxDiscountPercent = -1m }.Normalize());
        AssertBadRequest(() => new SalesSettings { PosMaxDiscountPercent = 100.01m }.Normalize());
        AssertBadRequest(() => new SalesSettings { PosMaxDiscountPercent = 10.005m }.Normalize());
    }

    [Fact]
    public void Sales_defaults_match_the_business_rules()
    {
        var sales = new SalesSettings().Normalize();
        Assert.Equal((24, false, 10m), (sales.ReservationHours, sales.ReservationAutoRelease, sales.PosMaxDiscountPercent));
    }
}

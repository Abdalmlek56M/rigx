using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class OrderRulesTests
{
    private static StoreOrder Order(string status = OrderStatus.New, string fulfillment = OrderFulfillment.Delivery,
        string payment = PaymentStatus.Unpaid, decimal total = 1000m, decimal paid = 0m, bool blacklisted = false) =>
        new()
        {
            Reference = "R", TrackingToken = "T", CustomerName = "C", PhoneNumber = "01000000000", Status = status,
            PaymentStatus = payment, FulfillmentMethod = fulfillment, Total = total, PaidAmount = paid, IsBlacklisted = blacklisted
        };

    private static void AssertBadRequest(Action action) => Assert.Equal(400, Assert.Throws<RetailException>(action).StatusCode);

    [Theory]
    [InlineData("0100 000 0000", "01000000000")]
    [InlineData("+20 (100) 000-0000", "201000000000")]
    [InlineData("١٢٣٤٥٦٧٨", "")]
    public void Phone_keeps_ascii_digits_only(string input, string expected)
    {
        if (expected.Length == 0) AssertBadRequest(() => OrderRules.Phone(input));
        else Assert.Equal(expected, OrderRules.Phone(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("1234567890123456")]
    [InlineData("phone")]
    public void Phone_must_have_between_eight_and_fifteen_digits(string? input) => AssertBadRequest(() => OrderRules.Phone(input));

    [Fact]
    public void Phone_accepts_the_length_boundaries()
    {
        Assert.Equal("12345678", OrderRules.Phone("12345678"));
        Assert.Equal("123456789012345", OrderRules.Phone("123456789012345"));
    }

    [Fact]
    public void Email_is_optional_lowercased_and_checked()
    {
        Assert.Null(OrderRules.Email(null));
        Assert.Null(OrderRules.Email("  "));
        Assert.Equal("a@b.com", OrderRules.Email(" A@B.com "));
        AssertBadRequest(() => OrderRules.Email("no-at-sign"));
        AssertBadRequest(() => OrderRules.Email("@b.com"));
        AssertBadRequest(() => OrderRules.Email("a@"));
        AssertBadRequest(() => OrderRules.Email("a b@c.com"));
    }

    [Fact]
    public void Fulfillment_is_delivery_or_pickup_only()
    {
        Assert.Equal("Delivery", OrderRules.Fulfillment(" Delivery "));
        Assert.Equal("Pickup", OrderRules.Fulfillment("Pickup"));
        AssertBadRequest(() => OrderRules.Fulfillment("pickup"));
        AssertBadRequest(() => OrderRules.Fulfillment(null));
    }

    [Fact]
    public void Delivery_address_needs_a_known_governorate_area_and_street()
    {
        var address = OrderRules.Address(new OrderAddress(" c ", " Nasr   City ", "Abbas St", " ", "  Near the mall "), OrderFulfillment.Delivery)!;
        Assert.Equal(("C", "Nasr City", "Abbas St", null, "Near the mall"),
            (address.Governorate, address.Area, address.Street, address.Building, address.Landmark));

        AssertBadRequest(() => OrderRules.Address(null, OrderFulfillment.Delivery));
        AssertBadRequest(() => OrderRules.Address(new OrderAddress("XX", "Area", "Street", null, null), OrderFulfillment.Delivery));
        AssertBadRequest(() => OrderRules.Address(new OrderAddress("C", " ", "Street", null, null), OrderFulfillment.Delivery));
        AssertBadRequest(() => OrderRules.Address(new OrderAddress("C", "Area", null, null, null), OrderFulfillment.Delivery));
    }

    [Fact]
    public void Pickup_keeps_no_address()
    {
        Assert.Null(OrderRules.Address(new OrderAddress("C", "Area", "Street", null, null), OrderFulfillment.Pickup));
        Assert.Null(OrderRules.Address(null, OrderFulfillment.Pickup));
    }

    [Fact]
    public void Address_key_ignores_case_spacing_and_punctuation()
    {
        Assert.Equal(OrderRules.AddressKey("C", "Nasr City", "Abbas St.", "Bldg 4"), OrderRules.AddressKey("C", "nasr  city", "abbas st", "BLDG-4"));
        Assert.Null(OrderRules.AddressKey(null, "a", "b", null));
        Assert.Null(OrderRules.AddressKey("C", "a", null, null));
    }

    [Theory]
    [InlineData("CashOnDelivery", "Online", true)]
    [InlineData("InstaPay", "Online", true)]
    [InlineData("Wallet", "Online", true)]
    [InlineData("Cash", "Online", false)]
    [InlineData("BankTransfer", "Online", false)]
    [InlineData("Split", "Online", false)]
    [InlineData("Cash", "Store", true)]
    [InlineData("InstaPay", "Store", true)]
    [InlineData("Wallet", "Store", true)]
    [InlineData("BankTransfer", "Store", true)]
    [InlineData("CashOnDelivery", "Store", false)]
    [InlineData("Split", "Store", false)]
    public void Payment_method_depends_on_the_channel(string method, string channel, bool allowed)
    {
        if (allowed) Assert.Equal(method, OrderRules.PaymentMethod(method, channel));
        else AssertBadRequest(() => OrderRules.PaymentMethod(method, channel));
    }

    public static TheoryData<string, string, string, bool> Moves => new()
    {
        { OrderStatus.New, OrderFulfillment.Delivery, OrderStatus.Confirmed, true },
        { OrderStatus.New, OrderFulfillment.Delivery, OrderStatus.Cancelled, true },
        { OrderStatus.New, OrderFulfillment.Delivery, OrderStatus.Preparing, false },
        { OrderStatus.New, OrderFulfillment.Delivery, OrderStatus.Delivered, false },
        { OrderStatus.Confirmed, OrderFulfillment.Delivery, OrderStatus.Preparing, true },
        { OrderStatus.Confirmed, OrderFulfillment.Delivery, OrderStatus.Cancelled, true },
        { OrderStatus.Confirmed, OrderFulfillment.Delivery, OrderStatus.Shipped, false },
        { OrderStatus.Confirmed, OrderFulfillment.Pickup, OrderStatus.Delivered, false },
        { OrderStatus.Preparing, OrderFulfillment.Delivery, OrderStatus.Shipped, true },
        { OrderStatus.Preparing, OrderFulfillment.Delivery, OrderStatus.Delivered, false },
        { OrderStatus.Preparing, OrderFulfillment.Pickup, OrderStatus.Delivered, true },
        { OrderStatus.Preparing, OrderFulfillment.Pickup, OrderStatus.Shipped, false },
        { OrderStatus.Preparing, OrderFulfillment.Pickup, OrderStatus.Cancelled, true },
        { OrderStatus.Shipped, OrderFulfillment.Delivery, OrderStatus.Delivered, true },
        { OrderStatus.Shipped, OrderFulfillment.Delivery, OrderStatus.Cancelled, true },
        { OrderStatus.Shipped, OrderFulfillment.Delivery, OrderStatus.Preparing, false },
        { OrderStatus.Delivered, OrderFulfillment.Delivery, OrderStatus.Cancelled, true },
        { OrderStatus.Delivered, OrderFulfillment.Delivery, OrderStatus.Completed, false },
        { OrderStatus.Completed, OrderFulfillment.Delivery, OrderStatus.Cancelled, false },
        { OrderStatus.Cancelled, OrderFulfillment.Delivery, OrderStatus.Confirmed, false }
    };

    [Theory]
    [MemberData(nameof(Moves))]
    public void Status_transitions_follow_the_workflow(string from, string fulfillment, string to, bool allowed) =>
        Assert.Equal(allowed, OrderRules.CanMove(Order(from, fulfillment), to));

    [Theory]
    [InlineData(OrderStatus.New, false)]
    [InlineData(OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Preparing, true)]
    [InlineData(OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void Only_confirmed_and_preparing_orders_hold_stock(string status, bool holds) =>
        Assert.Equal(holds, OrderRules.HoldsReservation(status));

    [Theory]
    [InlineData(0)]
    [InlineData(721)]
    public void Reservation_hours_outside_the_range_are_rejected(int hours) => AssertBadRequest(() => OrderRules.ValidateReservationHours(hours));

    [Theory]
    [InlineData(1)]
    [InlineData(720)]
    public void Reservation_hours_accept_the_range_ends(int hours) => OrderRules.ValidateReservationHours(hours);

    [Fact]
    public void A_blacklisted_order_needs_full_payment_before_it_is_confirmed()
    {
        OrderRules.EnsureCanConfirm(Order());
        OrderRules.EnsureCanConfirm(Order(blacklisted: true, payment: PaymentStatus.Paid));
        Assert.Equal(409, Assert.Throws<RetailException>(() => OrderRules.EnsureCanConfirm(Order(blacklisted: true))).StatusCode);
        Assert.Equal(409, Assert.Throws<RetailException>(() =>
            OrderRules.EnsureCanConfirm(Order(blacklisted: true, payment: PaymentStatus.PendingVerification, paid: 500m))).StatusCode);
    }

    [Fact]
    public void Remaining_is_what_is_left_to_pay_and_zero_once_paid_or_waived()
    {
        Assert.Equal(1000m, OrderRules.Remaining(Order()));
        Assert.Equal(400m, OrderRules.Remaining(Order(paid: 600m)));
        Assert.Equal(0m, OrderRules.Remaining(Order(paid: 1200m)));
        Assert.Equal(0m, OrderRules.Remaining(Order(payment: PaymentStatus.Paid, paid: 1000m)));
        Assert.Equal(0m, OrderRules.Remaining(Order(payment: PaymentStatus.Waived)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10.005)]
    public void A_payment_must_be_positive_with_two_decimals(double amount) =>
        AssertBadRequest(() => OrderRules.ValidatePayment((decimal)amount));

    [Fact]
    public void A_payment_accepts_two_decimals() => OrderRules.ValidatePayment(10.05m);

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rating_must_be_from_one_to_five_out_of_range(int score) => AssertBadRequest(() => OrderRules.ValidateRating(score));

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Rating_accepts_one_and_five(int score) => OrderRules.ValidateRating(score);

    [Fact]
    public void Discount_up_to_the_limit_is_allowed_and_above_it_is_forbidden()
    {
        OrderRules.EnsureDiscountAllowed(1000m, 0m, 0m, false);
        OrderRules.EnsureDiscountAllowed(1000m, 100m, 10m, false);
        Assert.Equal(403, Assert.Throws<RetailException>(() => OrderRules.EnsureDiscountAllowed(1000m, 100.01m, 10m, false)).StatusCode);
        Assert.Equal(403, Assert.Throws<RetailException>(() => OrderRules.EnsureDiscountAllowed(1000m, 1m, 0m, false)).StatusCode);
    }

    [Fact]
    public void Unlimited_discount_skips_the_limit()
    {
        OrderRules.EnsureDiscountAllowed(1000m, 900m, 10m, true);
        OrderRules.EnsureDiscountAllowed(0m, 5m, 0m, true);
    }

    [Fact]
    public void Tracking_tokens_are_url_safe_unique_and_fit_the_lookup_limit()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => OrderRules.NewTrackingToken()).ToList();
        Assert.Equal(50, tokens.Distinct().Count());
        Assert.All(tokens, token =>
        {
            Assert.InRange(token.Length, 20, 32);
            Assert.Matches("^[A-Za-z0-9_-]+$", token);
        });
    }

    [Fact]
    public void Usage_is_optional_and_other_needs_its_note()
    {
        Assert.Equal((null, null), OrderUsage.Parse(null, "ignored"));
        Assert.Equal((null, null), OrderUsage.Parse("  ", null));
        Assert.Equal(("Gaming", (string?)null), OrderUsage.Parse("Gaming", "dropped note"));
        Assert.Equal(("Upgrade", (string?)null), OrderUsage.Parse(" Upgrade ", null));
        Assert.Equal(("Other", (string?)"CAD work"), OrderUsage.Parse("Other", " CAD   work "));
        AssertBadRequest(() => OrderUsage.Parse("Other", null));
        AssertBadRequest(() => OrderUsage.Parse("Other", "   "));
        AssertBadRequest(() => OrderUsage.Parse("Cooking", null));
        AssertBadRequest(() => OrderUsage.Parse("Other", new string('x', 201)));
    }
}

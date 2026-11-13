using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class InvoiceRulesTests
{
    [Theory]
    [InlineData("INV", 2026, 1, "INV-2026-0001")]
    [InlineData("RGX", 2027, 42, "RGX-2027-0042")]
    [InlineData("INV", 2026, 12345, "INV-2026-12345")]
    public void FormatNumber_pads_the_sequence_to_a_minimum_width(string prefix, int year, int sequence, string expected) =>
        Assert.Equal(expected, InvoiceRules.FormatNumber(prefix, year, sequence));

    [Theory]
    [InlineData("INV-2026-0001", "INV-2026-0001")]
    [InlineData("inv-2026-0001", "INV-2026-0001")]
    [InlineData("  Inv-2026-0042 ", "INV-2026-0042")]
    [InlineData("RGX-2026-12345", "RGX-2026-12345")]
    public void TryParseNumber_accepts_typed_numbers_ignoring_case_and_spaces(string text, string expected)
    {
        Assert.True(InvoiceRules.TryParseNumber(text, out var number));
        Assert.Equal(expected, number);
    }

    [Theory]
    [InlineData("RIGX-20261113-AB12")]
    [InlineData("RIGX-20261113-0007")]
    [InlineData("RX-0001")]
    [InlineData("INV-26-1")]
    [InlineData("INV-2026-001")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParseNumber_rejects_order_references_codes_and_malformed_text(string? text) =>
        Assert.False(InvoiceRules.TryParseNumber(text, out _));

    private static StoreOrder Order(params OrderItem[] items) => new()
    {
        Reference = "RIGX-20261113-0001", TrackingToken = "t", CustomerName = "C", PhoneNumber = "01000000000",
        Status = OrderStatus.New, PaymentStatus = PaymentStatus.Unpaid, Items = items
    };

    [Fact]
    public void Kind_is_build_when_any_line_is_a_build_otherwise_parts()
    {
        Assert.Equal(InvoiceKind.Build, InvoiceRules.Kind(true));
        Assert.Equal(InvoiceKind.Parts, InvoiceRules.Kind(false));
        Assert.Equal(InvoiceKind.Build, InvoiceRules.Kind(Order(new OrderItem { Name = "Build", AssemblyId = Guid.NewGuid() }, new OrderItem { Name = "Part" })));
        Assert.Equal(InvoiceKind.Parts, InvoiceRules.Kind(Order(new OrderItem { Name = "Part" })));
    }

    [Theory]
    [InlineData(OrderStatus.Cancelled, InvoiceStatus.Void)]
    [InlineData(OrderStatus.Delivered, InvoiceStatus.Issued)]
    [InlineData(OrderStatus.Completed, InvoiceStatus.Issued)]
    public void Status_is_void_only_for_a_cancelled_order(string orderStatus, string expected) =>
        Assert.Equal(expected, InvoiceRules.Status(orderStatus));

    [Fact]
    public void YearOf_uses_the_utc_year()
    {
        Assert.Equal(2026, InvoiceRules.YearOf(new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero)));
        Assert.Equal(2026, InvoiceRules.YearOf(new DateTimeOffset(2027, 1, 1, 1, 0, 0, TimeSpan.FromHours(2))));
    }

    [Fact]
    public void Access_tokens_are_unique_and_url_safe()
    {
        var first = InvoiceRules.NewAccessToken();
        Assert.NotEqual(first, InvoiceRules.NewAccessToken());
        Assert.Matches("^[A-Za-z0-9_-]+$", first);
    }

    [Theory]
    [InlineData("INV-2026-0001", "INV-2026-0001")]
    [InlineData("inv-2026-0001", "INV-2026-0001")]
    public void ParseLookup_reads_an_invoice_number_first(string query, string value) =>
        Assert.Equal((WarrantyLookupKind.InvoiceNumber, value), WarrantyRules.ParseLookup(query));

    [Theory]
    [InlineData("RIGX-20261113-0007")]
    [InlineData("rigx-20261113-ab12")]
    public void ParseLookup_still_reads_an_order_reference(string query) =>
        Assert.Equal((WarrantyLookupKind.OrderReference, query.ToUpperInvariant()), WarrantyRules.ParseLookup(query));
}

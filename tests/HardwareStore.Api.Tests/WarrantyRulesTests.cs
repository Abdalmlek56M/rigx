using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class WarrantyRulesTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private static Warranty Warranty(int? months = 3, DateTimeOffset? voidedAt = null) => new()
    {
        Code = "RX-0001", Name = "GPU", Condition = "New", Label = "Store warranty", PhoneNumber = "01000000000",
        Months = months, StartsAtUtc = Start, EndsAtUtc = WarrantyRules.EndsAt(Start, months), VoidedAtUtc = voidedAt
    };

    [Theory]
    [InlineData(null, null)]
    [InlineData(1, "2026-12-01T10:00:00Z")]
    [InlineData(3, "2027-02-01T10:00:00Z")]
    [InlineData(12, "2027-11-01T10:00:00Z")]
    public void EndsAt_adds_months_and_stays_open_for_manufacturer_warranty(int? months, string? expected) =>
        Assert.Equal(expected is null ? null : DateTimeOffset.Parse(expected), WarrantyRules.EndsAt(Start, months));

    [Fact]
    public void EndsAt_clamps_to_the_last_day_of_a_shorter_month() =>
        Assert.Equal(new DateTimeOffset(2027, 2, 28, 0, 0, 0, TimeSpan.Zero),
            WarrantyRules.EndsAt(new DateTimeOffset(2027, 1, 31, 0, 0, 0, TimeSpan.Zero), 1));

    [Fact]
    public void Status_is_active_until_the_end_date_then_expired()
    {
        var warranty = Warranty();
        Assert.Equal(WarrantyStatus.Active, WarrantyRules.Status(warranty, Start.AddDays(10)));
        Assert.Equal(WarrantyStatus.Active, WarrantyRules.Status(warranty, warranty.EndsAtUtc!.Value.AddSeconds(-1)));
        Assert.Equal(WarrantyStatus.Expired, WarrantyRules.Status(warranty, warranty.EndsAtUtc.Value));
    }

    [Fact]
    public void Status_of_a_manufacturer_warranty_never_expires_and_a_voided_one_is_void()
    {
        Assert.Equal(WarrantyStatus.Active, WarrantyRules.Status(Warranty(null), Start.AddYears(20)));
        Assert.Equal(WarrantyStatus.Void, WarrantyRules.Status(Warranty(voidedAt: Start), Start.AddDays(1)));
    }

    [Fact]
    public void DaysLeft_rounds_up_never_goes_negative_and_is_empty_without_an_end_or_when_void()
    {
        var warranty = Warranty(1);
        Assert.Equal(30, WarrantyRules.DaysLeft(warranty, Start));
        Assert.Equal(1, WarrantyRules.DaysLeft(warranty, warranty.EndsAtUtc!.Value.AddHours(-1)));
        Assert.Equal(0, WarrantyRules.DaysLeft(warranty, warranty.EndsAtUtc.Value.AddDays(5)));
        Assert.Null(WarrantyRules.DaysLeft(Warranty(null), Start));
        Assert.Null(WarrantyRules.DaysLeft(Warranty(voidedAt: Start), Start));
    }

    [Theory]
    [InlineData("01289186840", "********840")]
    [InlineData("12345678", "*****678")]
    [InlineData("123", "123")]
    public void MaskPhone_keeps_only_the_last_three_digits(string digits, string expected) =>
        Assert.Equal(expected, WarrantyRules.MaskPhone(digits));

    [Theory]
    [InlineData("RX-0142", WarrantyLookupKind.Code, "RX-0142")]
    [InlineData("  rx-0142 ", WarrantyLookupKind.Code, "RX-0142")]
    [InlineData("rigx-20261113-0007", WarrantyLookupKind.OrderReference, "RIGX-20261113-0007")]
    [InlineData("+20 128 918 6840", WarrantyLookupKind.Phone, "01289186840")]
    [InlineData("0128-918-6840", WarrantyLookupKind.Phone, "01289186840")]
    [InlineData("SN-abc123", WarrantyLookupKind.Serial, "SN-abc123")]
    public void ParseLookup_tells_code_order_phone_and_serial_apart(string query, WarrantyLookupKind kind, string value) =>
        Assert.Equal((kind, value), WarrantyRules.ParseLookup(query));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1234567")]
    public void ParseLookup_rejects_empty_short_serial_and_short_phone(string? query) =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => WarrantyRules.ParseLookup(query)).StatusCode);

    [Fact]
    public void ParseLookup_rejects_text_over_64_characters() =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => WarrantyRules.ParseLookup(new string('A', 65))).StatusCode);

    [Fact]
    public void Void_records_who_when_and_why_and_a_second_void_conflicts()
    {
        var warranty = Warranty();
        var actor = Guid.NewGuid();
        WarrantyRules.Void(warranty, "  Sticker removed ", actor, Start.AddDays(1));
        Assert.Equal(("Sticker removed", actor, Start.AddDays(1), 2L), (warranty.VoidReason, warranty.VoidedByUserAccountId, warranty.VoidedAtUtc, warranty.RowVersion));
        Assert.Equal(409, Assert.Throws<RetailException>(() => WarrantyRules.Void(warranty, "Again", actor, Start)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Void_needs_a_reason(string? reason) =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => WarrantyRules.Void(Warranty(), reason, null, Start)).StatusCode);

    [Fact]
    public void Extend_moves_the_end_date_records_the_history_and_bumps_the_version()
    {
        var warranty = Warranty();
        var actor = Guid.NewGuid();

        var extension = WarrantyRules.Extend(warranty, 6, "  Goodwill  ", actor, Start.AddDays(10));

        Assert.Equal(Start.AddMonths(9), warranty.EndsAtUtc);
        Assert.Equal(2L, warranty.RowVersion);
        Assert.Equal((warranty.Id, 6, Start.AddMonths(3), Start.AddMonths(9), "Goodwill", actor, Start.AddDays(10)),
            (extension.WarrantyId, extension.Months, extension.PreviousEndUtc, extension.NewEndUtc, extension.Reason, extension.ActorUserAccountId, extension.CreatedAtUtc));
    }

    [Fact]
    public void Extend_refuses_a_void_warranty_and_a_manufacturer_warranty_with_a_conflict()
    {
        Assert.Equal(409, Assert.Throws<RetailException>(() => WarrantyRules.Extend(Warranty(voidedAt: Start), 3, "Why", Guid.NewGuid(), Start)).StatusCode);
        Assert.Equal(409, Assert.Throws<RetailException>(() => WarrantyRules.Extend(Warranty(months: null), 3, "Why", Guid.NewGuid(), Start)).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(37)]
    public void Extend_needs_one_to_thirty_six_months(int months) =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => WarrantyRules.Extend(Warranty(), months, "Why", Guid.NewGuid(), Start)).StatusCode);

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Extend_needs_a_reason_of_at_most_500_characters(string? reason)
    {
        Assert.Equal(400, Assert.Throws<RetailException>(() => WarrantyRules.Extend(Warranty(), 3, reason, Guid.NewGuid(), Start)).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => WarrantyRules.Extend(Warranty(), 3, new string('x', 501), Guid.NewGuid(), Start)).StatusCode);
    }

    [Fact]
    public void Extend_must_end_after_now_and_changes_nothing_when_refused()
    {
        var warranty = Warranty();

        Assert.Equal(400, Assert.Throws<RetailException>(() => WarrantyRules.Extend(warranty, 12, "Late", Guid.NewGuid(), Start.AddMonths(40))).StatusCode);
        Assert.Equal((Start.AddMonths(3), 1L), (warranty.EndsAtUtc, warranty.RowVersion));
        Assert.Equal(Start.AddMonths(15), WarrantyRules.Extend(warranty, 12, "In time", Guid.NewGuid(), Start.AddMonths(4)).NewEndUtc);
    }
}

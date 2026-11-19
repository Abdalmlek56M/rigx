using HardwareStore.Modules.Identity;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class StaffCustomerLinkRulesTests
{
    [Theory]
    [InlineData("01012345678")]
    [InlineData("+20 101 234 5678")]
    [InlineData("00201012345678")]
    public void Phones_in_any_format_share_the_last_ten_digits(string phone) =>
        Assert.Equal("1012345678", StaffCustomerLink.PhoneKey(phone));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123456789")]
    [InlineData("abc")]
    public void Short_or_missing_phones_have_no_key(string? phone) => Assert.Null(StaffCustomerLink.PhoneKey(phone));

    [Fact]
    public void Email_keys_are_trimmed_lower_case_distinct_and_non_empty() =>
        Assert.Equal(["a@x.com", "b@x.com"], StaffCustomerLink.EmailKeys(" A@X.com ", "a@x.com", null, "  ", "B@x.com"));

    [Fact]
    public void Accounts_match_on_a_shared_email_ignoring_case_and_spaces()
    {
        var staff = new AccountContact(StaffCustomerLink.EmailKeys("Ali@Shop.test"), null);
        var customer = new AccountContact(StaffCustomerLink.EmailKeys("other@x.test", " ali@shop.TEST "), null);
        Assert.True(StaffCustomerLink.Matches(staff, customer));
    }

    [Fact]
    public void Accounts_match_on_the_same_phone_in_different_formats()
    {
        var staff = new AccountContact([], "01012345678");
        var customer = new AccountContact([], "+20 101 234 5678");
        Assert.True(StaffCustomerLink.Matches(staff, customer));
    }

    [Fact]
    public void Accounts_with_nothing_shared_or_only_short_phones_do_not_match()
    {
        Assert.False(StaffCustomerLink.Matches(new AccountContact(["a@x.test"], "01012345678"), new AccountContact(["b@x.test"], "01112345678")));
        Assert.False(StaffCustomerLink.Matches(new AccountContact([], "12345"), new AccountContact([], "12345")));
    }
}

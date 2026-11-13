using System.Security.Cryptography;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class IdentityAccountLifecycleUnitTests
{
    private readonly PasswordPolicyValidator _policy = new();

    [Fact]
    public void Password_policy_counts_unicode_scalars_without_trimming_or_normalizing()
    {
        Assert.Equal(PasswordPolicyFailure.TooShort, _policy.Validate(new string('a', 14)));
        Assert.Equal(PasswordPolicyFailure.None, _policy.Validate(new string('a', 14) + " "));
        Assert.Equal(PasswordPolicyFailure.None, _policy.Validate(new string('a', 128)));
        Assert.Equal(PasswordPolicyFailure.TooLong, _policy.Validate(new string('a', 129)));
        Assert.Equal(PasswordPolicyFailure.None, _policy.Validate(string.Concat(Enumerable.Repeat("😀", 128))));
        Assert.Equal(PasswordPolicyFailure.TooLong, _policy.Validate(string.Concat(Enumerable.Repeat("😀", 129))));
    }

    [Fact]
    public void Password_policy_rejects_local_common_password_without_external_lookup()
    {
        Assert.Equal(PasswordPolicyFailure.Common, _policy.Validate("passwordpassword"));
    }

    [Fact]
    public void Raw_identity_token_hashing_accepts_only_32_random_bytes()
    {
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = WebEncoders.Base64UrlEncode(rawBytes);

        Assert.True(IdentityTokenFactory.TryHashRawToken(rawToken, out var hash));
        Assert.Equal(SHA256.HashData(rawBytes), hash);
        Assert.False(IdentityTokenFactory.TryHashRawToken(
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(31)), out _));
        Assert.False(IdentityTokenFactory.TryHashRawToken("not-a-token", out _));
    }
}

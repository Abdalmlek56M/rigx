using System.Buffers.Binary;
using System.Diagnostics;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace HardwareStore.Api.Tests;

public sealed class PasswordHashingTests(ITestOutputHelper output)
{
    private const int Iterations = 220_000;

    [Fact]
    public void Hash_uses_identity_v3_pbkdf2_sha512_with_220000_iterations_and_unique_salt()
    {
        var hashing = CreateHashingService(Iterations);
        var account = CreateAccount();
        const string password = "a sufficiently long test password";

        var firstHash = hashing.HashPassword(account, password);
        var secondHash = hashing.HashPassword(account, password);
        var bytes = Convert.FromBase64String(firstHash);

        Assert.Equal(0x01, bytes[0]);
        Assert.Equal((uint)KeyDerivationPrf.HMACSHA512, BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1, 4)));
        Assert.Equal(Iterations, checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4))));
        Assert.NotEqual(firstHash, secondHash);
        Assert.DoesNotContain(password, firstHash, StringComparison.Ordinal);
        Assert.Equal(PasswordHashVerification.Succeeded, hashing.VerifyPassword(account, firstHash, password));
        Assert.Equal(PasswordHashVerification.Failed, hashing.VerifyPassword(account, firstHash, "wrong password"));
        Assert.Equal(PasswordHashVerification.Failed, hashing.VerifyPassword(account, "not-a-hash", password));
    }

    [Fact]
    public void Old_identity_v3_hash_requests_rehash()
    {
        var oldHasher = new PasswordHasher<UserAccount>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = 100_000
        }));
        var account = CreateAccount();
        var oldHash = oldHasher.HashPassword(account, "correct password");

        Assert.Equal(PasswordHashVerification.SucceededNeedsRehash,
            CreateHashingService(Iterations).VerifyPassword(account, oldHash, "correct password"));
    }

    [Fact]
    public void Missing_password_hash_is_a_failed_verification_not_an_exception()
    {
        var hashing = CreateHashingService(Iterations);
        Assert.Equal(PasswordHashVerification.Failed,
            hashing.VerifyPassword(CreateAccount(), null, "unverified-account-password"));
    }

    [Fact]
    public void Login_identifier_normalization_preserves_plus_tags_and_dots()
    {
        Assert.Equal("USER+TAG.FOO@EXAMPLE.COM", IdentityLoginService.NormalizeIdentifier(" User+tag.foo@example.com "));
        Assert.NotEqual(IdentityLoginService.NormalizeIdentifier("user+tag@example.com"),
            IdentityLoginService.NormalizeIdentifier("user@example.com"));
        Assert.NotEqual(IdentityLoginService.NormalizeIdentifier("first.last@example.com"),
            IdentityLoginService.NormalizeIdentifier("firstlast@example.com"));
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void Verify_benchmark_reports_local_cost_for_220000_iterations()
    {
        var hashing = CreateHashingService(Iterations);
        var account = CreateAccount();
        var hash = hashing.HashPassword(account, "benchmark-only-password");
        _ = hashing.VerifyPassword(account, hash, "benchmark-only-password");

        var samples = new double[12];
        for (var index = 0; index < samples.Length; index++)
        {
            var timer = Stopwatch.StartNew();
            Assert.Equal(PasswordHashVerification.Succeeded,
                hashing.VerifyPassword(account, hash, "benchmark-only-password"));
            timer.Stop();
            samples[index] = timer.Elapsed.TotalMilliseconds;
        }

        Array.Sort(samples);
        output.WriteLine($"PasswordHasher IdentityV3 PBKDF2-HMAC-SHA512 {Iterations:N0}: median={Median(samples):F2} ms, p95={samples[(int)Math.Ceiling(samples.Length * .95) - 1]:F2} ms, n={samples.Length}.");
    }

    private static PasswordHashingService CreateHashingService(int iterations) =>
        new(new PasswordHasher<UserAccount>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = iterations
        })));

    private static UserAccount CreateAccount() => new()
    {
        Id = Guid.NewGuid(),
        PersonId = Guid.NewGuid(),
        Username = "login-test",
        NormalizedUsername = "LOGIN-TEST",
        Email = "login-test@example.test",
        NormalizedEmail = "LOGIN-TEST@EXAMPLE.TEST",
        PasswordHash = ""
    };

    private static double Median(double[] samples) => (samples[5] + samples[6]) / 2;
}

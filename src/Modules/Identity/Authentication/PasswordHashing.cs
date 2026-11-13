using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HardwareStore.Modules.Identity.Persistence;

namespace HardwareStore.Modules.Identity.Authentication;

public enum PasswordHashVerification
{
    Failed,
    Succeeded,
    SucceededNeedsRehash
}

public interface IPasswordHashingService
{
    string HashPassword(UserAccount account, string password);
    PasswordHashVerification VerifyPassword(UserAccount account, string? passwordHash, string password);
    bool VerifyDummy(string password);
}

public sealed class PasswordHashingService : IPasswordHashingService
{
    private readonly IPasswordHasher<UserAccount> _hasher;
    private readonly UserAccount _dummyAccount;
    private readonly string _dummyHash;

    public PasswordHashingService(IPasswordHasher<UserAccount> hasher)
    {
        _hasher = hasher;
        _dummyAccount = new UserAccount
        {
            Id = Guid.Empty,
            PersonId = Guid.Empty,
            Username = "dummy",
            NormalizedUsername = "DUMMY",
            Email = "dummy@example.invalid",
            NormalizedEmail = "DUMMY@EXAMPLE.INVALID",
            PasswordHash = string.Empty
        };
        _dummyHash = _hasher.HashPassword(_dummyAccount,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public string HashPassword(UserAccount account, string password) => _hasher.HashPassword(account, password);

    public PasswordHashVerification VerifyPassword(UserAccount account, string? passwordHash, string password) =>
        Verify(account, passwordHash, password);

    public bool VerifyDummy(string password) => Verify(_dummyAccount, _dummyHash, password) != PasswordHashVerification.Failed;

    private PasswordHashVerification Verify(UserAccount account, string? passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash))
            return PasswordHashVerification.Failed;

        try
        {
            return _hasher.VerifyHashedPassword(account, passwordHash, password) switch
            {
                PasswordVerificationResult.Success => PasswordHashVerification.Succeeded,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordHashVerification.SucceededNeedsRehash,
                _ => PasswordHashVerification.Failed
            };
        }
        catch (FormatException)
        {
            return PasswordHashVerification.Failed;
        }
        catch (ArgumentException)
        {
            return PasswordHashVerification.Failed;
        }
    }
}

public static class PasswordHashingRegistration
{
    public static IServiceCollection AddIdentityPasswordHashing(this IServiceCollection services)
    {
        services.Configure<PasswordHasherOptions>(options =>
        {
            options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
            options.IterationCount = 220_000;
        });
        services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
        return services;
    }
}

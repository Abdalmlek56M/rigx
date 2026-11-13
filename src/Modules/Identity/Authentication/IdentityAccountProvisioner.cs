using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

public sealed record ProvisionedAccount(Guid AccountId, bool IsNew);

/// <summary>
/// The single place that turns a proven email address (code or Google) into an account:
/// an existing account is marked verified, an unknown address becomes a new customer.
/// </summary>
public sealed class IdentityAccountProvisioner(TimeProvider timeProvider, TimeZoneInfo businessTimeZone)
{
    /// <summary>Seeded in the first Identity migration.</summary>
    public static readonly Guid CustomerRoleId = Guid.Parse("11111111-1111-4111-8111-111111111111");

    public async Task<ProvisionedAccount> FindOrCreateVerifiedAsync(
        IdentityDbContext db,
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityLoginService.NormalizeIdentifier(email);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var account = await db.UserAccounts
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);
        var isNew = account is null;
        if (account is null)
            account = await CreateCustomerAsync(db, email, normalizedEmail, cancellationToken);
        else if (!account.IsEmailVerified)
            account.IsEmailVerified = true;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ProvisionedAccount(account.Id, isNew);
    }

    private async Task<UserAccount> CreateCustomerAsync(
        IdentityDbContext db,
        string email,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            FullName = email.Split('@')[0],
            Email = email,
            IsCustomer = true,
            IsActive = true,
            CreatedAtUtc = now
        };
        await new PersonCodeGenerator(db, timeProvider, businessTimeZone)
            .CreatePersonInCurrentTransactionAsync(person, cancellationToken);

        var accountId = Guid.CreateVersion7();
        var username = await ChooseUsernameAsync(db, email, accountId, cancellationToken);
        var account = new UserAccount
        {
            Id = accountId,
            PersonId = person.Id,
            Person = person,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = email,
            NormalizedEmail = normalizedEmail,
            IsEmailVerified = true,
            IsActive = true,
            CreatedAtUtc = now
        };
        db.UserAccounts.Add(account);
        db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = CustomerRoleId });
        return account;
    }

    /// <summary>The username is internal (admin lists); it defaults to the email, or a stable id if taken/too long.</summary>
    private static async Task<string> ChooseUsernameAsync(
        IdentityDbContext db,
        string email,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var normalized = IdentityLoginService.NormalizeIdentifier(email);
        var free = email.Length <= 128 &&
                   !await db.UserAccounts.AnyAsync(item => item.NormalizedUsername == normalized, cancellationToken);
        return free ? email : $"user-{accountId:N}";
    }
}

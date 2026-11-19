using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity;

/// <summary>One account on the other side of a staff ↔ customer link.</summary>
public sealed record LinkedAccount(Guid Id, string PersonCode, string Name);

/// <summary>The contact details of one account that the link compares.</summary>
public sealed record AccountContact(IReadOnlyCollection<string> Emails, string? Phone);

/// <summary>
/// A staff account and a customer account are the same person when they share an email (account or person email, letter case
/// ignored) or a phone number. Phones are compared by their last ten digits, so 010…, +2010… and 002010… all match.
/// </summary>
public static class StaffCustomerLink
{
    public const int PhoneKeyLength = 10;

    public static string? PhoneKey(string? phone)
    {
        var digits = string.Concat((phone ?? string.Empty).Where(char.IsAsciiDigit));
        return digits.Length < PhoneKeyLength ? null : digits[^PhoneKeyLength..];
    }

    public static IReadOnlyCollection<string> EmailKeys(params string?[] emails) =>
        emails.Where(email => !string.IsNullOrWhiteSpace(email)).Select(email => email!.Trim().ToLowerInvariant()).Distinct().ToArray();

    public static bool Matches(AccountContact left, AccountContact right)
    {
        if (left.Emails.Intersect(right.Emails, StringComparer.Ordinal).Any()) return true;
        var phone = PhoneKey(left.Phone);
        return phone is not null && phone == PhoneKey(right.Phone);
    }
}

/// <summary>Finds the customer record of a staff member and the staff account of a customer (no stored link; it follows the contact details).</summary>
public sealed class StaffCustomerLinkService(IDbContextFactory<IdentityDbContext> contextFactory)
{
    public Task<IReadOnlyList<LinkedAccount>> CustomersOfStaffAsync(Guid staffId, CancellationToken cancellationToken = default) =>
        FindAsync(staffId, accounts => accounts.Customers().Where(account => account.MergedIntoAccountId == null), cancellationToken);

    public Task<IReadOnlyList<LinkedAccount>> StaffOfCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        FindAsync(customerId, accounts => accounts.Where(account => account.UserRoles.Any(role => role.Role.Code != CustomerAccounts.RoleCode)),
            cancellationToken);

    private async Task<IReadOnlyList<LinkedAccount>> FindAsync(Guid id, Func<IQueryable<UserAccount>, IQueryable<UserAccount>> otherSide,
        CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var subject = await db.UserAccounts.AsNoTracking().Where(account => account.Id == id)
            .Select(account => new { account.Email, PersonEmail = account.Person.Email, account.Person.PhoneNumber })
            .SingleOrDefaultAsync(cancellationToken);
        if (subject is null) return [];
        var contact = new AccountContact(StaffCustomerLink.EmailKeys(subject.Email, subject.PersonEmail), subject.PhoneNumber);
        var emails = contact.Emails.ToArray();
        var phone = StaffCustomerLink.PhoneKey(subject.PhoneNumber);
        var hasPhone = phone is not null;
        phone ??= string.Empty;
        var candidates = await otherSide(db.UserAccounts.AsNoTracking())
            .Where(account => account.Id != id && (emails.Contains(account.Email.ToLower()) ||
                (account.Person.Email != null && emails.Contains(account.Person.Email.Trim().ToLower())) ||
                (hasPhone && account.Person.PhoneNumber != null &&
                 account.Person.PhoneNumber.Replace(" ", "").Replace("-", "").EndsWith(phone))))
            .OrderBy(account => account.CreatedAtUtc)
            .Select(account => new { account.Id, account.Person.PersonCode, account.Person.FullName, account.Email, PersonEmail = account.Person.Email,
                account.Person.PhoneNumber })
            .Take(50)
            .ToListAsync(cancellationToken);
        return candidates
            .Where(row => StaffCustomerLink.Matches(contact, new AccountContact(StaffCustomerLink.EmailKeys(row.Email, row.PersonEmail), row.PhoneNumber)))
            .Select(row => new LinkedAccount(row.Id, row.PersonCode, row.FullName))
            .ToList();
    }
}

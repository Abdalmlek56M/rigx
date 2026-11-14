using HardwareStore.Common.Domain;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity;

/// <summary>
/// Customer administration for staff: the customer list, the account part of the 360 view and the support actions
/// (note, sign-in code, Google unlink, sign out everywhere). Every action is audited with the staff member as actor.
/// </summary>
public sealed class CustomerAdminService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    ISecurityAuditWriter auditWriter,
    EmailCodeSignInService emailCodes,
    GoogleSignInService google,
    IdentitySessionService sessions,
    TimeProvider timeProvider,
    TimeZoneInfo businessZone)
{
    public const int PageSize = 25;

    /// <summary>Customer accounts (not merged ones), newest first; the search matches name, email, phone digits or person code.</summary>
    public async Task<CustomerListPage> ListAsync(string? search, int page, CancellationToken cancellationToken = default)
    {
        var number = Math.Max(1, page);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = db.UserAccounts.AsNoTracking().Customers().Where(account => account.MergedIntoAccountId == null);
        if (!string.IsNullOrWhiteSpace(search)) accounts = Matching(accounts, search.Trim());
        var total = await accounts.CountAsync(cancellationToken);
        var items = await accounts.OrderByDescending(account => account.CreatedAtUtc).ThenByDescending(account => account.Id)
            .Skip((number - 1) * PageSize).Take(PageSize)
            .Select(account => new CustomerListRow(account.Id, account.Person.PersonCode, account.Person.FullName, account.Email,
                account.Person.PhoneNumber, account.IsActive, account.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new CustomerListPage(items, number, PageSize, total);
    }

    /// <summary>
    /// Customer accounts (not merged ones) registered in the range, oldest first, for the customer export.
    /// Reads at most <see cref="ExportRules.ReadLimit"/> rows.
    /// </summary>
    public async Task<IReadOnlyList<CustomerListRow>> ListForExportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = BusinessDay.Range(from, to, businessZone);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking().Customers()
            .Where(account => account.MergedIntoAccountId == null && account.CreatedAtUtc >= fromUtc && account.CreatedAtUtc < toUtc)
            .OrderBy(account => account.CreatedAtUtc).ThenBy(account => account.Id).Take(ExportRules.ReadLimit)
            .Select(account => new CustomerListRow(account.Id, account.Person.PersonCode, account.Person.FullName, account.Email,
                account.Person.PhoneNumber, account.IsActive, account.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerAccountView> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.UserAccounts.AsNoTracking().Customers().Where(account => account.Id == id)
            .Select(account => new
            {
                account.Id, account.Person.PersonCode, account.Person.FullName, account.Email, account.Person.PhoneNumber,
                account.IsActive, account.IsEmailVerified, account.CreatedAtUtc, account.Person.CustomerNote,
                account.MergedIntoAccountId,
                DefaultGovernorate = db.CustomerAddresses.Where(address => address.UserAccountId == account.Id && address.IsDefault && !address.IsDeleted)
                    .Select(address => address.Governorate).FirstOrDefault(),
                ActiveSessions = db.AuthSessions.Count(session => session.UserAccountId == account.Id && session.RevokedAtUtc == null &&
                    session.IdleExpiresAtUtc > now && session.AbsoluteExpiresAtUtc > now)
            })
            .SingleOrDefaultAsync(cancellationToken) ?? throw NotFound();
        return new CustomerAccountView(row.Id, row.PersonCode, row.FullName, row.Email, row.PhoneNumber, row.IsActive, row.IsEmailVerified,
            row.CreatedAtUtc, row.CustomerNote, row.DefaultGovernorate, row.ActiveSessions, row.MergedIntoAccountId);
    }

    public async Task SetNoteAsync(Guid id, string? note, Guid actorId, CancellationToken cancellationToken = default)
    {
        var text = note?.Trim();
        if (string.IsNullOrEmpty(text)) text = null;
        if (text is { Length: > CustomerAccounts.MaxNoteLength })
            throw new IdentityException(400, $"The note must be at most {CustomerAccounts.MaxNoteLength} characters.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var person = await db.UserAccounts.Customers().Where(account => account.Id == id).Select(account => account.Person)
            .SingleOrDefaultAsync(cancellationToken) ?? throw NotFound();
        person.CustomerNote = text;
        await db.SaveChangesAsync(cancellationToken);
        await AuditAsync("Customer.NoteChanged", actorId, id, cancellationToken);
    }

    /// <summary>Sends the customer a normal sign-in code by email (the usual limits apply); the code itself is never returned.</summary>
    public async Task<EmailCodeTicket> SendLoginCodeAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.UserAccounts.AsNoTracking().Customers().Where(item => item.Id == id)
            .Select(item => new { item.Email, item.IsActive }).SingleOrDefaultAsync(cancellationToken) ?? throw NotFound();
        if (!account.IsActive) throw new IdentityException(409, "The account is not active.");
        var ticket = await emailCodes.RequestAsync(account.Email, cancellationToken);
        await AuditAsync("Customer.LoginCodeSent", actorId, id, cancellationToken);
        return ticket;
    }

    public async Task UnlinkGoogleAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        await RequireCustomerAsync(id, cancellationToken);
        await google.UnlinkAsync(id, actorId, cancellationToken);
    }

    public async Task SignOutAllAsync(Guid id, Guid actorId, string correlationId, CancellationToken cancellationToken = default)
    {
        await RequireCustomerAsync(id, cancellationToken);
        await sessions.RevokeAllByAdminAsync(id, actorId, correlationId, cancellationToken);
    }

    public Task RecordViewedAsAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default) =>
        AuditAsync("Customer.ViewedAs", actorId, id, cancellationToken);

    private async Task RequireCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.UserAccounts.AsNoTracking().Customers().AnyAsync(account => account.Id == id, cancellationToken))
            throw NotFound();
    }

    private static IQueryable<UserAccount> Matching(IQueryable<UserAccount> accounts, string search)
    {
        var pattern = "%" + search.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
        var digits = string.Concat(search.Where(char.IsAsciiDigit));
        return accounts.Where(account => EF.Functions.ILike(account.Person.FullName, pattern) ||
            EF.Functions.ILike(account.Email, pattern) || EF.Functions.ILike(account.Person.PersonCode, pattern) ||
            (digits.Length >= 3 && account.Person.PhoneNumber != null &&
             account.Person.PhoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "").Contains(digits)));
    }

    private Task AuditAsync(string eventType, Guid actorId, Guid customerId, CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(new SecurityAuditEntry(eventType, "Succeeded", "User",
            ActorUserAccountId: actorId, SubjectUserAccountId: customerId), cancellationToken);

    private static IdentityException NotFound() => new(404, "Customer was not found.");
}

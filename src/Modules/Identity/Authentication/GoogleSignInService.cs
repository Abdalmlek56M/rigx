using System.Security.Claims;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

public sealed record GoogleSignInResult(
    bool Succeeded,
    Guid? UserAccountId,
    ClaimsPrincipal? Principal,
    bool IsNewAccount,
    string AuditReason);

/// <summary>How the signed-in user can sign in (X-Profile "login methods").</summary>
public sealed record LoginMethodsView(string Email, bool EmailCode, string? GoogleEmail);

/// <summary>
/// Google sign-in and linking — the same flow for customers and staff. A Google account is matched
/// by its stable subject; on first use it is linked to the account with the same, Google-verified
/// email (an invited staff member, or an existing customer), or a new customer account is created.
/// </summary>
public sealed class GoogleSignInService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IGoogleIdTokenValidator validator,
    IdentityAccountProvisioner provisioner,
    IdentityLoginService loginService,
    ISecurityAuditWriter auditWriter,
    TimeProvider timeProvider)
{
    public const string Provider = "Google";

    public async Task<GoogleSignInResult> SignInAsync(string? idToken, CancellationToken cancellationToken = default)
    {
        var google = await validator.ValidateAsync(idToken, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var linkedAccountId = await db.ExternalLogins.AsNoTracking()
            .Where(link => link.Provider == Provider && link.Subject == google.Subject)
            .Select(link => (Guid?)link.UserAccountId)
            .SingleOrDefaultAsync(cancellationToken);

        var isNew = false;
        if (linkedAccountId is null)
        {
            if (!google.EmailVerified)
                return new GoogleSignInResult(false, null, null, false, "GoogleEmailNotVerified");
            var account = await provisioner.FindOrCreateVerifiedAsync(db, google.Email, cancellationToken);
            isNew = account.IsNew;
            if (await db.ExternalLogins.AnyAsync(
                    link => link.UserAccountId == account.AccountId && link.Provider == Provider, cancellationToken))
                return new GoogleSignInResult(false, account.AccountId, null, false, "OtherGoogleAccountLinked");
            await AddLinkAsync(db, account.AccountId, google, cancellationToken);
            linkedAccountId = account.AccountId;
        }

        var login = await loginService.CreateVerifiedSessionAsync(linkedAccountId.Value, cancellationToken);
        return login.Status == IdentityLoginStatus.Succeeded
            ? new GoogleSignInResult(true, linkedAccountId, login.Principal, isNew, "Google")
            : new GoogleSignInResult(false, linkedAccountId, null, false, login.AuditReason);
    }

    /// <summary>Links a Google account to the signed-in account (any Google email may be linked).</summary>
    public async Task LinkAsync(Guid accountId, string? idToken, CancellationToken cancellationToken = default)
    {
        var google = await validator.ValidateAsync(idToken, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.ExternalLogins.AsNoTracking()
            .Where(link => (link.Provider == Provider && link.Subject == google.Subject) ||
                           (link.Provider == Provider && link.UserAccountId == accountId))
            .ToListAsync(cancellationToken);
        if (existing.Any(link => link.Subject == google.Subject && link.UserAccountId == accountId))
            return;
        if (existing.Any(link => link.Subject == google.Subject))
            throw new IdentityException(409, "This Google account is already linked to another RIGX account.");
        if (existing.Count > 0)
            throw new IdentityException(409, "Unlink the current Google account first.");

        await AddLinkAsync(db, accountId, google, cancellationToken);
        await AuditAsync("Identity.Google.Linked", accountId, accountId, cancellationToken);
    }

    /// <summary>Removes the Google link; the account can still sign in with an email code.</summary>
    public async Task UnlinkAsync(Guid accountId, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var removed = await db.ExternalLogins
            .Where(link => link.UserAccountId == accountId && link.Provider == Provider)
            .ExecuteDeleteAsync(cancellationToken);
        if (removed == 0)
            throw new IdentityException(404, "No Google account is linked.");
        await AuditAsync("Identity.Google.Unlinked", actorId, accountId, cancellationToken);
    }

    public async Task<LoginMethodsView> GetLoginMethodsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var email = await db.UserAccounts.AsNoTracking()
            .Where(account => account.Id == accountId)
            .Select(account => account.Email)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new IdentityException(401, "The account is not active.");
        var googleEmail = await db.ExternalLogins.AsNoTracking()
            .Where(link => link.UserAccountId == accountId && link.Provider == Provider)
            .Select(link => link.Email)
            .SingleOrDefaultAsync(cancellationToken);
        return new LoginMethodsView(email, true, googleEmail);
    }

    private async Task AddLinkAsync(IdentityDbContext db, Guid accountId, GoogleIdentity google, CancellationToken cancellationToken)
    {
        db.ExternalLogins.Add(new ExternalLogin
        {
            Provider = Provider,
            Subject = google.Subject,
            UserAccountId = accountId,
            Email = google.Email,
            CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task AuditAsync(string eventType, Guid actorId, Guid subjectId, CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(new SecurityAuditEntry(eventType, "Succeeded", "User",
            ActorUserAccountId: actorId,
            SubjectUserAccountId: subjectId,
            SafeMetadata: JsonSerializer.Serialize(new { provider = Provider })), cancellationToken);
}

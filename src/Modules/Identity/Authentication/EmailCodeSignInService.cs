using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

public enum EmailCodeSignInStatus
{
    SignedIn,
    InvalidCode,
    Unavailable
}

/// <summary>Outcome of entering a sign-in code. <see cref="AttemptsLeft"/> drives the "tries left" hint.</summary>
public sealed record EmailCodeSignInResult(
    EmailCodeSignInStatus Status,
    int AttemptsLeft = 0,
    Guid? UserAccountId = null,
    ClaimsPrincipal? Principal = null,
    bool IsNewAccount = false,
    string AuditReason = "InvalidCode");

/// <summary>
/// Sign-in with a 6-digit email code — the same flow for customers and staff (owner decision 3).
/// The first verified sign-in of an unknown address creates a customer account; an invited staff
/// account becomes verified the first time its owner proves the address.
/// </summary>
public sealed class EmailCodeSignInService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    EmailCodeService codes,
    IdentityAccountProvisioner provisioner,
    IdentityLoginService loginService,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Sends a code to any well-formed address. Deactivated accounts get no email, but the response is
    /// the same, so the endpoint never reveals whether an address is registered.
    /// </summary>
    public async Task<EmailCodeTicket> RequestAsync(string? email, CancellationToken cancellationToken = default)
    {
        var address = NormalizeEmail(email);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.UserAccounts.AsNoTracking()
            .Where(item => item.NormalizedEmail == address.Normalized)
            .Select(item => new { item.Id, item.Email, item.IsActive, item.Person.FullName })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is { IsActive: false })
        {
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            return new EmailCodeTicket(now.Add(EmailCodeService.Lifetime), now.Add(EmailCodeService.ResendCooldown));
        }

        var recipient = account is null
            ? new EmailCodeRecipient(address.Original, address.Normalized, address.Original, null)
            : new EmailCodeRecipient(account.Email, address.Normalized, account.FullName, account.Id);
        return await codes.IssueAsync(db, recipient, EmailCodePurpose.SignIn, cancellationToken);
    }

    public async Task<EmailCodeSignInResult> VerifyAsync(
        string? email,
        string? code,
        CancellationToken cancellationToken = default)
    {
        var address = NormalizeEmail(email);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var check = await codes.VerifyAsync(db, address.Normalized, code, EmailCodePurpose.SignIn, cancellationToken);
        if (!check.Succeeded)
            return new EmailCodeSignInResult(EmailCodeSignInStatus.InvalidCode, check.AttemptsLeft);

        var account = await provisioner.FindOrCreateVerifiedAsync(db, address.Original, cancellationToken);
        var login = await loginService.CreateVerifiedSessionAsync(account.AccountId, cancellationToken);
        return login.Status == IdentityLoginStatus.Succeeded
            ? new EmailCodeSignInResult(EmailCodeSignInStatus.SignedIn, check.AttemptsLeft, account.AccountId,
                login.Principal, account.IsNew, "EmailCode")
            : new EmailCodeSignInResult(EmailCodeSignInStatus.Unavailable, 0, account.AccountId,
                AuditReason: login.AuditReason);
    }

    /// <summary>Validates and normalizes an email address; invalid input is a 400, not a silent no-op.</summary>
    public static (string Original, string Normalized) NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 255 || !new EmailAddressAttribute().IsValid(trimmed))
            throw new IdentityException(400, "A valid email address is required.");
        return (trimmed, IdentityLoginService.NormalizeIdentifier(trimmed));
    }
}

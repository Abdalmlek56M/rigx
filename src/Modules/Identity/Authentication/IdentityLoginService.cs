using System.Data;
using System.Security.Claims;
using System.Text;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

public enum IdentityLoginStatus
{
    Succeeded,
    InvalidCredentials,
    AccountLocked,
    AccountInactive
}

public sealed record IdentityLoginResult(
    IdentityLoginStatus Status,
    Guid? UserAccountId = null,
    ClaimsPrincipal? Principal = null,
    string AuditReason = "InvalidCredentials");

public sealed class IdentityLoginService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IPasswordHashingService passwordHashing,
    TimeProvider timeProvider)
{
    public const string AuthorizationVersionClaim = "authorization_version";
    public const string RbacRevisionClaim = "rbac_revision";
    public const string SessionScopeClaim = "identity_session_scope";
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);
    private const int FailureThreshold = 10;

    public async Task<IdentityLoginResult> AuthenticateAsync(
        string? identifier,
        string? password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier) || password is null)
        {
            if (password is not null)
                _ = passwordHashing.VerifyDummy(password);
            return new IdentityLoginResult(IdentityLoginStatus.InvalidCredentials);
        }

        var normalizedIdentifier = NormalizeIdentifier(identifier);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var candidates = await db.UserAccounts
            .FromSqlInterpolated($"""
                SELECT accounts.*
                FROM user_accounts AS accounts
                INNER JOIN user_account_login_identifiers AS login_identifier
                    ON login_identifier.user_account_id = accounts.id
                WHERE login_identifier.normalized_identifier = {normalizedIdentifier}
                FOR UPDATE OF accounts
                """)
            .ToListAsync(cancellationToken);

        if (candidates.Count != 1)
        {
            _ = passwordHashing.VerifyDummy(password);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityLoginResult(IdentityLoginStatus.InvalidCredentials,
                AuditReason: candidates.Count > 1 ? "IdentifierAmbiguous" : "InvalidCredentials");
        }

        var account = candidates[0];
        var now = timeProvider.GetUtcNow();
        if (!account.IsActive)
        {
            _ = passwordHashing.VerifyDummy(password);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityLoginResult(IdentityLoginStatus.AccountInactive, account.Id, AuditReason: "AccountInactive");
        }

        if (account.LockoutEndUtc is { } lockoutEnd && lockoutEnd > now)
        {
            _ = passwordHashing.VerifyDummy(password);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityLoginResult(IdentityLoginStatus.AccountLocked, account.Id, AuditReason: "AccountLocked");
        }

        var restrictions = await db.AccountRestrictions.AsNoTracking()
            .Where(item => item.UserAccountId == account.Id && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        if (restrictions.Any(item => AccountRestrictionService.IsActive(item, now)))
        {
            _ = passwordHashing.VerifyDummy(password);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityLoginResult(IdentityLoginStatus.AccountInactive, account.Id, AuditReason: "AccountRestricted");
        }

        if (!account.IsEmailVerified || account.PasswordHash is null)
        {
            _ = passwordHashing.VerifyDummy(password);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityLoginResult(IdentityLoginStatus.InvalidCredentials, account.Id,
                AuditReason: "EmailUnverifiedOrPasswordNotSet");
        }

        var verification = passwordHashing.VerifyPassword(account, account.PasswordHash, password);
        if (verification == PasswordHashVerification.Failed)
        {
            if (account.AccessFailedWindowStartedUtc is not { } windowStart || windowStart <= now - FailureWindow)
            {
                account.AccessFailedWindowStartedUtc = now;
                account.AccessFailedCount = 1;
            }
            else
            {
                account.AccessFailedCount++;
            }

            account.LockoutEndUtc = account.AccessFailedCount >= FailureThreshold
                ? now.Add(LockoutDuration)
                : null;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return account.LockoutEndUtc is not null
                ? new IdentityLoginResult(IdentityLoginStatus.AccountLocked, account.Id, AuditReason: "AccountLocked")
                : new IdentityLoginResult(IdentityLoginStatus.InvalidCredentials, account.Id);
        }

        if (verification == PasswordHashVerification.SucceededNeedsRehash)
            account.PasswordHash = passwordHashing.HashPassword(account, password);

        account.AccessFailedCount = 0;
        account.AccessFailedWindowStartedUtc = null;
        account.LockoutEndUtc = null;
        await db.SaveChangesAsync(cancellationToken);

        var authorizationState = await db.AuthorizationStates
            .FromSqlInterpolated($"SELECT * FROM authorization_state WHERE id = {1} FOR SHARE")
            .SingleAsync(cancellationToken);
        var principal = await CreatePrincipalAsync(db, account, authorizationState.RbacRevision, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IdentityLoginResult(IdentityLoginStatus.Succeeded, account.Id, principal,
            "LoginSucceeded");
    }

    public async Task<IdentityLoginResult> CreateVerifiedSessionAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var account = await db.UserAccounts.SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (account is null || !account.IsActive || !account.IsEmailVerified)
        {
            await transaction.CommitAsync(cancellationToken);
            return new IdentityLoginResult(IdentityLoginStatus.InvalidCredentials, account?.Id, AuditReason: "OtpAccountUnavailable");
        }
        var restrictions = await db.AccountRestrictions.AsNoTracking()
            .Where(item => item.UserAccountId == account.Id && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        if (restrictions.Any(item => AccountRestrictionService.IsActive(item, now)))
        {
            await transaction.CommitAsync(cancellationToken);
            return new IdentityLoginResult(IdentityLoginStatus.AccountInactive, account.Id, AuditReason: "AccountRestricted");
        }
        var authorizationState = await db.AuthorizationStates
            .FromSqlInterpolated($"SELECT * FROM authorization_state WHERE id = {1} FOR SHARE")
            .SingleAsync(cancellationToken);
        var principal = await CreatePrincipalAsync(db, account, authorizationState.RbacRevision, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new IdentityLoginResult(IdentityLoginStatus.Succeeded, account.Id, principal, "OtpLoginSucceeded");
    }

    public static string NormalizeIdentifier(string identifier) =>
        identifier.Trim().Normalize(NormalizationForm.FormC).ToUpperInvariant();

    private static async Task<ClaimsPrincipal> CreatePrincipalAsync(
        IdentityDbContext db,
        UserAccount account,
        long rbacRevision,
        CancellationToken cancellationToken)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, account.Id.ToString("D")));
        identity.AddClaim(new Claim(AuthorizationVersionClaim, account.AuthorizationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        identity.AddClaim(new Claim(RbacRevisionClaim, rbacRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        identity.AddClaim(new Claim("email_verified", account.IsEmailVerified ? "true" : "false"));

        var roles = await db.UserRoles.AsNoTracking()
            .Where(userRole => userRole.UserAccountId == account.Id)
            .Select(userRole => userRole.Role.Code)
            .ToListAsync(cancellationToken);
        foreach (var role in roles.Distinct(StringComparer.Ordinal))
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        var permissions = await db.RolePermissions.AsNoTracking()
            .Where(rolePermission => db.UserRoles.Any(userRole =>
                userRole.UserAccountId == account.Id && userRole.RoleId == rolePermission.RoleId))
            .Select(rolePermission => rolePermission.PermissionCode)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var permission in permissions.Where(PermissionCatalog.All.Contains)
                     .Distinct(StringComparer.Ordinal))
            identity.AddClaim(new Claim(PermissionAuthorizationHandler.ClaimType, permission));

        identity.AddClaim(new Claim(SessionScopeClaim, "Full"));
        return new ClaimsPrincipal(identity);
    }
}

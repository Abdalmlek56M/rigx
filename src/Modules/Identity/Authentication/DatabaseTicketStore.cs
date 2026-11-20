using System.Security.Claims;
using System.Security.Cryptography;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.DataProtection;

namespace HardwareStore.Modules.Identity.Authentication;

/// <summary>Stores only a hash of the browser's random session key and a protected ticket.</summary>
public sealed class DatabaseTicketStore(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider,
    IHttpContextAccessor? httpContextAccessor = null) : ITicketStore
{
    private const int MaxKeyAllocationAttempts = 4;
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromMinutes(5);
    private static readonly object RequestSessionKeyItem = new();
    private static readonly object RetrievedSessionKeyItem = new();
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("HardwareStore.AuthenticationTicket.v1");

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var accountId = ReadAccountId(ticket.Principal);
        var (idle, absolute) = GetSessionPolicy(ticket);
        var now = timeProvider.GetUtcNow();

        await using var db = await contextFactory.CreateDbContextAsync();
        var account = await db.UserAccounts.AsNoTracking()
            .Where(user => user.Id == accountId && user.IsActive)
            .Select(user => new { user.AuthorizationVersion })
            .SingleOrDefaultAsync();
        var authorizationState = await db.AuthorizationStates.AsNoTracking()
            .Where(state => state.Id == 1)
            .Select(state => new { state.RbacRevision })
            .SingleOrDefaultAsync();

        if (account is null || authorizationState is null)
            throw new InvalidOperationException("Authentication session cannot be stored because the account or authorization state is unavailable.");

        var principalAuthorizationVersion = ReadIntegerClaim(ticket.Principal, IdentityLoginService.AuthorizationVersionClaim);
        var principalRbacRevision = ReadLongClaim(ticket.Principal, IdentityLoginService.RbacRevisionClaim);
        if (principalAuthorizationVersion != account.AuthorizationVersion ||
            principalRbacRevision != authorizationState.RbacRevision)
            throw new InvalidOperationException("Authentication claims changed while the login session was being created.");

        var absoluteExpiry = now.Add(absolute);
        var idleExpiry = Min(now.Add(idle), absoluteExpiry);
        ticket.Properties.IssuedUtc = now;
        ticket.Properties.ExpiresUtc = idleExpiry;
        var request = httpContextAccessor?.HttpContext?.Request;
        var userAgent = NormalizeUserAgent(request?.Headers.UserAgent.ToString());
        // Raw IP addresses are intentionally not collected: device/browser labels are
        // sufficient for customer session management, and the column remains for legacy rows only.

        for (var attempt = 0; attempt < MaxKeyAllocationAttempts; attempt++)
        {
            var sessionIdBytes = RandomNumberGenerator.GetBytes(32);
            var rawKey = WebEncoders.Base64UrlEncode(sessionIdBytes);
            var keyHash = SHA256.HashData(sessionIdBytes);
            var session = new AuthSession
            {
                SessionKeyHash = keyHash,
                PublicId = Guid.NewGuid(),
                UserAccountId = accountId,
                ProtectedTicket = Protect(ticket),
                UserAgent = userAgent,
                CreatedAtUtc = now,
                LastActivityAtUtc = now,
                IdleExpiresAtUtc = idleExpiry,
                AbsoluteExpiresAtUtc = absoluteExpiry,
                IdleTimeoutSeconds = checked((int)idle.TotalSeconds),
                AuthorizationVersion = account.AuthorizationVersion,
                RbacRevision = authorizationState.RbacRevision,
                RowVersion = 1
            };

            db.AuthSessions.Add(session);
            try
            {
                await db.SaveChangesAsync();
                if (httpContextAccessor?.HttpContext is { } currentContext)
                    currentContext.Items[RequestSessionKeyItem] = rawKey;
                return rawKey;
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                db.Entry(session).State = EntityState.Detached;
                if (attempt + 1 == MaxKeyAllocationAttempts)
                    throw new InvalidOperationException("Unable to allocate a unique authentication session key after repeated collisions.", exception);
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique authentication session key.");
    }

    /// <summary>Revokes only the ticket stored by this request, without reading the response cookie.</summary>
    public async Task<bool> RevokeSessionCreatedByRequestAsync(HttpContext context)
    {
        if (!context.Items.TryGetValue(RequestSessionKeyItem, out var storedKey) || storedKey is not string key)
            return false;

        await RemoveAsync(key);
        context.Items.Remove(RequestSessionKeyItem);
        return true;
    }

    public static bool TryGetCurrentRequestSessionKey(HttpContext context, out string sessionKey)
    {
        if (context.Items.TryGetValue(RetrievedSessionKeyItem, out var value) && value is string key)
        {
            sessionKey = key;
            return true;
        }

        sessionKey = string.Empty;
        return false;
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!TryHashSessionKey(key, out var keyHash))
            return null;

        await using var db = await contextFactory.CreateDbContextAsync();
        var now = timeProvider.GetUtcNow();
        var session = await db.AuthSessions.AsNoTracking()
            .Where(row => row.SessionKeyHash == keyHash && row.RevokedAtUtc == null &&
                          row.IdleExpiresAtUtc > now && row.AbsoluteExpiresAtUtc > now)
            .Where(row => db.UserAccounts.Any(user => user.Id == row.UserAccountId && user.IsActive &&
                          user.AuthorizationVersion == row.AuthorizationVersion))
            .Where(row => db.AuthorizationStates.Any(state => state.Id == 1 && state.RbacRevision == row.RbacRevision))
            .Select(row => new { row.ProtectedTicket, row.LastActivityAtUtc, row.IdleTimeoutSeconds,
                                 row.AbsoluteExpiresAtUtc, row.RowVersion })
            .SingleOrDefaultAsync();

        if (session is null)
            return null;

        if (httpContextAccessor?.HttpContext is { } currentContext)
            currentContext.Items[RetrievedSessionKeyItem] = key;

        if (now - session.LastActivityAtUtc >= ActivityWriteInterval)
        {
            var idleExpiry = Min(now.AddSeconds(session.IdleTimeoutSeconds), session.AbsoluteExpiresAtUtc);
            var update = await db.AuthSessions
                .Where(row => row.SessionKeyHash == keyHash && row.RowVersion == session.RowVersion &&
                              row.RevokedAtUtc == null && row.IdleExpiresAtUtc > now && row.AbsoluteExpiresAtUtc > now)
                .Where(row => db.UserAccounts.Any(user => user.Id == row.UserAccountId && user.IsActive &&
                              user.AuthorizationVersion == row.AuthorizationVersion))
                .Where(row => db.AuthorizationStates.Any(state => state.Id == 1 && state.RbacRevision == row.RbacRevision))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.LastActivityAtUtc, now)
                    .SetProperty(row => row.IdleExpiresAtUtc, idleExpiry)
                    .SetProperty(row => row.RowVersion, row => row.RowVersion + 1));

            if (update == 0)
                return await RetrieveWithoutActivityUpdateAsync(keyHash, now, db);
        }

        return Unprotect(session.ProtectedTicket);
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (!TryHashSessionKey(key, out var keyHash))
            return;

        var accountId = ReadAccountId(ticket.Principal);
        var (idle, _) = GetSessionPolicy(ticket);
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync();
        var current = await db.AuthSessions.AsNoTracking()
            .Where(row => row.SessionKeyHash == keyHash && row.UserAccountId == accountId && row.RevokedAtUtc == null &&
                          row.IdleExpiresAtUtc > now && row.AbsoluteExpiresAtUtc > now)
            .Where(row => db.UserAccounts.Any(user => user.Id == row.UserAccountId && user.IsActive &&
                          user.AuthorizationVersion == row.AuthorizationVersion))
            .Where(row => db.AuthorizationStates.Any(state => state.Id == 1 && state.RbacRevision == row.RbacRevision))
            .Select(row => new { row.RowVersion, row.LastActivityAtUtc, row.AbsoluteExpiresAtUtc, row.IdleTimeoutSeconds })
            .SingleOrDefaultAsync();

        if (current is null)
            return;

        var idleExpiry = Min(now.Add(idle), current.AbsoluteExpiresAtUtc);
        ticket.Properties.ExpiresUtc = idleExpiry;
        var protectedTicket = Protect(ticket);
        var updateActivity = now - current.LastActivityAtUtc >= ActivityWriteInterval;

        var updated = await db.AuthSessions
            .Where(row => row.SessionKeyHash == keyHash && row.UserAccountId == accountId &&
                          row.RowVersion == current.RowVersion && row.RevokedAtUtc == null &&
                          row.IdleExpiresAtUtc > now && row.AbsoluteExpiresAtUtc > now)
            .Where(row => db.UserAccounts.Any(user => user.Id == row.UserAccountId && user.IsActive &&
                          user.AuthorizationVersion == row.AuthorizationVersion))
            .Where(row => db.AuthorizationStates.Any(state => state.Id == 1 && state.RbacRevision == row.RbacRevision))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.ProtectedTicket, protectedTicket)
                .SetProperty(row => row.LastActivityAtUtc, row => updateActivity ? now : row.LastActivityAtUtc)
                .SetProperty(row => row.IdleExpiresAtUtc, row => updateActivity ? idleExpiry : row.IdleExpiresAtUtc)
                .SetProperty(row => row.RowVersion, row => row.RowVersion + 1));

        // A concurrent RemoveAsync wins once it commits: this conditional update then affects zero rows.
        _ = updated;
    }

    public async Task RemoveAsync(string key)
    {
        if (!TryHashSessionKey(key, out var keyHash))
            return;

        await using var db = await contextFactory.CreateDbContextAsync();
        var now = timeProvider.GetUtcNow();
        await db.AuthSessions
            .Where(row => row.SessionKeyHash == keyHash && row.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.RevokedAtUtc, now)
                .SetProperty(row => row.RevokeReason, "CookieAuthenticationRemoved")
                .SetProperty(row => row.RowVersion, row => row.RowVersion + 1));
    }

    private async Task<AuthenticationTicket?> RetrieveWithoutActivityUpdateAsync(
        byte[] keyHash, DateTimeOffset now, IdentityDbContext db)
    {
        var protectedTicket = await db.AuthSessions.AsNoTracking()
            .Where(row => row.SessionKeyHash == keyHash && row.RevokedAtUtc == null &&
                          row.IdleExpiresAtUtc > now && row.AbsoluteExpiresAtUtc > now)
            .Where(row => db.UserAccounts.Any(user => user.Id == row.UserAccountId && user.IsActive &&
                          user.AuthorizationVersion == row.AuthorizationVersion))
            .Where(row => db.AuthorizationStates.Any(state => state.Id == 1 && state.RbacRevision == row.RbacRevision))
            .Select(row => row.ProtectedTicket)
            .SingleOrDefaultAsync();
        return protectedTicket is null ? null : Unprotect(protectedTicket);
    }

    private byte[] Protect(AuthenticationTicket ticket) =>
        _protector.Protect(TicketSerializer.Default.Serialize(ticket));

    private AuthenticationTicket? Unprotect(byte[] protectedTicket)
    {
        try
        {
            var ticket = TicketSerializer.Default.Deserialize(_protector.Unprotect(protectedTicket));
            return ticket;
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static Guid ReadAccountId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(value, out var accountId))
            throw new InvalidOperationException("Authentication ticket has no valid account identifier.");
        return accountId;
    }

    private static int ReadIntegerClaim(ClaimsPrincipal principal, string claimType) =>
        int.TryParse(principal.FindFirst(claimType)?.Value, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidOperationException($"Authentication ticket has no valid {claimType} claim.");

    private static long ReadLongClaim(ClaimsPrincipal principal, string claimType) =>
        long.TryParse(principal.FindFirst(claimType)?.Value, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidOperationException($"Authentication ticket has no valid {claimType} claim.");

    /// <summary>
    /// Customers stay signed in up to 30 days on a remembered device; staff sessions end after 2 idle hours and within 8 hours.
    /// Without "remember this device" a session also ends within <see cref="UnrememberedMaxLifetime"/>.
    /// </summary>
    private static (TimeSpan Idle, TimeSpan Absolute) GetSessionPolicy(AuthenticationTicket ticket)
    {
        var principal = ticket.Principal;
        var customerOnly = principal.IsInRole("Customer") && principal.FindAll(ClaimTypes.Role).Count() == 1;
        var (idle, absolute) = customerOnly
            ? (TimeSpan.FromDays(14), TimeSpan.FromDays(30))
            : (TimeSpan.FromHours(2), TimeSpan.FromHours(8));
        return ticket.Properties.IsPersistent || absolute <= UnrememberedMaxLifetime
            ? (idle, absolute)
            : (idle, UnrememberedMaxLifetime);
    }

    private static readonly TimeSpan UnrememberedMaxLifetime = TimeSpan.FromHours(12);

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) => left <= right ? left : right;

    /// <summary>The user agent exactly as sessions store it.</summary>
    internal static string? NormalizeUserAgent(string? value) => Truncate(value, 512);

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    /// <summary>Hashes a browser session key (32 random bytes, base64url); false for anything else.</summary>
    public static bool TryHashSessionKey(string? key, out byte[] hash)
    {
        hash = [];
        if (string.IsNullOrWhiteSpace(key))
            return false;
        try
        {
            if (WebEncoders.Base64UrlDecode(key).Length != 32)
                return false;
            hash = SHA256.HashData(WebEncoders.Base64UrlDecode(key));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

/// <summary>
/// "Email me when someone signs in from a new device" (X-Profile). A device is new when the account
/// has earlier sessions but none from the same browser (user agent). The first sign-in never alerts.
/// </summary>
public sealed class NewDeviceSignInAlerts(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IdentityEmailOutboxWriter outbox,
    TimeProvider timeProvider)
{
    public async Task<bool> GetAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking()
            .Where(account => account.Id == accountId && account.IsActive)
            .Select(account => (bool?)account.NotifyNewDeviceSignIn)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new IdentityException(401, "The account is not active.");
    }

    public async Task SetAsync(Guid accountId, bool enabled, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var updated = await db.UserAccounts
            .Where(account => account.Id == accountId && account.IsActive)
            .ExecuteUpdateAsync(set => set.SetProperty(account => account.NotifyNewDeviceSignIn, enabled), cancellationToken);
        if (updated == 0)
            throw new IdentityException(401, "The account is not active.");
    }

    /// <summary>Call before the new session is stored, so it does not count as an earlier one.</summary>
    public async Task NotifyIfNewDeviceAsync(Guid accountId, string? userAgent, CancellationToken cancellationToken = default)
    {
        userAgent = DatabaseTicketStore.NormalizeUserAgent(userAgent);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.UserAccounts.AsNoTracking()
            .Where(item => item.Id == accountId && item.NotifyNewDeviceSignIn)
            .Select(item => new { item.Email, item.Person.FullName })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
            return;

        var sessions = db.AuthSessions.AsNoTracking().Where(session => session.UserAccountId == accountId);
        if (!await sessions.AnyAsync(cancellationToken) ||
            await sessions.AnyAsync(session => session.UserAgent == userAgent, cancellationToken))
            return;

        outbox.Enqueue(db, IdentityEmailOutboxWriter.NewDeviceSignInKind, accountId, new OutboxNewDeviceSignInPayload(
            account.Email,
            account.FullName,
            IdentitySessionService.DeviceLabel(userAgent),
            IdentitySessionService.BrowserLabel(userAgent),
            timeProvider.GetUtcNow().ToUniversalTime()));
        await db.SaveChangesAsync(cancellationToken);
    }
}

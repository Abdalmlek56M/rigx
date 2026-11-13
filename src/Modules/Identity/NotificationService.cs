using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity;

public sealed record NotificationView(
    Guid Id,
    string Kind,
    string TitleKey,
    string? BodyKey,
    string? LinkUrl,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);

public sealed record NotificationPage(IReadOnlyList<NotificationView> Items, int UnreadCount);

public sealed class NotificationService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task PublishAsync(
        Guid recipientAccountId,
        string kind,
        string titleKey,
        string? bodyKey = null,
        string? linkUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (recipientAccountId == Guid.Empty)
            return;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.UserNotifications.Add(new UserNotification
        {
            Id = Guid.CreateVersion7(),
            UserAccountId = recipientAccountId,
            Kind = kind,
            TitleKey = titleKey,
            BodyKey = bodyKey,
            LinkUrl = linkUrl,
            CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationPage> GetAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.UserNotifications.AsNoTracking()
            .Where(item => item.UserAccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
        var unread = await db.UserNotifications.CountAsync(
            item => item.UserAccountId == accountId && !item.IsRead, cancellationToken);
        return new NotificationPage(
            rows.Select(item => new NotificationView(
                item.Id, item.Kind, item.TitleKey, item.BodyKey, item.LinkUrl,
                item.IsRead, item.CreatedAtUtc)).ToList(),
            unread);
    }

    public async Task MarkReadAsync(Guid accountId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.UserNotifications
            .Where(item => item.Id == id && item.UserAccountId == accountId && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAtUtc, timeProvider.GetUtcNow().ToUniversalTime()),
                cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.UserNotifications
            .Where(item => item.UserAccountId == accountId && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAtUtc, timeProvider.GetUtcNow().ToUniversalTime()),
                cancellationToken);
    }
}


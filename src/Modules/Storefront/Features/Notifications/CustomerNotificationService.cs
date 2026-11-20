using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

public sealed record CustomerNotificationView(Guid Id, string Kind, string Subject, string? Status, string Target, DateTimeOffset CreatedAtUtc, bool Read);

public sealed record CustomerNotificationFeed(IReadOnlyList<CustomerNotificationView> Items, int UnreadCount);

/// <summary>The notifications bell of a signed-in customer; Host writes entries on order status changes and back-in-stock matches.</summary>
public sealed class CustomerNotificationService(IDbContextFactory<StorefrontDbContext> contextFactory, TimeProvider timeProvider)
{
    /// <summary>The newest entries and how many are unread in total.</summary>
    public async Task<CustomerNotificationFeed> ListAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var mine = db.CustomerNotifications.AsNoTracking().Where(item => item.UserAccountId == accountId);
        var items = await mine.OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
            .Take(CustomerNotificationRules.ListSize)
            .Select(item => new CustomerNotificationView(item.Id, item.Kind, item.Subject, item.Status, item.Target, item.CreatedAtUtc, item.ReadAtUtc != null))
            .ToListAsync(cancellationToken);
        return new CustomerNotificationFeed(items, await mine.CountAsync(item => item.ReadAtUtc == null, cancellationToken));
    }

    /// <summary>Marks one entry read; an unknown id or another account's entry changes nothing.</summary>
    public Task MarkReadAsync(Guid accountId, Guid id, CancellationToken cancellationToken = default) =>
        MarkAsync(accountId, item => item.Id == id, cancellationToken);

    public Task MarkAllReadAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        MarkAsync(accountId, _ => true, cancellationToken);

    /// <summary><paramref name="status"/> is the status the customer sees (completed reads as delivered).</summary>
    public Task AddOrderStatusAsync(Guid accountId, Guid orderId, string reference, string status, CancellationToken cancellationToken = default) =>
        AddAsync(accountId, CustomerNotificationKind.OrderStatus, reference, status, orderId.ToString(), cancellationToken);

    public Task AddBackInStockAsync(Guid accountId, string itemCode, string itemName, CancellationToken cancellationToken = default) =>
        AddAsync(accountId, CustomerNotificationKind.BackInStock, itemName, null, itemCode, cancellationToken);

    /// <summary>Moves a merged account's notifications to the account that was kept; safe to run again.</summary>
    public async Task MoveAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.CustomerNotifications.Where(item => item.UserAccountId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserAccountId, targetId), cancellationToken);
    }

    private async Task MarkAsync(Guid accountId, Expression<Func<CustomerNotification, bool>> which, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.CustomerNotifications.Where(item => item.UserAccountId == accountId && item.ReadAtUtc == null).Where(which)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ReadAtUtc, now), cancellationToken);
    }

    private async Task AddAsync(Guid accountId, string kind, string subject, string? status, string target, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CustomerNotifications.Add(new CustomerNotification
        {
            Id = Guid.CreateVersion7(), UserAccountId = accountId, Kind = kind, Subject = CustomerNotificationRules.Subject(subject),
            Status = status, Target = target, CreatedAtUtc = timeProvider.GetUtcNow()
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

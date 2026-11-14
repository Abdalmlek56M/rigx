using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Blacklist (business §5): customers matched by phone, email or address. Their orders still arrive, flagged, and are
/// confirmed only after full prepayment (<see cref="OrderRules.EnsureCanConfirm"/>).
/// </summary>
public sealed class BlacklistService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<BlacklistView>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Blacklist.AsNoTracking().OrderByDescending(entry => entry.CreatedAtUtc)
            .Select(entry => new BlacklistView(entry.Id, entry.Name, entry.PhoneNumber, entry.Email, entry.AddressText, entry.Reason, entry.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Adds an entry (typed in, or copied from an order) and flags the customer's open new orders.</summary>
    public async Task<BlacklistView> AddAsync(BlacklistRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var reason = OrderRules.Required(request.Reason, 500, "Reason");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        string? name = OrderRules.Optional(request.Name, 200, "Name"), phone, email;
        OrderAddress? address;
        if (request.OrderId is Guid orderId)
        {
            var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
                ?? throw new RetailException(404, "Order was not found.");
            name ??= order.CustomerName; phone = order.PhoneNumber; email = order.CustomerEmail; address = OrderMapping.AddressOf(order);
        }
        else
        {
            phone = string.IsNullOrWhiteSpace(request.Phone) ? null : OrderRules.Phone(request.Phone);
            email = OrderRules.Email(request.Email);
            address = request.Address is null ? null : OrderRules.Address(request.Address, OrderFulfillment.Delivery);
        }
        if (phone is null && email is null && address is null)
            throw new RetailException(400, "Enter a phone number, an email or an address.");
        var entry = new BlacklistEntry
        {
            Id = Guid.CreateVersion7(), Name = name, PhoneNumber = phone, Email = email,
            AddressKey = address is null ? null : OrderRules.AddressKey(address.Governorate, address.Area, address.Street, address.Building),
            AddressText = address is null ? null : string.Join("، ", new[] { address.Area, address.Street, address.Building }.Where(part => part is not null)),
            Reason = reason, CreatedByUserAccountId = actorId, CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
        };
        db.Blacklist.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await db.Orders.Where(order => order.Status == OrderStatus.New && !order.IsBlacklisted &&
                ((phone != null && order.PhoneNumber == phone) || (email != null && order.CustomerEmail == email)))
            .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.IsBlacklisted, true), cancellationToken);
        return new BlacklistView(entry.Id, entry.Name, entry.PhoneNumber, entry.Email, entry.AddressText, entry.Reason, entry.CreatedAtUtc);
    }

    /// <summary>Moves the entry to the trash: it stops matching orders at once and can be restored for 30 days.</summary>
    public async Task RemoveAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.Blacklist.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Blacklist entry was not found.");
        TrashRules.MoveToTrash(entry, actorId, timeProvider.GetUtcNow().ToUniversalTime());
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static Task<bool> MatchesAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken) =>
        AnyMatchAsync(db, order.PhoneNumber, order.CustomerEmail, OrderRules.AddressKey(order.Governorate, order.Area, order.Street, order.Building), cancellationToken);

    /// <summary>True when the phone or the email is on the blacklist (the same matching as for an order).</summary>
    public async Task<bool> MatchesContactAsync(string? phone, string? email, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await AnyMatchAsync(db, phone, OrderRules.Email(email), null, cancellationToken);
    }

    private static Task<bool> AnyMatchAsync(RetailDbContext db, string? phone, string? email, string? addressKey, CancellationToken cancellationToken) =>
        db.Blacklist.AnyAsync(entry => (phone != null && entry.PhoneNumber == phone) ||
            (email != null && entry.Email == email) ||
            (addressKey != null && entry.AddressKey == addressKey), cancellationToken);
}

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

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Blacklist.Where(entry => entry.Id == id).ExecuteDeleteAsync(cancellationToken) == 0)
            throw new RetailException(404, "Blacklist entry was not found.");
    }

    internal static Task<bool> MatchesAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken)
    {
        var addressKey = OrderRules.AddressKey(order.Governorate, order.Area, order.Street, order.Building);
        return db.Blacklist.AnyAsync(entry => entry.PhoneNumber == order.PhoneNumber ||
            (order.CustomerEmail != null && entry.Email == order.CustomerEmail) ||
            (addressKey != null && entry.AddressKey == addressKey), cancellationToken);
    }
}

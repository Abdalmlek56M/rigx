using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// The sales side of customer administration: statistics for the customer list, the orders, warranties and blacklist state
/// of one customer, and moving a merged account's records. Ownership follows <see cref="CustomerOwnership"/>.
/// </summary>
public sealed class CustomerSalesService(IDbContextFactory<RetailDbContext> contextFactory, WarrantyService warranties, BlacklistService blacklist)
{
    /// <summary>Order statistics per account for a page of the customer list.</summary>
    public async Task<IReadOnlyDictionary<Guid, CustomerOrderStats>> StatsAsync(IReadOnlyCollection<CustomerSalesKey> customers,
        CancellationToken cancellationToken = default)
    {
        var phones = customers.Select(customer => CustomerOwnership.PhoneKey(customer.Phone)).OfType<string>().Distinct().ToList();
        var ids = customers.Select(customer => customer.AccountId).ToList();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Orders.AsNoTracking().Where(CustomerOwnership.OrdersOf(ids, phones))
            .Select(order => new { order.UserAccountId, order.PhoneNumber, Facts = Facts(order) }).ToListAsync(cancellationToken);
        return customers.ToDictionary(customer => customer.AccountId, customer =>
        {
            var phoneKey = CustomerOwnership.PhoneKey(customer.Phone);
            return CustomerStatsRules.Summarize(rows.Where(row => CustomerOwnership.Owns(customer.AccountId, phoneKey, row.UserAccountId, row.PhoneNumber))
                .Select(row => row.Facts));
        });
    }

    public async Task<CustomerSalesView> GetAsync(Guid accountId, string? phone, string email, bool includeSerial, CancellationToken cancellationToken = default)
    {
        var phoneKey = CustomerOwnership.PhoneKey(phone);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Orders.AsNoTracking().Where(CustomerOwnership.OrdersOf([accountId], phoneKey is null ? [] : [phoneKey]))
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(order => new
            {
                Row = new CustomerOrderRow(order.Id, order.Reference, order.CreatedAtUtc, order.Status, order.Total, order.PaidAmount, order.Channel,
                    order.UserAccountId == accountId),
                Facts = Facts(order)
            }).ToListAsync(cancellationToken);
        return new CustomerSalesView(CustomerStatsRules.Summarize(rows.Select(row => row.Facts)), rows.Select(row => row.Row).ToList(),
            await warranties.ListForCustomerAsync(accountId, phoneKey, includeSerial, cancellationToken),
            await blacklist.MatchesContactAsync(phoneKey, email, cancellationToken));
    }

    /// <summary>Moves a merged account's orders and warranties to the account that was kept; safe to run again.</summary>
    public async Task<CustomerSalesMoved> MoveAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var orders = await db.Orders.Where(order => order.UserAccountId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.UserAccountId, targetId), cancellationToken);
        var moved = await db.Warranties.Where(warranty => warranty.UserAccountId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(warranty => warranty.UserAccountId, targetId), cancellationToken);
        return new CustomerSalesMoved(orders, moved);
    }

    private static CustomerOrderFacts Facts(StoreOrder order) =>
        new(order.Status, order.Total, order.RefundedAmount, order.CreatedAtUtc, order.LeadSource, order.Usage, order.Governorate);
}

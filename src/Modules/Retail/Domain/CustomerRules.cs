using System.Linq.Expressions;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Which sales records belong to a customer account: the ones saved on the account, and the guest or POS ones (no account)
/// that carry the account's phone number. The one rule behind the customer 360, the customer list and the warranties.
/// </summary>
public static class CustomerOwnership
{
    /// <summary>The account's phone in the stored order form; null when the account has no usable phone (then only linked records count).</summary>
    public static string? PhoneKey(string? phone) => OrderRules.TryPhone(phone);

    public static Expression<Func<StoreOrder, bool>> OrdersOf(IReadOnlyCollection<Guid> accountIds, IReadOnlyCollection<string> phoneKeys) =>
        order => (order.UserAccountId != null && accountIds.Contains(order.UserAccountId.Value)) ||
                 (order.UserAccountId == null && phoneKeys.Contains(order.PhoneNumber));

    public static Expression<Func<Warranty, bool>> WarrantiesOf(Guid accountId, string? phoneKey) =>
        warranty => warranty.UserAccountId == accountId ||
                    (phoneKey != null && warranty.UserAccountId == null && warranty.PhoneNumber == phoneKey);

    /// <summary>Same rule as <see cref="OrdersOf"/> for one record already loaded.</summary>
    public static bool Owns(Guid accountId, string? phoneKey, Guid? recordAccountId, string recordPhone) =>
        recordAccountId == accountId || (recordAccountId is null && phoneKey is not null && recordPhone == phoneKey);
}

/// <summary>The order facts the customer statistics need.</summary>
public sealed record CustomerOrderFacts(string Status, decimal Total, decimal RefundedAmount, DateTimeOffset CreatedAtUtc,
    string? LeadSource, string? Usage, string? Governorate);

/// <summary>Customer statistics: order count and last order ignore cancelled orders; spent counts delivered and completed orders net of refunds.</summary>
public static class CustomerStatsRules
{
    public static CustomerOrderStats Summarize(IEnumerable<CustomerOrderFacts> orders)
    {
        var all = orders.OrderBy(order => order.CreatedAtUtc).ToList();
        var counted = all.Where(order => order.Status != OrderStatus.Cancelled).ToList();
        var spent = all.Where(order => order.Status is OrderStatus.Delivered or OrderStatus.Completed).Sum(order => order.Total - order.RefundedAmount);
        return new CustomerOrderStats(counted.Count, spent, counted.Count == 0 ? null : counted[^1].CreatedAtUtc,
            all.Select(order => order.LeadSource).FirstOrDefault(value => value is not null),
            all.Select(order => order.Usage).FirstOrDefault(value => value is not null),
            all.Select(order => order.Governorate).LastOrDefault(value => value is not null));
    }
}

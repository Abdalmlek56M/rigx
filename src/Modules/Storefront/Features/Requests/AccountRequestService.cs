using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Requests seen from a customer account: the customer's own list, the staff 360 list, and moving them when accounts merge.</summary>
public sealed class AccountRequestService(IDbContextFactory<StorefrontDbContext> contextFactory)
{
    public async Task<IReadOnlyList<MyRequestView>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.CustomerRequests.AsNoTracking().Where(request => request.UserAccountId == userId)
            .OrderByDescending(request => request.CreatedAtUtc).ThenByDescending(request => request.Id).ToListAsync(cancellationToken);
        return rows.Select(request => new MyRequestView(request.Code, request.Type, request.Status, request.CreatedAtUtc, request.ClosedAtUtc,
            request.ModelName ?? request.ItemCode, CustomerRequestRules.OfferVisibleToCustomer(request.Status) ? request.OfferAmount : null)).ToList();
    }

    /// <summary>Requests saved on the account, plus the ones sent without signing in from the account's phone.</summary>
    public async Task<IReadOnlyList<CustomerRequestSummary>> ListForCustomerAsync(Guid accountId, string? phone, CancellationToken cancellationToken = default)
    {
        var mobile = EgyptMobile.TryNormalize(phone, out var normalized) ? normalized : null;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.CustomerRequests.AsNoTracking()
            .Where(request => request.UserAccountId == accountId || (mobile != null && request.UserAccountId == null && request.Phone == mobile))
            .OrderByDescending(request => request.CreatedAtUtc).ThenByDescending(request => request.Id)
            .Select(request => new CustomerRequestSummary(request.Id, request.Code, request.Type, request.Status, request.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Moves a merged account's requests to the account that was kept; safe to run again.</summary>
    public async Task<int> MoveAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.CustomerRequests.Where(request => request.UserAccountId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(request => request.UserAccountId, targetId), cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Retail;

/// <summary>Query and transaction helpers shared by the Retail features.</summary>
internal static class RetailQueries
{
    /// <summary>Loads assemblies with their component links and units.</summary>
    public static IQueryable<Assembly> WithUnits(this IQueryable<Assembly> query) =>
        query.Include(a => a.Units).ThenInclude(link => link.Unit);

    /// <summary>Loads orders with items, their stock components and the event history.</summary>
    public static IQueryable<StoreOrder> WithDetails(this IQueryable<StoreOrder> query) =>
        query.Include(order => order.Items).ThenInclude(item => item.Components).Include(order => order.Events);

    /// <summary>Row-locks an order for the rest of the current transaction.</summary>
    public static Task LockOrderAsync(this RetailDbContext db, Guid id, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync("SELECT 1 FROM store_orders WHERE id = {0} FOR UPDATE", new object[] { id }, cancellationToken);

    /// <summary>Rolls back without hiding the exception that caused the rollback.</summary>
    public static async Task RollbackQuietlyAsync(this IDbContextTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); } catch { /* preserve original */ }
    }
}

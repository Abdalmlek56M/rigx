using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Retail;

/// <summary>PostgreSQL sequences behind the public codes (RX-0001, BLD-01).</summary>
internal static class CodeSequence
{
    public const string StockItem = "stock_item_code_seq";
    public const string Assembly = "assembly_code_seq";
}

/// <summary>Query and transaction helpers shared by the Retail features.</summary>
internal static class RetailQueries
{
    /// <summary>Loads assemblies with their component links, units and the stock items those units belong to.</summary>
    public static IQueryable<Assembly> WithUnits(this IQueryable<Assembly> query) =>
        query.Include(a => a.Units).ThenInclude(link => link.Unit).ThenInclude(unit => unit.StockItem);

    /// <summary>Loads orders with items, their stock components and the event history.</summary>
    public static IQueryable<StoreOrder> WithDetails(this IQueryable<StoreOrder> query) =>
        query.Include(order => order.Items).ThenInclude(item => item.Components).Include(order => order.Events);

    /// <summary>Row-locks an order for the rest of the current transaction.</summary>
    public static Task LockOrderAsync(this RetailDbContext db, Guid id, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync("SELECT 1 FROM store_orders WHERE id = {0} FOR UPDATE", new object[] { id }, cancellationToken);

    /// <summary>Takes the next number of a code sequence and formats it (e.g. <c>RX-0142</c>).</summary>
    public static async Task<string> NextCodeAsync(this RetailDbContext db, string sequence, string prefix, CancellationToken cancellationToken)
    {
        var number = await db.Database.SqlQueryRaw<long>("SELECT nextval({0}::regclass) AS \"Value\"", sequence).SingleAsync(cancellationToken);
        return PublicCode.Format(prefix, number);
    }

    /// <summary>Rolls back without hiding the exception that caused the rollback.</summary>
    public static async Task RollbackQuietlyAsync(this IDbContextTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); } catch { /* preserve original */ }
    }
}

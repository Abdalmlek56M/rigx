using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Purchasing;

internal static class PurchasingQueries
{
    /// <summary>Takes the next number of the purchase code sequence and formats it (e.g. <c>PO-0031</c>).</summary>
    public static async Task<string> NextPurchaseCodeAsync(this PurchasingDbContext db, CancellationToken cancellationToken)
    {
        var number = await db.Database.SqlQueryRaw<long>("SELECT nextval({0}::regclass) AS \"Value\"", CodeSequence.Purchase)
            .SingleAsync(cancellationToken);
        return PublicCode.Format(PublicCode.Purchase, number);
    }
}

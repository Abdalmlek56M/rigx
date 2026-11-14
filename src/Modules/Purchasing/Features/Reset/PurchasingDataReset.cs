using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Reset;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Purchasing;

/// <summary>The Purchasing part of the "business" reset: purchases, their lines and the sellers.</summary>
public sealed class PurchasingDataReset(IDbContextFactory<PurchasingDbContext> contextFactory) : IDataResetSource
{
    private static readonly string[] BusinessTables = ["purchase_lines", "purchases", "sellers"];

    public async Task<int> ResetAsync(IReadOnlySet<string> sections, CancellationToken cancellationToken = default)
    {
        if (!sections.Contains(DataResetRules.Business)) return 0;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await TableReset.DeleteAllAsync(db, BusinessTables, cancellationToken);
    }
}

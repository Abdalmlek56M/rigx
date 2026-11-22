using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Reset;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>The PC Builder part of the "business" reset: customers' saved builds with their reactions and moderation.</summary>
public sealed class PcBuilderDataReset(IDbContextFactory<PcBuilderDbContext> contextFactory) : IDataResetSource
{
    public async Task<int> ResetAsync(IReadOnlySet<string> sections, CancellationToken cancellationToken = default)
    {
        if (!sections.Contains(DataResetRules.Business)) return 0;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await TableReset.DeleteAllAsync(db, ["saved_builds", "saved_build_moderations", "saved_build_reactions"], cancellationToken);
    }
}

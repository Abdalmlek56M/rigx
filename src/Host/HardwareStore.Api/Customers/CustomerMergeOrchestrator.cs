using HardwareStore.Modules.Identity;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>
/// Merges a duplicate customer account into the one to keep (decision 11: it crosses four modules, so Host orchestrates).
/// The rules are checked first; Retail, Storefront and PcBuilder then move their records; Identity goes last and closes the
/// source account. Every step is idempotent, so running the merge again after a partial failure finishes it.
/// </summary>
public sealed class CustomerMergeOrchestrator(
    CustomerMergeService identity,
    CustomerSalesService sales,
    AccountRequestService requests,
    SavedBuildStore savedBuilds,
    CustomerProfileComposer composer)
{
    public async Task<CustomerProfile> MergeAsync(Guid targetId, Guid sourceId, Guid actorId, bool includeSerial, CancellationToken cancellationToken)
    {
        await identity.EnsureCanMergeAsync(targetId, sourceId, cancellationToken);
        var moved = await sales.MoveAsync(sourceId, targetId, cancellationToken);
        var movedRequests = await requests.MoveAsync(sourceId, targetId, cancellationToken);
        var movedBuilds = await savedBuilds.MoveOwnerAsync(sourceId, targetId, cancellationToken);
        await identity.MergeAsync(targetId, sourceId, actorId, new CustomerMergeCounts(moved.Orders, moved.Warranties, movedRequests, movedBuilds),
            cancellationToken);
        return await composer.GetAsync(targetId, includeSerial, cancellationToken);
    }
}

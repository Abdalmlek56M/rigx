using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;

/// <summary>Who a new build part was bought from and what was paid for it now (the rest stays owed to the seller).</summary>
public sealed record BuildPartSource(Guid SellerId, string? PaymentMethod, decimal PaidAmount, bool PayFromCash);

/// <summary>
/// A part bought for a build and added to it in one go: a one-line purchase from the seller is created and received (so the stock
/// item, the seller's debt and the money paid all exist like for any purchase), then the received unit joins the build. It spans
/// Purchasing, Retail stock, Retail money and the build, so it lives in Host (architecture decision 11).
/// </summary>
public sealed class BuildPartPurchaseService(PurchaseService purchases, PurchaseReceiptService receipts, AssemblyPartService parts)
{
    public async Task<AssemblyView> AddAsync(Guid assemblyId, long rowVersion, PurchaseLineRequest line, BuildPartSource source, bool includeCosts,
        Guid actorId, CancellationToken cancellationToken)
    {
        // Money must not move for a part that cannot join the build, so the build is checked first.
        await parts.EnsureCanAddPartAsync(assemblyId, rowVersion, cancellationToken);
        var draft = await purchases.CreateAsync(new PurchaseRequest(source.SellerId, PurchaseKind.Parts, false, null, null, null,
            string.IsNullOrWhiteSpace(source.PaymentMethod) ? SellerPaymentMethod.Cash : source.PaymentMethod.Trim(), source.PaidAmount, 0m, null,
            [line], PayFromCash: source.PayFromCash), actorId, cancellationToken);
        var received = await receipts.ReceiveAsync(draft.Id, actorId, cancellationToken);
        var unitId = received.Items.Items.Single().Units.Single().Id;
        return await parts.AddPartAsync(assemblyId, new AssemblyAddPartRequest(unitId, rowVersion), includeCosts, cancellationToken);
    }
}

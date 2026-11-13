using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Storefront;

/// <summary>
/// Turns an accepted sell / trade-in request into a seller and a draft purchase. It spans two modules
/// (Storefront request, Purchasing seller and purchase), so it lives in Host per architecture decision 11.
/// </summary>
public sealed class RequestPurchaseConversion(RequestFollowUpService followUp, SellerService sellers, PurchaseService purchases)
{
    public async Task<CustomerRequestView> ConvertAsync(Guid requestId, long rowVersion, Guid actorId, CancellationToken cancellationToken)
    {
        var request = await followUp.EnsureConvertibleAsync(requestId, rowVersion, cancellationToken);
        var sellerId = await FindOrCreateSellerAsync(request, cancellationToken);
        var purchase = await purchases.CreateAsync(DraftPurchase(request, sellerId), actorId, cancellationToken);
        try
        {
            return await followUp.MarkConvertedAsync(requestId, rowVersion, purchase.Id, purchase.Code, cancellationToken);
        }
        catch
        {
            await purchases.CancelAsync(purchase.Id, purchase.RowVersion, CancellationToken.None);
            throw;
        }
    }

    private async Task<Guid> FindOrCreateSellerAsync(CustomerRequestView request, CancellationToken cancellationToken)
    {
        var existing = await sellers.ListAsync(request.Phone, null, false, cancellationToken);
        if (existing.Count > 0) return existing[0].Id;
        var created = await sellers.CreateAsync(new SellerRequest(SellerKind.Person, request.Name, request.Phone, null,
            request.Governorate, null, request.Source, $"From request {request.Code}"), cancellationToken);
        return created.Id;
    }

    private static PurchaseRequest DraftPurchase(CustomerRequestView request, Guid sellerId)
    {
        var origin = $"From request {request.Code}";
        var notes = string.IsNullOrWhiteSpace(request.Message) ? origin : $"{origin}: {request.Message}";
        if (notes.Length > PurchaseDraftRules.MaxNotesLength) notes = notes[..PurchaseDraftRules.MaxNotesLength];
        var cost = request.OfferAmount!.Value;
        var line = new PurchaseLineRequest(request.ModelName, PurchaseCondition.Used, 1, cost, request.ExpectedResale ?? cost);
        return new PurchaseRequest(sellerId, PurchaseKind.Parts, false, null, null, notes, SellerPaymentMethod.Cash, 0m, 0m, null, [line]);
    }
}

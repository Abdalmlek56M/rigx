using HardwareStore.Common.Domain;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;

public sealed record PurchasePaymentRequest(decimal Amount, string? Method, long RowVersion);

/// <summary>
/// Pays the seller of a received purchase what is still owed. It spans Purchasing (what is owed) and Retail (the money
/// account), so it lives in Host like the receipt (architecture decision 11).
/// </summary>
public sealed class PurchasePaymentService(PurchaseService purchases, MoneyAccountService money, TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    public async Task<PurchaseView> PayAsync(Guid purchaseId, PurchasePaymentRequest request, Guid actorId, CancellationToken cancellationToken)
    {
        var purchase = await purchases.GetAsync(purchaseId, cancellationToken);
        if (purchase.RowVersion != request.RowVersion) throw new PurchasingException(409, "The purchase was changed by another user.");
        if (purchase.Status != PurchaseStatus.Received) throw new PurchasingException(409, "Only a received purchase takes payments.");
        if (request.Amount <= 0 || request.Amount > purchase.Remaining) throw new PurchasingException(400, "The payment is more than what is still owed to the seller.");
        var method = string.IsNullOrWhiteSpace(request.Method) ? SellerPaymentMethod.Cash : request.Method.Trim();
        if (method is not (SellerPaymentMethod.Cash or SellerPaymentMethod.InstaPay or SellerPaymentMethod.Wallet))
            throw new PurchasingException(400, "The payment method must be Cash, InstaPay or Wallet.");
        // The money leaves first: a short balance stops the payment (409) before the purchase records it.
        await money.PaySellerAsync(purchase.Code, method, request.Amount, BusinessDay.Today(timeProvider, businessZone), actorId, cancellationToken);
        return await purchases.RecordPaymentAsync(purchaseId, request.Amount, request.RowVersion, cancellationToken);
    }
}

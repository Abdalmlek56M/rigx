namespace HardwareStore.Modules.Storefront;

internal static class CustomerRequestMapping
{
    public static CustomerRequestView ToView(this CustomerRequest request) =>
        new(request.Id, request.Code, request.Type, request.Status, request.Name, request.Phone, request.Email, request.Governorate,
            request.Source, request.UserAccountId, request.Subject, request.Message, request.PartType, request.ModelName,
            request.OwnedFor, request.ItemCode, request.ServiceKind, request.PreferredDate, request.OfferAmount,
            request.ExpectedResale, request.InternalNote, request.PurchaseId, request.PurchaseCode, request.CreatedAtUtc,
            request.UpdatedAtUtc, request.ClosedAtUtc, request.RowVersion);
}

using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    private static async Task<Ok<IReadOnlyList<OrderView>>> ListOrdersAsync(HttpContext context, OrderService orders, CancellationToken ct, string? status = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await orders.ListOrdersAsync(status, context.HasPermission("Sales.ViewCustomerContact"), ct));
    }

    private static async Task<Ok<AdminOrderDetailView>> GetOrderDetailAsync(Guid id, HttpContext context, OrderService orders, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await orders.GetAdminOrderDetailAsync(id, context.HasPermission("Inventory.ViewSerials"), ct));
    }

    /// <summary>
    /// Status change; confirming starts the reservation from the store settings, delivering issues the invoice (numbered with the
    /// settings prefix, seller = the signed-in staff member) and the customer is emailed (confirmed, shipped, invoice).
    /// </summary>
    private static async Task<Ok<OrderView>> UpdateStatusAsync(Guid id, OrderStatusRequest request, HttpContext context, OrderStatusService statuses,
        StoreSettingsService settings, IdentityProfileService profiles, InvoiceService invoices, IdentityEmailOutboxWriter outbox,
        OrderLinks links, TimeZoneInfo businessTimeZone, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        var store = await settings.GetAdminAsync(ct);
        var seller = request.Status?.Trim() == OrderStatus.Delivered ? await profiles.GetFullNameAsync(actorId, ct) : null;
        var order = await statuses.UpdateStatusAsync(id, request, actorId, new ReservationPolicy(store.Sales.ReservationHours, store.Sales.ReservationAutoRelease),
            new InvoicePolicy(store.Invoice.NumberPrefix, seller, businessTimeZone), ct);
        // Same row version = the order was already in that status, so nothing changed and no email goes out again.
        if (order.Status != request.Status?.Trim() || order.RowVersion == request.RowVersion) return TypedResults.Ok(order);
        if (order.Status == OrderStatus.Delivered) await OrderEmails.QueueInvoiceAsync(outbox, links, order, invoices, ct);
        var stage = order.Status switch
        {
            OrderStatus.Confirmed => OrderEmailMessage.Confirmed,
            OrderStatus.Shipped => OrderEmailMessage.Shipped,
            _ => null
        };
        if (stage is not null) await OrderEmails.QueueAsync(outbox, links, order, stage, ct);
        return TypedResults.Ok(order);
    }

    private static async Task<Ok<OrderView>> AddPaymentAsync(Guid id, OrderPaymentRequest request, HttpContext context, OrderPaymentService payments, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await payments.AddPaymentAsync(id, request, actorId, ct));
    }

    /// <summary>Courier and waybill; an empty courier takes the default courier from the shipping settings.</summary>
    private static async Task<Ok<OrderView>> SetShippingAsync(Guid id, OrderShippingRequest request, HttpContext context,
        OrderFulfillmentService fulfillment, StoreSettingsService settings, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        var courier = string.IsNullOrWhiteSpace(request.CourierName) ? (await settings.GetAdminAsync(ct)).Shipping.CourierName : request.CourierName;
        return TypedResults.Ok(await fulfillment.SetShippingAsync(id, request with { CourierName = courier }, actorId, ct));
    }

    private static async Task<Ok<OrderView>> SetReservationAsync(Guid id, OrderReservationRequest request, HttpContext context, OrderFulfillmentService fulfillment, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await fulfillment.SetReservationAsync(id, request, actorId, ct));
    }

    private static async Task<Ok<IReadOnlyList<BlacklistView>>> ListBlacklistAsync(HttpContext context, BlacklistService blacklist, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await blacklist.ListAsync(ct));
    }

    private static async Task<Ok<BlacklistView>> AddBlacklistAsync(BlacklistRequest request, HttpContext context, BlacklistService blacklist, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await blacklist.AddAsync(request, actorId, ct));
    }

    private static async Task<NoContent> RemoveBlacklistAsync(Guid id, HttpContext context, BlacklistService blacklist, TrashAuditor audit, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        await blacklist.RemoveAsync(id, actorId, ct);
        await audit.DeletedAsync(BlacklistTrashSource.TrashKind, id, actorId, ct);
        return TypedResults.NoContent();
    }

    private static async Task<PosPolicy> PosPolicyAsync(HttpContext context, StoreSettingsService settings, CancellationToken ct)
    {
        var store = await settings.GetAdminAsync(ct);
        var (sales, shipping) = (store.Sales, store.Shipping);
        return new PosPolicy(sales.PosMaxDiscountPercent, context.HasPermission("Sales.OverrideMinimumPrice"),
            new ReservationPolicy(sales.ReservationHours, sales.ReservationAutoRelease), code => ShippingRateOf(shipping, code));
    }

    private static ShippingRate ShippingRateOf(ShippingSettings shipping, string governorateCode)
    {
        var quote = shipping.QuoteFor(governorateCode.Trim().ToUpperInvariant());
        return new ShippingRate(quote.Fee, quote.DeliveryTime);
    }

    private static async Task<Ok<PosPreview>> PreviewPosSaleAsync(PosSaleRequest request, HttpContext context, PosService pos, StoreSettingsService settings, CancellationToken ct) =>
        TypedResults.Ok(await pos.PreviewAsync(request, await PosPolicyAsync(context, settings, ct), context.HasPermission("Inventory.ViewCosts"), ct));

    /// <summary>Store sale: the lead source must come from the settings list.</summary>
    private static async Task<Ok<OrderView>> CreatePosSaleAsync(PosSaleRequest request, HttpContext context, PosService pos, StoreSettingsService settings, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        await EnsureLeadSourceAsync(settings, request.LeadSource, ct);
        return TypedResults.Ok(await pos.SellAsync(request, await PosPolicyAsync(context, settings, ct), actorId, ct));
    }

    private static async Task<Ok<IReadOnlyList<PosCustomerView>>> SearchPosCustomersAsync(PosService pos, CancellationToken ct, string? search = null) =>
        TypedResults.Ok(await pos.SearchCustomersAsync(search, ct));

    private static async Task EnsureLeadSourceAsync(StoreSettingsService settings, string? leadSource, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(leadSource)) return;
        if (!(await settings.GetAdminAsync(ct)).LeadSources.Sources.Contains(leadSource.Trim()))
            throw new RetailException(400, "Choose the lead source from the list in the store settings.");
    }

    private static async Task<Ok<OrderView>> CompleteOrderAsync(Guid id, CompleteOrderRequest request, HttpContext context, OrderStatusService statuses, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await statuses.CompleteOrderAsync(id, request, actorId, ct));
    }

    private static async Task<Ok<OrderView>> UpdatePaymentAsync(Guid id, PaymentStatusRequest request, HttpContext context, OrderPaymentService payments, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await payments.UpdatePaymentAsync(id, request, actorId, ct));
    }

    private static async Task<Ok<OrderView>> WaivePaymentAsync(Guid id, WaivePaymentRequest request, HttpContext context, OrderPaymentService payments, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await payments.WaivePaymentAsync(id, request, actorId, ct));
    }

    private static async Task<Ok<IReadOnlyList<ReturnView>>> ListReturnsAsync(HttpContext context, ReturnService returns, CancellationToken ct, string? status = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await returns.ListReturnsAsync(status, ct));
    }

    private static async Task<Ok<ReturnView>> CreateReturnAsync(ReturnCreateRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await returns.CreateReturnAsync(request, actorId, ct));
    }

    private static async Task<Ok<ReturnView>> InspectReturnAsync(Guid id, ReturnInspectRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await returns.InspectReturnAsync(id, request, actorId, ct));
    }

    private static async Task<Ok<ReturnView>> RejectReturnAsync(Guid id, ReturnRejectRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await returns.RejectReturnAsync(id, request, actorId, ct));
    }

    /// <summary>
    /// Website checkout (Storefront settings + published Terms + Retail, so it stays in Host): guests may order when
    /// guest checkout is on; fee, upfront part, enabled methods and the minimum come from the store settings.
    /// </summary>
    private static async Task<Ok<CustomerOrderView>> CreateCustomerOrderAsync(CheckoutRequest request, HttpContext context, OrderService orders,
        StoreSettingsService settings, ContentPolicyService policies, IdentityEmailOutboxWriter outbox, OrderLinks links,
        BuilderCatalogLoader builderCatalogs, CancellationToken ct)
    {
        var signedIn = context.TryGetUserId(out var accountId);
        var store = await settings.GetAdminAsync(ct);
        if (!signedIn && !store.Controls.GuestCheckoutEnabled) throw new UnauthenticatedException();
        string termsVersion;
        try
        {
            var terms = await policies.GetPublicAsync("Terms", ct);
            termsVersion = terms.VersionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (StorefrontException)
        {
            throw new RetailException(StatusCodes.Status409Conflict, "The Terms policy is not published; ordering is unavailable.");
        }
        await EnsureLeadSourceAsync(settings, request.LeadSource, ct);
        var delivery = request.FulfillmentMethod?.Trim() == OrderFulfillment.Delivery;
        if (!delivery && !store.Shipping.PickupEnabled) throw new RetailException(400, "Pickup from the store is not available.");
        var shipping = delivery && !string.IsNullOrWhiteSpace(request.Address?.Governorate)
            ? ShippingRateOf(store.Shipping, request.Address.Governorate)
            : new ShippingRate(0m, null);
        var methods = new List<string>();
        if (store.Payment.CashOnDeliveryEnabled) methods.Add(OrderPaymentMethod.CashOnDelivery);
        if (store.Payment.InstaPayEnabled) methods.Add(OrderPaymentMethod.InstaPay);
        if (store.Payment.WalletEnabled) methods.Add(OrderPaymentMethod.Wallet);
        var policy = new CheckoutPolicy(signedIn ? accountId : null, termsVersion, shipping,
            subtotal => store.Payment.UpfrontFor(subtotal, shipping.Fee, pickup: false).Total, methods, store.Controls.MinimumOrderAmount,
            await builderCatalogs.BuildPriceRuleForAsync(request.Items, ct));
        var placed = await orders.CreateOnlineAsync(request, policy, ct);
        await OrderEmails.QueueAsync(outbox, links, placed.Order, OrderEmailMessage.Received, ct);
        return TypedResults.Ok(placed.Customer);
    }

    private static async Task<Ok<TrackingView>> GetTrackingAsync(string token, HttpContext context, OrderTrackingService tracking, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await tracking.GetAsync(token, ct));
    }

    private static async Task<Ok<TrackingView>> RateOrderAsync(string token, OrderRatingRequest request, OrderTrackingService tracking, CancellationToken ct) =>
        TypedResults.Ok(await tracking.RateAsync(token, request, ct));

    private static async Task<Ok<IReadOnlyList<CustomerOrderView>>> ListMyOrdersAsync(HttpContext context, OrderService orders, CancellationToken ct)
    {
        var accountId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await orders.ListMyOrdersAsync(accountId, ct));
    }

    private static async Task<Ok<CustomerOrderView>> GetMyOrderAsync(Guid id, HttpContext context, OrderService orders, CancellationToken ct)
    {
        var accountId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await orders.GetMyOrderAsync(id, accountId, ct));
    }
}

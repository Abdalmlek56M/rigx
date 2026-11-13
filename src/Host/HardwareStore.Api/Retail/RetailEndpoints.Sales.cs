using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListOrdersAsync(HttpContext context, OrderService orders, CancellationToken ct, string? status = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await orders.ListOrdersAsync(status, context.HasPermission("Sales.ViewCustomerContact"), ct));
    }

    private static async Task<IResult> GetOrderDetailAsync(Guid id, HttpContext context, OrderService orders, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await orders.GetAdminOrderDetailAsync(id, context.HasPermission("Inventory.ViewSerials"), ct));
    }

    /// <summary>
    /// Status change; confirming starts the reservation from the store settings, delivering issues the invoice (numbered with the
    /// settings prefix, seller = the signed-in staff member) and the customer is emailed (confirmed, shipped, invoice).
    /// </summary>
    private static async Task<IResult> UpdateStatusAsync(Guid id, OrderStatusRequest request, HttpContext context, OrderStatusService statuses,
        StoreSettingsService settings, IdentityProfileService profiles, InvoiceService invoices, IdentityEmailOutboxWriter outbox,
        OrderLinks links, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        var store = await settings.GetAdminAsync(ct);
        var seller = request.Status?.Trim() == OrderStatus.Delivered ? await profiles.GetFullNameAsync(actorId, ct) : null;
        var order = await statuses.UpdateStatusAsync(id, request, actorId, new ReservationPolicy(store.Sales.ReservationHours, store.Sales.ReservationAutoRelease),
            new InvoicePolicy(store.Invoice.NumberPrefix, seller), ct);
        // Same row version = the order was already in that status, so nothing changed and no email goes out again.
        if (order.Status != request.Status?.Trim() || order.RowVersion == request.RowVersion) return Results.Ok(order);
        if (order.Status == OrderStatus.Delivered) await OrderEmails.QueueInvoiceAsync(outbox, links, order, invoices, ct);
        var stage = order.Status switch
        {
            OrderStatus.Confirmed => OrderEmailMessage.Confirmed,
            OrderStatus.Shipped => OrderEmailMessage.Shipped,
            _ => null
        };
        if (stage is not null) await OrderEmails.QueueAsync(outbox, links, order, stage, ct);
        return Results.Ok(order);
    }

    private static async Task<IResult> AddPaymentAsync(Guid id, OrderPaymentRequest request, HttpContext context, OrderPaymentService payments, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await payments.AddPaymentAsync(id, request, actorId, ct));
    }

    /// <summary>Courier and waybill; an empty courier takes the default courier from the shipping settings.</summary>
    private static async Task<IResult> SetShippingAsync(Guid id, OrderShippingRequest request, HttpContext context,
        OrderFulfillmentService fulfillment, StoreSettingsService settings, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        var courier = string.IsNullOrWhiteSpace(request.CourierName) ? (await settings.GetAdminAsync(ct)).Shipping.CourierName : request.CourierName;
        return Results.Ok(await fulfillment.SetShippingAsync(id, request with { CourierName = courier }, actorId, ct));
    }

    private static async Task<IResult> SetReservationAsync(Guid id, OrderReservationRequest request, HttpContext context, OrderFulfillmentService fulfillment, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await fulfillment.SetReservationAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> ListBlacklistAsync(HttpContext context, BlacklistService blacklist, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await blacklist.ListAsync(ct));
    }

    private static async Task<IResult> AddBlacklistAsync(BlacklistRequest request, HttpContext context, BlacklistService blacklist, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await blacklist.AddAsync(request, actorId, ct));
    }

    private static async Task<IResult> RemoveBlacklistAsync(Guid id, BlacklistService blacklist, CancellationToken ct)
    {
        await blacklist.RemoveAsync(id, ct);
        return Results.NoContent();
    }

    private static async Task<PosPolicy> PosPolicyAsync(HttpContext context, StoreSettingsService settings, CancellationToken ct)
    {
        var sales = (await settings.GetAdminAsync(ct)).Sales;
        return new PosPolicy(sales.PosMaxDiscountPercent, context.HasPermission("Sales.OverrideMinimumPrice"),
            new ReservationPolicy(sales.ReservationHours, sales.ReservationAutoRelease));
    }

    private static async Task<IResult> PreviewPosSaleAsync(PosSaleRequest request, HttpContext context, PosService pos, StoreSettingsService settings, CancellationToken ct) =>
        Results.Ok(await pos.PreviewAsync(request, await PosPolicyAsync(context, settings, ct), context.HasPermission("Inventory.ViewCosts"), ct));

    /// <summary>Store sale: the lead source must come from the settings list.</summary>
    private static async Task<IResult> CreatePosSaleAsync(PosSaleRequest request, HttpContext context, PosService pos, StoreSettingsService settings, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        await EnsureLeadSourceAsync(settings, request.LeadSource, ct);
        return Results.Ok(await pos.SellAsync(request, await PosPolicyAsync(context, settings, ct), actorId, ct));
    }

    private static async Task<IResult> FindPosCustomerAsync(PosService pos, CancellationToken ct, string? phone = null)
    {
        var customer = await pos.FindCustomerAsync(phone, ct);
        return customer is null ? Results.NotFound() : Results.Ok(customer);
    }

    private static async Task EnsureLeadSourceAsync(StoreSettingsService settings, string? leadSource, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(leadSource)) return;
        if (!(await settings.GetAdminAsync(ct)).LeadSources.Sources.Contains(leadSource.Trim()))
            throw new RetailException(400, "Choose the lead source from the list in the store settings.");
    }

    private static async Task<IResult> CompleteOrderAsync(Guid id, CompleteOrderRequest request, HttpContext context, OrderStatusService statuses, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await statuses.CompleteOrderAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> UpdatePaymentAsync(Guid id, PaymentStatusRequest request, HttpContext context, OrderPaymentService payments, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await payments.UpdatePaymentAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> WaivePaymentAsync(Guid id, WaivePaymentRequest request, HttpContext context, OrderPaymentService payments, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await payments.WaivePaymentAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> ListReturnsAsync(HttpContext context, ReturnService returns, CancellationToken ct, string? status = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await returns.ListReturnsAsync(status, ct));
    }

    private static async Task<IResult> CreateReturnAsync(ReturnCreateRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await returns.CreateReturnAsync(request, actorId, ct));
    }

    private static async Task<IResult> InspectReturnAsync(Guid id, ReturnInspectRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await returns.InspectReturnAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> RejectReturnAsync(Guid id, ReturnRejectRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await returns.RejectReturnAsync(id, request, actorId, ct));
    }

    /// <summary>
    /// Website checkout (Storefront settings + published Terms + Retail, so it stays in Host): guests may order when
    /// guest checkout is on; fee, upfront part, enabled methods and the minimum come from the store settings.
    /// </summary>
    private static async Task<IResult> CreateCustomerOrderAsync(CheckoutRequest request, HttpContext context, OrderService orders,
        StoreSettingsService settings, ContentPolicyService policies, IdentityEmailOutboxWriter outbox, OrderLinks links, CancellationToken ct)
    {
        var signedIn = context.TryGetUserId(out var accountId);
        var store = await settings.GetAdminAsync(ct);
        if (!signedIn && !store.Controls.GuestCheckoutEnabled) return Results.Unauthorized();
        string termsVersion;
        try
        {
            var terms = await policies.GetPublicAsync("Terms", ct);
            termsVersion = terms.VersionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (StorefrontException)
        {
            return Results.Problem(statusCode: 409, title: "The Terms policy is not published; ordering is unavailable.");
        }
        await EnsureLeadSourceAsync(settings, request.LeadSource, ct);
        var delivery = request.FulfillmentMethod?.Trim() == OrderFulfillment.Delivery;
        if (!delivery && !store.Shipping.PickupEnabled) throw new RetailException(400, "Pickup from the store is not available.");
        var fee = delivery && !string.IsNullOrWhiteSpace(request.Address?.Governorate)
            ? store.Shipping.QuoteFor(request.Address.Governorate.Trim().ToUpperInvariant()).Fee
            : 0m;
        var methods = new List<string>();
        if (store.Payment.CashOnDeliveryEnabled) methods.Add(OrderPaymentMethod.CashOnDelivery);
        if (store.Payment.InstaPayEnabled) methods.Add(OrderPaymentMethod.InstaPay);
        if (store.Payment.WalletEnabled) methods.Add(OrderPaymentMethod.Wallet);
        var policy = new CheckoutPolicy(signedIn ? accountId : null, termsVersion, fee,
            subtotal => store.Payment.UpfrontFor(subtotal, fee, pickup: false).Total, methods, store.Controls.MinimumOrderAmount);
        var order = await orders.CreateOnlineAsync(request, policy, ct);
        await OrderEmails.QueueAsync(outbox, links, order, OrderEmailMessage.Received, ct);
        return Results.Ok(order);
    }

    private static async Task<IResult> GetTrackingAsync(string token, HttpContext context, OrderTrackingService tracking, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await tracking.GetAsync(token, ct));
    }

    private static async Task<IResult> RateOrderAsync(string token, OrderRatingRequest request, OrderTrackingService tracking, CancellationToken ct) =>
        Results.Ok(await tracking.RateAsync(token, request, ct));

    /// <summary>"اتباع قبل كده": off → 404; the governorate shows only when the sales settings allow it.</summary>
    private static async Task<IResult> ListSoldFeedAsync(SoldFeedService sold, StoreSettingsService settings, CancellationToken ct,
        string? kind = null, Guid? categoryId = null, int page = 1)
    {
        var sales = (await settings.GetAdminAsync(ct)).Sales;
        if (!sales.SoldFeedEnabled) return Results.NotFound();
        return Results.Ok(await sold.ListAsync(sales.SoldFeedShowGovernorate, kind, categoryId, page, ct));
    }

    private static async Task<IResult> ListMyOrdersAsync(HttpContext context, OrderService orders, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var accountId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await orders.ListMyOrdersAsync(accountId, ct));
    }

    private static async Task<IResult> GetMyOrderAsync(Guid id, HttpContext context, OrderService orders, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var accountId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await orders.GetMyOrderAsync(id, accountId, ct));
    }
}

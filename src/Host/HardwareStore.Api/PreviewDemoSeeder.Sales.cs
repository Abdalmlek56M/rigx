using System.Globalization;
using HardwareStore.Common.Domain;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;

internal sealed partial class PreviewDemoSeeder
{
    /// <summary>
    /// Sixteen online orders over the last 30 days, every customer status, delivery and pickup, COD/InstaPay/wallet;
    /// the four completed ones are what the dashboard's sales and charts count.
    /// </summary>
    private async Task<int> SeedOrdersAsync(CancellationToken cancellationToken)
    {
        var customerIds = customerIdsByEmail.Values.ToList();
        if (customerIds.Count == 0) return 0;
        await using var db = await retailFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Orders.AsNoTracking().AnyAsync(
                order => order.UserAccountId != null && customerIds.Contains(order.UserAccountId.Value), cancellationToken))
            return 0;

        var store = await Settings.GetAdminAsync(cancellationToken);
        var terms = await Policies.GetPublicAsync("Terms", cancellationToken);
        var methods = PaymentMethods(store.Payment);
        if (methods.Count == 0) throw new InvalidOperationException("Demo orders need at least one enabled payment method.");
        var reservation = new ReservationPolicy(store.Sales.ReservationHours, store.Sales.ReservationAutoRelease);
        var invoice = new InvoicePolicy(store.Invoice.NumberPrefix, ownerName, businessZone);
        var customers = PreviewDemoDefinitions.Customers().ToDictionary(customer => customer.Email, StringComparer.OrdinalIgnoreCase);

        var created = 0;
        foreach (var definition in PreviewDemoDefinitions.Orders())
        {
            var customer = customers[definition.CustomerEmail];
            var address = customer.Addresses[0];
            var delivery = definition.Fulfillment == OrderFulfillment.Delivery;
            clock.UtcNow = definition.HoursAgo > 0 ? seedNow.AddHours(-definition.HoursAgo) : At(definition.DaysAgo, 11);
            var shipping = delivery ? Rate(store.Shipping, address.GovernorateCode) : new ShippingRate(0m, null);
            var payment = methods.Contains(definition.Payment) ? definition.Payment : methods[0];
            var policy = new CheckoutPolicy(customerIdsByEmail[definition.CustomerEmail],
                terms.VersionNumber.ToString(CultureInfo.InvariantCulture), shipping,
                subtotal => store.Payment.UpfrontFor(subtotal, shipping.Fee, pickup: false).Total, methods,
                store.Controls.MinimumOrderAmount);
            var request = new CheckoutRequest(new OrderCustomer(customer.Name, customer.Phone, false, customer.Email),
                definition.Fulfillment,
                delivery ? new OrderAddress(address.GovernorateCode, address.City, address.Street, address.Building, null) : null,
                payment, Lines(definition.Lines), TermsAccepted: true);

            var order = (await Orders.CreateOnlineAsync(request, policy, cancellationToken)).Order;
            if (definition.Status == OrderStatus.Cancelled)
            {
                clock.UtcNow = clock.UtcNow.AddDays(1);
                await Statuses.UpdateStatusAsync(order.Id, new OrderStatusRequest(OrderStatus.Cancelled, null, order.RowVersion),
                    ownerId, reservation, invoice, cancellationToken);
                created++;
                continue;
            }
            if (definition.Status == OrderStatus.New)
            {
                created++;
                continue;
            }

            var steps = delivery
                ? new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Delivered }
                : new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivered };
            var target = definition.Status == OrderStatus.Completed ? steps.Length - 1 : Array.IndexOf(steps, definition.Status);
            var paidMethod = order.PaymentMethod == OrderPaymentMethod.CashOnDelivery ? OrderPaymentMethod.Cash : order.PaymentMethod;
            for (var index = 0; index <= target; index++)
            {
                if (index > 0) clock.UtcNow = clock.UtcNow.AddHours(8);
                if (steps[index] == OrderStatus.Preparing && order.UpfrontDue > 0)
                    order = await Payments.AddPaymentAsync(order.Id,
                        new OrderPaymentRequest(paidMethod, order.UpfrontDue, null, order.RowVersion), ownerId, cancellationToken);
                order = await Statuses.UpdateStatusAsync(order.Id, new OrderStatusRequest(steps[index], null, order.RowVersion),
                    ownerId, reservation, invoice, cancellationToken);
            }
            // A completed pickup order was paid at the store, like a delivery order is paid on delivery.
            var paysNow = definition.Status == OrderStatus.Completed || (definition.Status == OrderStatus.Delivered && order.PaymentRequired);
            if (paysNow && order.Remaining > 0)
                order = await Payments.AddPaymentAsync(order.Id,
                    new OrderPaymentRequest(paidMethod, order.Remaining, null, order.RowVersion), ownerId, cancellationToken);
            if (definition.Status == OrderStatus.Completed)
                await Statuses.CompleteOrderAsync(order.Id, new CompleteOrderRequest(order.RowVersion, null), ownerId, cancellationToken);
            created++;
        }
        return created;
    }

    /// <summary>Five paid store sales, handed over and completed like the real POS flow; their four used pieces are what makes the four demo items sold out.</summary>
    private async Task<int> SeedPosAsync(CancellationToken cancellationToken)
    {
        var phones = PreviewDemoDefinitions.Sales().Select(sale => sale.Phone).ToArray();
        await using var db = await retailFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Orders.AsNoTracking().AnyAsync(
                order => order.Channel == OrderChannel.Store && phones.Contains(order.PhoneNumber), cancellationToken))
            return 0;

        var store = await Settings.GetAdminAsync(cancellationToken);
        var reservation = new ReservationPolicy(store.Sales.ReservationHours, store.Sales.ReservationAutoRelease);
        var invoice = new InvoicePolicy(store.Invoice.NumberPrefix, ownerName, businessZone);
        var policy = new PosPolicy(store.Sales.PosMaxDiscountPercent, true, reservation, code => Rate(store.Shipping, code));
        var created = 0;
        foreach (var sale in PreviewDemoDefinitions.Sales())
        {
            clock.UtcNow = At(sale.DaysAgo, 13);
            var request = new PosSaleRequest(new OrderCustomer(sale.CustomerName, sale.Phone), OrderFulfillment.Pickup, null,
                Lines(sale.Lines));
            var preview = await Pos.PreviewAsync(request, policy, false, cancellationToken);
            var order = await Pos.SellAsync(request with
            {
                Payments = [new PosPaymentRequest(sale.Payment, preview.Total, null)],
                Confirm = true
            }, policy, ownerId, cancellationToken);
            clock.UtcNow = clock.UtcNow.AddHours(1);
            order = await Statuses.UpdateStatusAsync(order.Id, new OrderStatusRequest(OrderStatus.Preparing, null, order.RowVersion),
                ownerId, reservation, invoice, cancellationToken);
            clock.UtcNow = clock.UtcNow.AddHours(1);
            order = await Statuses.UpdateStatusAsync(order.Id, new OrderStatusRequest(OrderStatus.Delivered, null, order.RowVersion),
                ownerId, reservation, invoice, cancellationToken);
            clock.UtcNow = clock.UtcNow.AddHours(1);
            await Statuses.CompleteOrderAsync(order.Id, new CompleteOrderRequest(order.RowVersion, null), ownerId, cancellationToken);
            created++;
        }
        return created;
    }

    /// <summary>Six general overhead expenses paid from the cash drawer (the demo stock sales fund it first).</summary>
    private async Task<int> SeedExpensesAsync(CancellationToken cancellationToken)
    {
        await using var db = await retailFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Expenses.AsNoTracking().AnyAsync(
                expense => expense.Note != null && expense.Note.StartsWith(PreviewDemoDefinitions.ExpenseNotePrefix),
                cancellationToken))
            return 0;

        var cash = (await Money.ListAsync(cancellationToken)).First(account => account.Kind == MoneyAccountKind.Cash);
        var definitions = PreviewDemoDefinitions.Expenses();
        foreach (var expense in definitions)
        {
            clock.UtcNow = At(expense.DaysAgo, 12);
            await Expenses.CreateExpenseAsync(new ExpenseCreateRequest(today.AddDays(-expense.DaysAgo), expense.Amount,
                expense.Category, ExpenseClassification.GeneralOverhead,
                PreviewDemoDefinitions.ExpenseNotePrefix + expense.Note, null, null, null, cash.Id), ownerId, cancellationToken);
        }
        return definitions.Length;
    }

    private List<OrderLineRequest> Lines(IEnumerable<PreviewDemoLine> lines) =>
        lines.Select(line => new OrderLineRequest(ItemId(line.Key), null, line.Quantity)).ToList();

    private Guid ItemId(string key) => itemIdsByKey.TryGetValue(key, out var id)
        ? id
        : throw new InvalidOperationException($"Demo stock item '{key}' is missing.");

    private static List<string> PaymentMethods(PaymentSettings payment)
    {
        var methods = new List<string>();
        if (payment.CashOnDeliveryEnabled) methods.Add(OrderPaymentMethod.CashOnDelivery);
        if (payment.InstaPayEnabled) methods.Add(OrderPaymentMethod.InstaPay);
        if (payment.WalletEnabled) methods.Add(OrderPaymentMethod.Wallet);
        return methods;
    }
}

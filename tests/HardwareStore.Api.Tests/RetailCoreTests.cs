using HardwareStore.Modules.Retail;
using static HardwareStore.Api.Tests.RetailReceipts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class RetailCorePostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();

    private static ReceiptLine Line(Guid productId, string name, decimal cost, decimal? standalone = null, Guid? partnerId = null) =>
        new(name, "New", 1, cost, standalone ?? 0m, ProductId: productId, PartnerId: partnerId);

    private static OrderLineRequest Sale(StockItemAdminView item, decimal price, int quantity = 1) =>
        RetailSales.StockLine(item.Id, quantity, price);

    /// <summary>A pending store order (pickup, no payment yet) that the test then moves through the workflow.</summary>
    private static Task<OrderView> NewOrderAsync(RetailTestServices retail, decimal discount = 0m, params OrderLineRequest[] lines) =>
        retail.PosAsync(RetailSales.Sale(lines, discount: discount));

    private static async Task<OrderView> DeliverAsync(RetailTestServices retail, OrderView order, bool paid = true, bool waived = false)
    {
        var path = order.FulfillmentMethod == OrderFulfillment.Delivery
            ? new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Delivered }
            : [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivered];
        foreach (var status in path)
            order = await retail.StatusAsync(order, status);
        if (waived)
            order = await retail.Payments.WaivePaymentAsync(order.Id, new WaivePaymentRequest("Owner-approved waiver", order.RowVersion), Actor);
        else if (paid)
            order = await retail.Payments.UpdatePaymentAsync(order.Id, new PaymentStatusRequest(PaymentStatus.Paid, "cash", order.RowVersion), Actor);
        return order;
    }

    private static async Task<OrderView> CompleteAsync(RetailTestServices retail, OrderView delivered) =>
        await retail.OrderStatus.CompleteOrderAsync(delivered.Id, new CompleteOrderRequest(delivered.RowVersion, "delivered"), Actor);

    [Fact]
    public async Task Receive_build_sell_and_partner_accrual_are_atomic_and_idempotent()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        await retail.FundAsync(1000m);
        var productCpu = Guid.NewGuid();
        var productMb = Guid.NewGuid();
        var partnerA = await retail.CreateAsync("Partner A", 10m);

        var units = await retail.ReceiveUnitsAsync(Receipt(
            Guid.NewGuid(), "Initial receipt",
            [
                Line(productCpu, "Test CPU", 1000m, 1000m, partnerA.Id),
                Line(productMb, "Test Motherboard", 500m, 500m, partnerA.Id)
            ]), Actor);
        Assert.Equal(2, units.Count);
        Assert.All(units, unit => Assert.Equal(UnitStatus.Available, unit.Status));

        var assembly = await retail.Assemblies.CreateAssemblyAsync(
            new AssemblyCreateRequest("Gaming Build", units.Select(unit => unit.Id).ToList()));
        Assert.Equal(1500m, assembly.ComponentCost!.Value);
        Assert.Equal(66.67m, assembly.Units.Single(unit => unit.UnitId == units[0].Id).AllocationPercent);
        Assert.Equal(33.33m, assembly.Units.Single(unit => unit.UnitId == units[1].Id).AllocationPercent);

        var priced = await retail.Assemblies.UpdateAssemblyAsync(assembly.Id,
            new AssemblyUpdateRequest("Gaming Build", "تجميع", "Gaming Build", 2200m, assembly.RowVersion));
        var published = await retail.Assemblies.SetAssemblyPublishedAsync(priced.Id, true, priced.RowVersion);
        Assert.Equal(AssemblyStatus.Published, published.Status);
        Assert.Single(await retail.Assemblies.ListPublishedAssembliesAsync());

        var order = await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.BuildLine(assembly.Id)]));
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Equal(2200m, order.Subtotal);

        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed, "stock checked");
        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
        var assembliesAfter = await retail.Assemblies.ListAssembliesAsync();
        Assert.Equal(AssemblyStatus.Sold, assembliesAfter.Single(item => item.Id == assembly.Id).Status);

        var delivered = await DeliverAsync(retail, confirmed);
        var completed = await CompleteAsync(retail, delivered);
        Assert.Equal(OrderStatus.Completed, completed.Status);

        var earnings = await retail.PartnerLedger.ListEarningsAsync(partnerA.Id);
        Assert.Equal(2, earnings.Count);
        Assert.Equal(70m, earnings.Sum(item => item.Amount));
        Assert.Equal(700m, earnings.Sum(item => item.GrossProfit));
        Assert.Equal(1500m, earnings.Sum(item => item.CostAmount));
        Assert.Equal(2200m, earnings.Sum(item => item.RevenueAmount));

        var settlement = await retail.PartnerLedger.PayoutAsync(partnerA.Id, new PartnerPayoutRequest(70m, "Cash", "REF-1", "Settled"), Actor);
        Assert.Equal(70m, settlement.Amount);
        Assert.All(await retail.PartnerLedger.ListEarningsAsync(partnerA.Id), item => Assert.Equal(EarningStatus.Paid, item.Status));

        // A sold unit cannot be sold or assembled again.
        await Assert.ThrowsAsync<RetailException>(() =>
            retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Duplicate", [units[0].Id])));
    }

    [Fact]
    public async Task Confirming_without_enough_stock_is_rejected_without_partial_deduction()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line(Guid.NewGuid(), "Only One", 100m), Line(Guid.NewGuid(), "Plenty", 100m)]), Actor);
        var (scarce, other) = (items[0], items[1]);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => NewOrderAsync(retail, 0m, Sale(scarce, 150m, 3))));

        var first = await NewOrderAsync(retail, 0m, Sale(scarce, 150m));
        var second = await NewOrderAsync(retail, 0m, Sale(other, 150m), Sale(scarce, 150m));
        await retail.StatusAsync(first, OrderStatus.Confirmed);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(second, OrderStatus.Confirmed)));
        Assert.Equal(1, (await retail.StockItems.GetAsync(other.Id, true, true)).AvailableCount);
    }

    [Fact]
    public async Task Net_profit_uses_all_units_and_the_order_discount()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var partnerB = await retail.CreateAsync("Partner B", 10m);
        var fan = await retail.ReceiveItemAsync(new ReceiptLine("Fan", "New", 3, 100m, 150m, PartnerId: partnerB.Id));

        var order = await NewOrderAsync(retail, 30m, Sale(fan, 150m, 3));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        var item = Assert.Single(confirmed.Items);
        Assert.Equal(3, item.Components.Count);
        Assert.Equal(450m, item.Components.Sum(component => component.RevenueAmount));
        Assert.Equal(300m, item.Components.Sum(component => component.CostAmount));
        Assert.Equal(30m, item.Components.Sum(component => component.DiscountAmount));
        Assert.Equal(120m, item.NetProfit);

        var completed = await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var earnings = await retail.PartnerLedger.ListEarningsAsync(partnerB.Id);
        Assert.Equal(3, earnings.Count);
        Assert.Equal(120m, earnings.Sum(earning => earning.GrossProfit));
        Assert.Equal(12m, earnings.Sum(earning => earning.Amount));
    }

    [Fact]
    public async Task Loss_making_sale_is_zero_unless_loss_sharing_is_explicit()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var partnerC = await retail.CreateAsync("Partner C", 10m);
        var partnerSharing = await retail.CreateAsync("Partner C Sharing", 10m, lossSharing: true);
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line(Guid.NewGuid(), "Loss Item", 200m, null, partnerC.Id), Line(Guid.NewGuid(), "Loss Item 2", 200m, null, partnerSharing.Id)]), Actor);

        var order = await NewOrderAsync(retail, 0m, Sale(items[0], 150m));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var earning = Assert.Single(await retail.PartnerLedger.ListEarningsAsync(partnerC.Id));
        Assert.Equal(-50m, earning.GrossProfit);
        Assert.Equal(0m, earning.Amount);

        var second = await NewOrderAsync(retail, 0m, Sale(items[1], 150m));
        var secondConfirmed = await retail.StatusAsync(second, OrderStatus.Confirmed);
        await CompleteAsync(retail, await DeliverAsync(retail, secondConfirmed));
        var lossEarning = Assert.Single(await retail.PartnerLedger.ListEarningsAsync(partnerSharing.Id));
        Assert.Equal(-5m, lossEarning.Amount);
        Assert.Equal(EarningStatus.Reversed, lossEarning.Status);
        var recoverable = Assert.Single(await retail.PartnerLedger.ListAdjustmentsAsync(partnerSharing.Id));
        Assert.Equal(-5m, recoverable.Amount);
    }

    [Fact]
    public async Task Assembly_allocations_are_editable_validated_and_used_for_partner_profit()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productCpu = Guid.NewGuid();
        var productGpu = Guid.NewGuid();
        var partnerD = await retail.CreateAsync("Partner D", 10m);
        var units = await retail.ReceiveUnitsAsync(Receipt(Guid.NewGuid(), null,
            [Line(productCpu, "CPU", 1000m, 1000m, partnerD.Id), Line(productGpu, "GPU", 500m, 500m, partnerD.Id)]), Actor);
        var assembly = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Allocated Build", units.Select(unit => unit.Id).ToList()));

        var invalid = await Assert.ThrowsAsync<RetailException>(() => retail.Assemblies.SetAssemblyAllocationsAsync(assembly.Id,
            new AssemblyAllocationRequest(assembly.RowVersion,
                [new AssemblyAllocationInput(units[0].Id, 70m), new AssemblyAllocationInput(units[1].Id, 40m)])));
        Assert.Equal(400, invalid.StatusCode);

        var allocated = await retail.Assemblies.SetAssemblyAllocationsAsync(assembly.Id,
            new AssemblyAllocationRequest(assembly.RowVersion,
                [new AssemblyAllocationInput(units[0].Id, 70m), new AssemblyAllocationInput(units[1].Id, 30m)]));
        Assert.Equal(70m, allocated.Units.Single(unit => unit.UnitId == units[0].Id).AllocationPercent);

        var priced = await retail.Assemblies.UpdateAssemblyAsync(allocated.Id,
            new AssemblyUpdateRequest("Allocated Build", null, "Allocated Build", 2000m, allocated.RowVersion));
        await retail.Assemblies.SetAssemblyPublishedAsync(priced.Id, true, priced.RowVersion);

        var order = await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.BuildLine(priced.Id)]));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        var components = confirmed.Items.Single().Components;
        Assert.Equal(1400m, components.Single(item => item.UnitId == units[0].Id).RevenueAmount);
        Assert.Equal(600m, components.Single(item => item.UnitId == units[1].Id).RevenueAmount);
        Assert.Equal(500m, components.Sum(item => item.NetProfit));

        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var earnings = await retail.PartnerLedger.ListEarningsAsync(partnerD.Id);
        Assert.Equal(40m, earnings.Single(item => item.UnitId == units[0].Id).Amount);
        Assert.Equal(10m, earnings.Single(item => item.UnitId == units[1].Id).Amount);
    }

    [Fact]
    public async Task Return_before_payout_reverses_accrual_and_waits_for_inspection()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        await retail.FundAsync(1000m);
        var productId = Guid.NewGuid();
        var partnerE = await retail.CreateAsync("Partner E", 10m);
        var returnable = Assert.Single(await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line(productId, "Returnable", 100m, null, partnerE.Id)]), Actor));
        var order = await NewOrderAsync(retail, 0m, Sale(returnable, 200m));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        var completed = await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var unitId = completed.Items.Single().Components.Single().UnitId;

        var entity = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(
            completed.Id, completed.Items.Single().Id, unitId, 200m, "Customer changed mind"), Actor);
        Assert.Equal(ReturnStatus.PendingInspection, entity.Status);
        Assert.Contains(await retail.Units.ListUnitsAsync(UnitStatus.PendingInspection, productId),
            unit => unit.Id == unitId);

        var inspected = await retail.Returns.InspectReturnAsync(entity.Id,
            new ReturnInspectRequest("Used", "Opened but complete", entity.RowVersion), Actor);
        Assert.Equal(ReturnStatus.Inspected, inspected.Status);
        Assert.Equal("Used", inspected.InspectionCondition);
        var restocked = (await retail.Units.ListUnitsAsync(UnitStatus.Available, productId)).Single(unit => unit.Id == unitId);
        Assert.Equal("Used", restocked.InspectionCondition);

        var earning = Assert.Single(await retail.PartnerLedger.ListEarningsAsync(partnerE.Id));
        Assert.Equal(EarningStatus.Reversed, earning.Status);
        Assert.Equal(0m, earning.PaidAmount);
        Assert.Empty(await retail.PartnerLedger.ListAdjustmentsAsync(partnerE.Id));

        var replay = await Assert.ThrowsAsync<RetailException>(() => retail.Returns.CreateReturnAsync(
            new ReturnCreateRequest(completed.Id, completed.Items.Single().Id, unitId, 200m, "again"), Actor));
        Assert.Equal(409, replay.StatusCode);
    }

    [Fact]
    public async Task Return_after_payout_creates_recoverable_balance_and_offsets_future_earnings()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        await retail.FundAsync(1000m);
        var productId = Guid.NewGuid();
        var partnerF = await retail.CreateAsync("Partner F", 10m);
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line(productId, "Unit One", 100m, null, partnerF.Id), Line(productId, "Unit Two", 100m, null, partnerF.Id)]), Actor);

        var first = await NewOrderAsync(retail, 0m, Sale(items[0], 200m));
        var firstConfirmed = await retail.StatusAsync(first, OrderStatus.Confirmed);
        var firstCompleted = await CompleteAsync(retail, await DeliverAsync(retail, firstConfirmed));
        await retail.PartnerLedger.PayoutAsync(partnerF.Id, new PartnerPayoutRequest(10m, "Cash", null, null), Actor);
        var returnedUnit = firstCompleted.Items.Single().Components.Single().UnitId;

        var entity = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(
            firstCompleted.Id, firstCompleted.Items.Single().Id, returnedUnit, 200m, "Defective"), Actor);
        await retail.Returns.InspectReturnAsync(entity.Id, new ReturnInspectRequest("New", "Resellable", entity.RowVersion), Actor);

        var adjustment = Assert.Single(await retail.PartnerLedger.ListAdjustmentsAsync(partnerF.Id));
        Assert.Equal(-10m, adjustment.Amount);
        Assert.Equal(-10m, adjustment.RemainingAmount);

        var second = await NewOrderAsync(retail, 0m, Sale(items[1], 300m));
        var secondConfirmed = await retail.StatusAsync(second, OrderStatus.Confirmed);
        await CompleteAsync(retail, await DeliverAsync(retail, secondConfirmed));

        var future = (await retail.PartnerLedger.ListEarningsAsync(partnerF.Id)).Single(item => item.Amount == 20m);
        Assert.Equal(10m, future.OffsetAmount);
        Assert.Equal(EarningStatus.Outstanding, future.Status);
        Assert.Equal(AdjustmentStatus.Applied, (await retail.PartnerLedger.ListAdjustmentsAsync(partnerF.Id)).Single().Status);

        var card = (await retail.Partners.ListAsync()).Single(item => item.Id == partnerF.Id);
        Assert.Equal(10m, card.Due);
        Assert.Equal(0m, card.Recoverable);

        var settlement = await retail.PartnerLedger.PayoutAsync(partnerF.Id, new PartnerPayoutRequest(10m, "Cash", null, null), Actor);
        Assert.Equal(10m, settlement.Amount);
        Assert.Equal(EarningStatus.Paid, (await retail.PartnerLedger.ListEarningsAsync(partnerF.Id)).Single(item => item.Amount == 20m).Status);
    }

    [Fact]
    public async Task Completion_requires_delivered_status_and_paid_or_explicitly_waived_payment()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line(Guid.NewGuid(), "Complete Me", 100m, 200m), Line(Guid.NewGuid(), "Complete Me Too", 100m, 200m)]), Actor);
        await retail.PublishAsync(items[1]);

        var required = await NewOrderAsync(retail, 0m, Sale(items[0], 200m));
        var requiredConfirmed = await retail.StatusAsync(required, OrderStatus.Confirmed);
        var requiredDelivered = await DeliverAsync(retail, requiredConfirmed, paid: false);
        var blocked = await Assert.ThrowsAsync<RetailException>(() => CompleteAsync(retail, requiredDelivered));
        Assert.Equal(409, blocked.StatusCode);
        var paid = await retail.Payments.UpdatePaymentAsync(requiredDelivered.Id,
            new PaymentStatusRequest(PaymentStatus.Paid, "cash", requiredDelivered.RowVersion), Actor);
        Assert.Equal(OrderStatus.Completed, (await CompleteAsync(retail, paid)).Status);

        // An online pickup order is paid at the store, so payment is not required up front (the waiver is explicit).
        var notRequired = await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.StockLine(items[1].Id)], OrderFulfillment.Pickup));
        Assert.False(notRequired.PaymentRequired);
        var waivedConfirmed = await retail.StatusAsync(notRequired, OrderStatus.Confirmed);
        var waivedDelivered = await DeliverAsync(retail, waivedConfirmed, paid: false);
        var missingWaiver = await Assert.ThrowsAsync<RetailException>(() => CompleteAsync(retail, waivedDelivered));
        Assert.Equal(409, missingWaiver.StatusCode);
        var waived = await retail.Payments.WaivePaymentAsync(waivedDelivered.Id,
            new WaivePaymentRequest("Goodwill replacement", waivedDelivered.RowVersion), Actor);
        Assert.Equal(PaymentStatus.Waived, waived.PaymentStatus);
        Assert.False(waived.PaymentRequired);
        Assert.Equal("Goodwill replacement", waived.PaymentWaiverReason);
        Assert.Equal(OrderStatus.Completed, (await CompleteAsync(retail, waived)).Status);
        Assert.NotEqual(PaymentStatus.Paid, waived.PaymentStatus);
    }

    [Fact]
    public async Task Unit_snapshot_preserves_purchase_fields_and_masks_costs_for_limited_roles()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        var date = new DateOnly(2026, 11, 1);
        var snapshotPartner = await retail.CreateAsync("Snapshot Partner", 10m);
        await retail.ReceiveUnitsAsync(new ReceiveRequest(Guid.NewGuid(), null,
            new ReceiptSource("QA Supplier", "Supplier", "Component", date),
            [new ReceiptLine("Snapshot CPU", "Used", 1, 900m, 1200m, ProductId: productId, SerialNumber: "SN-SECRET", PartnerPercentOverride: 7.5m, PartnerId: snapshotPartner.Id)]), Actor);

        var full = Assert.Single(await retail.Units.ListUnitsAsync(null, productId, true, true));
        Assert.Equal("Component", full.PurchaseKind);
        Assert.Equal("Supplier", full.SourceType);
        Assert.Equal(date, full.PurchaseDate);
        Assert.Equal("QA Supplier", full.SourceName);
        Assert.Equal("SN-SECRET", full.SerialNumber);
        Assert.Equal(900m, full.AcquisitionCost);
        Assert.Equal(snapshotPartner.Id, full.PartnerId);
        Assert.Equal(7.5m, full.PartnerPercentOverride);

        var limited = Assert.Single(await retail.Units.ListUnitsAsync(null, productId, false, false));
        Assert.Null(limited.AcquisitionCost);
        Assert.Null(limited.PartnerPercentOverride);
        Assert.Null(limited.PartnerId);
        Assert.Null(limited.SourceName);
        Assert.Null(limited.SourceType);
        Assert.Null(limited.SerialNumber);
        Assert.Equal("Snapshot CPU", limited.Name);
        Assert.Equal("Used", limited.Condition);
    }

    [Fact]
    public async Task Multiple_partners_can_own_different_components_in_one_order()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var partnerX = await retail.CreateAsync("Partner X", 100m);
        var partnerY = await retail.CreateAsync("Partner Y", 100m);
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
        [
            Line(Guid.NewGuid(), "Partner CPU", 100m, null, partnerX.Id),
            Line(Guid.NewGuid(), "Partner GPU", 200m, null, partnerY.Id)
        ]), Actor);

        var order = await NewOrderAsync(retail, 0m, Sale(items[0], 300m), Sale(items[1], 500m));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));

        var earnings = await retail.PartnerLedger.ListEarningsAsync();
        Assert.Equal(200m, earnings.Single(item => item.PartnerId == partnerX.Id).Amount);
        Assert.Equal(300m, earnings.Single(item => item.PartnerId == partnerY.Id).Amount);
        Assert.Equal(500m, earnings.Sum(item => item.Amount));
    }

    [Fact]
    public async Task Returns_reverse_each_partners_earnings_after_payout()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        await retail.FundAsync(1000m);
        var partnerA = await retail.CreateAsync("Partner A", 60m);
        var partnerB = await retail.CreateAsync("Partner B", 40m);
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line(Guid.NewGuid(), "Unit A", 100m, null, partnerA.Id), Line(Guid.NewGuid(), "Unit B", 100m, null, partnerB.Id)]), Actor);

        var order = await NewOrderAsync(retail, 0m, Sale(items[0], 300m), Sale(items[1], 300m));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        var completed = await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var earnings = await retail.PartnerLedger.ListEarningsAsync();
        Assert.Equal(120m, earnings.Single(item => item.PartnerId == partnerA.Id).Amount);
        Assert.Equal(80m, earnings.Single(item => item.PartnerId == partnerB.Id).Amount);
        await retail.PartnerLedger.PayoutAsync(partnerA.Id, new PartnerPayoutRequest(120m, "Cash", null, null), Actor);
        await retail.PartnerLedger.PayoutAsync(partnerB.Id, new PartnerPayoutRequest(80m, "Cash", null, null), Actor);

        foreach (var sold in completed.Items)
        {
            var entity = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(
                completed.Id, sold.Id, sold.Components.Single().UnitId, 300m, "Return"), Actor);
            await retail.Returns.InspectReturnAsync(entity.Id, new ReturnInspectRequest("New", "Resellable", entity.RowVersion), Actor);
        }

        var adjustments = await retail.PartnerLedger.ListAdjustmentsAsync();
        Assert.Equal(-120m, adjustments.Single(item => item.PartnerId == partnerA.Id).Amount);
        Assert.Equal(-80m, adjustments.Single(item => item.PartnerId == partnerB.Id).Amount);
        Assert.Equal(2, (await retail.PartnerLedger.ListEarningsAsync()).Count(item => item.Status == EarningStatus.Reversed));
    }

    [Fact]
    public async Task Units_without_a_partner_post_no_earning()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        await retail.CreateAsync("Idle Partner", 50m);
        var item = Assert.Single(await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null, [Line(Guid.NewGuid(), "Own Money", 100m)]), Actor));
        var confirmed = await retail.StatusAsync(await NewOrderAsync(retail, 0m, Sale(item, 300m)), OrderStatus.Confirmed);
        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        Assert.Empty(await retail.PartnerLedger.ListEarningsAsync());
    }

    [Fact]
    public async Task Expenses_stay_separate_and_business_report_reconciles_orders_direct_and_overhead()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        await retail.FundAsync(1000m);
        var reportUnit = Assert.Single(await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line(Guid.NewGuid(), "Report Unit", 100m)]), Actor));
        var order = await NewOrderAsync(retail, 10m, Sale(reportUnit, 300m));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var today = HardwareStore.Common.Domain.BusinessDay.Of(DateTimeOffset.UtcNow, RetailSales.BusinessZone);
        await retail.Expenses.CreateExpenseAsync(new ExpenseCreateRequest(today, 50m, "تغليف", "Direct", null, order.Id, null), Actor);
        await retail.Expenses.CreateExpenseAsync(new ExpenseCreateRequest(today, 100m, "إنترنت", "GeneralOverhead", null, null, null), Actor);

        var report = await retail.Reports.BuildReportAsync(today, today);
        Assert.Equal(290m, report.GrossSales);
        Assert.Equal(10m, report.Discounts);
        Assert.Equal(100m, report.CostOfGoods);
        Assert.Equal(0m, report.ItemDirectExpenses);
        Assert.Equal(190m, report.NetItemProfit);
        Assert.Equal(50m, report.AdditionalDirectExpenses);
        Assert.Equal(100m, report.GeneralOverhead);
        Assert.Equal(40m, report.StoreNetResult);
        Assert.Equal(1, report.CompletedOrders);
        Assert.Equal(2, (await retail.Expenses.ListExpensesAsync(today, today)).Count);
        await Assert.ThrowsAsync<RetailException>(() => retail.Expenses.CreateExpenseAsync(
            new ExpenseCreateRequest(today, -1m, "Bad", "GeneralOverhead", null, null, null), Actor));
    }

    [Fact]
    public async Task Order_detail_is_owner_scoped_and_admin_component_serials_are_permission_safe()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Detail Unit", "New", 1, 100m, 300m, SerialNumber: "SERIAL-DETAIL"));
        await retail.PublishAsync(item);
        var customerId = Guid.NewGuid();
        var order = await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.StockLine(item.Id)]), RetailSales.Policy(customerId));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);

        var own = await retail.Orders.GetMyOrderAsync(order.Id, customerId);
        Assert.Equal(order.Id, own.Id);
        var foreign = await Assert.ThrowsAsync<RetailException>(() => retail.Orders.GetMyOrderAsync(order.Id, Guid.NewGuid()));
        Assert.Equal(404, foreign.StatusCode);

        var detail = await retail.Orders.GetAdminOrderDetailAsync(confirmed.Id, true);
        Assert.Single(detail.Units);
        Assert.Equal("SERIAL-DETAIL", detail.Units[0].SerialNumber);
        var masked = await retail.Orders.GetAdminOrderDetailAsync(confirmed.Id, false);
        Assert.Null(masked.Units[0].SerialNumber);
        Assert.Equal("Detail Unit", masked.Units[0].Name);
    }

    [Fact]
    public async Task Fulfillment_payment_matrix_terms_and_thumbnail_snapshots_are_server_enforced()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var item = await retail.PublishedItemAsync("Matrix Unit", 3, 300m);
        var line = new[] { RetailSales.StockLine(item.Id) };

        var delivery = await retail.CheckoutAsync(RetailSales.Checkout(line));
        Assert.Equal("Delivery", delivery.FulfillmentMethod);
        Assert.Equal("CashOnDelivery", delivery.PaymentMethod);
        Assert.True(delivery.PaymentRequired);
        Assert.Equal($"items/{item.Code}/0-t.webp", delivery.Items[0].ThumbnailStorageKeySnapshot);
        await using (var db = database.CreateRetailContext())
        {
            var stored = await db.Orders.SingleAsync(order => order.Id == delivery.Id);
            Assert.NotNull(stored.TermsAcceptedAtUtc);
            Assert.Equal("3", stored.TermsPolicyVersion);
        }

        var pickup = await retail.CheckoutAsync(RetailSales.Checkout(line, OrderFulfillment.Pickup, OrderPaymentMethod.InstaPay));
        Assert.Equal("Pickup", pickup.FulfillmentMethod);
        Assert.Equal("InstaPay", pickup.PaymentMethod);
        Assert.False(pickup.PaymentRequired);
        Assert.Null(pickup.Address);

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(RetailSales.Checkout(line, payment: "CashOnHandover"))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(
            RetailSales.Checkout(line) with { Address = null })));
    }

    [Fact]
    public async Task Return_rejection_restores_the_sold_unit_without_reversing_the_sale()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        var partnerG = await retail.CreateAsync("Partner G", 10m);
        var rejectMe = Assert.Single(await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line(productId, "Reject Me", 100m, null, partnerG.Id)]), Actor));
        var order = await NewOrderAsync(retail, 0m, Sale(rejectMe, 200m));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        var completed = await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var unitId = completed.Items.Single().Components.Single().UnitId;
        var entity = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(
            completed.Id, completed.Items.Single().Id, unitId, 200m, "Claimed defect"), Actor);

        var rejected = await retail.Returns.RejectReturnAsync(entity.Id, new ReturnRejectRequest("No defect found", entity.RowVersion), Actor);
        Assert.Equal(ReturnStatus.Rejected, rejected.Status);
        Assert.Contains(await retail.Units.ListUnitsAsync(UnitStatus.Sold, productId), unit => unit.Id == unitId);
        Assert.Equal(EarningStatus.Outstanding, Assert.Single(await retail.PartnerLedger.ListEarningsAsync(partnerG.Id)).Status);
    }

    [Fact]
    public async Task Concurrent_confirmation_cannot_sell_the_same_unit_twice()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        var single = Assert.Single(await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), "Concurrency receipt",
            [Line(productId, "Single Unit", 100m)]), Actor));

        var first = await NewOrderAsync(retail, 0m, Sale(single, 300m));
        var second = await NewOrderAsync(retail, 0m, Sale(single, 300m));

        async Task<bool> TryConfirm(OrderView order)
        {
            try
            {
                await retail.StatusAsync(order, OrderStatus.Confirmed, "concurrent");
                return true;
            }
            catch (RetailException exception) when (exception.StatusCode == 409)
            {
                return false;
            }
        }

        var outcomes = await Task.WhenAll(TryConfirm(first), TryConfirm(second));
        Assert.Equal(1, outcomes.Count(value => value));

        await using var db = database.CreateRetailContext();
        Assert.Single(await db.Units.Where(unit => unit.ProductId == productId && unit.Status == UnitStatus.Sold).ToListAsync());
        Assert.Single(await db.OrderItemComponents.ToListAsync());
    }

    [Fact]
    public async Task Concurrent_orders_both_succeed_when_stock_is_sufficient()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        var lot = Assert.Single(await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), "Two units",
            [new ReceiptLine("Unit", "New", 2, 100m, 0m, ProductId: productId)]), Actor));

        var first = await NewOrderAsync(retail, 0m, Sale(lot, 300m));
        var second = await NewOrderAsync(retail, 0m, Sale(lot, 300m));

        var outcomes = await Task.WhenAll(retail.StatusAsync(first, OrderStatus.Confirmed), retail.StatusAsync(second, OrderStatus.Confirmed));

        Assert.All(outcomes, order => Assert.Equal(OrderStatus.Confirmed, order.Status));
        await using var db = database.CreateRetailContext();
        Assert.Equal(2, await db.Units.CountAsync(unit => unit.ProductId == productId && unit.Status == UnitStatus.Sold));
        Assert.Equal(2, await db.OrderItemComponents.CountAsync());
    }

    [Fact]
    public async Task Concurrent_confirmation_cannot_sell_the_same_assembly_twice()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        var units = await retail.ReceiveUnitsAsync(Receipt(Guid.NewGuid(), "Assembly units",
            [Line(productId, "Assembly CPU", 400m), Line(productId, "Assembly Board", 300m)]), Actor);
        var assembly = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Concurrent Build", units.Select(unit => unit.Id).ToList()));
        var priced = await retail.Assemblies.UpdateAssemblyAsync(assembly.Id,
            new AssemblyUpdateRequest("Concurrent Build", "بناء", "Concurrent Build", 1200m, assembly.RowVersion));
        await retail.Assemblies.SetAssemblyPublishedAsync(priced.Id, true, priced.RowVersion);

        var first = await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.BuildLine(assembly.Id)], phone: "01000000001"));
        var second = await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.BuildLine(assembly.Id)], phone: "01000000002"));

        async Task<bool> TryConfirm(OrderView order)
        {
            try
            {
                await retail.StatusAsync(order, OrderStatus.Confirmed, "concurrent");
                return true;
            }
            catch (RetailException exception) when (exception.StatusCode == 409)
            {
                return false;
            }
        }

        var outcomes = await Task.WhenAll(TryConfirm(first), TryConfirm(second));
        Assert.Equal(1, outcomes.Count(value => value));
        await using var db = database.CreateRetailContext();
        Assert.Equal(AssemblyStatus.Sold, (await db.Assemblies.SingleAsync(item => item.Id == assembly.Id)).Status);
        Assert.All(await db.Units.Where(unit => units.Select(u => u.Id).Contains(unit.Id)).ToListAsync(),
            unit => Assert.Equal(UnitStatus.Sold, unit.Status));
        Assert.Equal(2, await db.OrderItemComponents.CountAsync());
    }

    [Fact]
    public async Task Cancelling_a_confirmed_build_returns_components_to_the_build_not_standalone_stock()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), "Cancel build",
        [
            Line(Guid.NewGuid(), "Cancel CPU", 400m),
            Line(Guid.NewGuid(), "Cancel Board", 300m)
        ]), Actor);
        var units = items.SelectMany(item => item.Units).ToList();
        var assembly = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Cancel Build", units.Select(unit => unit.Id).ToList()));
        var priced = await retail.Assemblies.UpdateAssemblyAsync(assembly.Id,
            new AssemblyUpdateRequest("Cancel Build", "بناء", "Cancel Build", 1200m, assembly.RowVersion));
        await retail.Assemblies.SetAssemblyPublishedAsync(priced.Id, true, priced.RowVersion);

        var order = await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.BuildLine(assembly.Id)]));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        var cancelled = await retail.StatusAsync(confirmed, OrderStatus.Cancelled, "customer request");
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);

        await using var db = database.CreateRetailContext();
        var unitIds = units.Select(unit => unit.Id).ToList();
        Assert.All(await db.Units.Where(unit => unitIds.Contains(unit.Id)).ToListAsync(),
            unit => Assert.Equal(UnitStatus.InAssembly, unit.Status));
        Assert.Equal(AssemblyStatus.Published, (await db.Assemblies.SingleAsync(item => item.Id == assembly.Id)).Status);

        // The parts stay inside the build, so they cannot be sold on their own.
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => NewOrderAsync(retail, 0m, Sale(items[0], 300m))));
    }

    [Fact]
    public async Task Removing_swapping_and_disassembling_return_units_and_rebalance_allocations()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var units = await retail.ReceiveUnitsAsync(Receipt(Guid.NewGuid(), null,
        [
            Line(Guid.NewGuid(), "Edit CPU", 500m, 600m),
            Line(Guid.NewGuid(), "Edit GPU", 200m, 300m),
            Line(Guid.NewGuid(), "Edit Fan", 50m, 100m),
            Line(Guid.NewGuid(), "Edit PSU", 80m, 100m)
        ]), Actor);
        var (cpu, gpu, fan, psu) = (units[0].Id, units[1].Id, units[2].Id, units[3].Id);
        var first = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("First", [cpu, gpu, fan]));
        var second = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Second", [psu]));
        Assert.Equal(10m, first.Units.Single(unit => unit.UnitId == fan).AllocationPercent);

        var stale = await Assert.ThrowsAsync<RetailException>(() => retail.Parts.RemovePartAsync(first.Id, fan, new AssemblyRemovePartRequest(first.RowVersion - 1)));
        Assert.Equal(409, stale.StatusCode);
        var removed = Assert.Single(await retail.Parts.RemovePartAsync(first.Id, fan, new AssemblyRemovePartRequest(first.RowVersion)));
        Assert.Equal(2, removed.Units.Count);
        Assert.Equal(100m, removed.Units.Sum(unit => unit.AllocationPercent));
        Assert.Equal(66.67m, removed.Units.Single(unit => unit.UnitId == cpu).AllocationPercent);
        Assert.Equal(UnitStatus.Available, (await retail.Units.ListUnitsAsync(null, null)).Single(unit => unit.Id == fan).Status);

        var sameAssembly = await Assert.ThrowsAsync<RetailException>(() => retail.Parts.RemovePartAsync(first.Id, gpu,
            new AssemblyRemovePartRequest(removed.RowVersion, first.Id, removed.RowVersion)));
        Assert.Equal(400, sameAssembly.StatusCode);
        var moved = await retail.Parts.RemovePartAsync(first.Id, gpu, new AssemblyRemovePartRequest(removed.RowVersion, second.Id, second.RowVersion));
        Assert.Equal(100m, Assert.Single(moved[0].Units).AllocationPercent);
        Assert.Equal(2, moved[1].Units.Count);
        Assert.Equal(100m, moved[1].Units.Sum(unit => unit.AllocationPercent));
        Assert.Equal(75m, moved[1].Units.Single(unit => unit.UnitId == gpu).AllocationPercent);
        Assert.Equal(25m, moved[1].Units.Single(unit => unit.UnitId == psu).AllocationPercent);

        var disassembled = await retail.Assemblies.DisassembleAsync(second.Id, moved[1].RowVersion);
        Assert.Equal(AssemblyStatus.Disassembled, disassembled.Status);
        Assert.Empty(disassembled.Units);

        var stock = await retail.Units.ListUnitsAsync(null, null);
        Assert.All(stock.Where(unit => unit.Id == fan || unit.Id == gpu || unit.Id == psu), unit =>
        {
            Assert.Equal(UnitStatus.Available, unit.Status);
            Assert.Null(unit.AssemblyId);
        });
        var stillBuilt = stock.Single(unit => unit.Id == cpu);
        Assert.Equal(UnitStatus.InAssembly, stillBuilt.Status);
        Assert.Equal(first.Id, stillBuilt.AssemblyId);
    }
}

using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class RetailCorePostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();

    private static ReceiptLine Line(Guid productId, string name, string sku, decimal cost, decimal? standalone = null, decimal? partnerPercent = null) =>
        new(productId, null, name, sku, "New", null, cost, 1, partnerPercent, null, standalone);

    private static OrderLine OrderLineFor(Guid productId, string name, string sku, int quantity, decimal price,
        decimal? discount = null, decimal? expense = null) =>
        new(productId, null, name, sku, quantity, price, discount, expense);

    private async Task<PartnerAgreement> AgreementAsync(RetailTestServices retail, string name, decimal percent, bool lossSharing = false) =>
        await retail.Agreements.UpsertAgreementAsync(new PartnerAgreementRequest(
            name, percent, true, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), null, lossSharing));

    private static async Task<OrderView> DeliverAsync(RetailTestServices retail, OrderView order, bool paid = true, bool waived = false)
    {
        foreach (var status in new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Delivered })
        {
            order = await retail.OrderStatus.UpdateStatusAsync(order.Id, new OrderStatusRequest(status, null, order.RowVersion), Actor);
        }
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
        var productCpu = Guid.NewGuid();
        var productMb = Guid.NewGuid();

        var units = await retail.Units.ReceiveAsync(new ReceiveRequest(
            Guid.NewGuid(), "Initial receipt",
            [
                Line(productCpu, "Test CPU", "CPU-T1", 1000m, 1000m),
                Line(productMb, "Test Motherboard", "MB-T1", 500m, 500m)
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

        await AgreementAsync(retail, "Partner A", 10m);

        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [new OrderLine(null, assembly.Id, "Gaming Build", null, 1, 2200m)], LeadSource.Website), null);
        Assert.Equal(OrderStatus.New, order.Status);

        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, "stock checked", order.RowVersion), Actor);
        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
        var assembliesAfter = await retail.Assemblies.ListAssembliesAsync();
        Assert.Equal(AssemblyStatus.Sold, assembliesAfter.Single(item => item.Id == assembly.Id).Status);

        var delivered = await DeliverAsync(retail, confirmed);
        var completed = await CompleteAsync(retail, delivered);
        Assert.Equal(OrderStatus.Completed, completed.Status);

        var earnings = await retail.PartnerLedger.ListEarningsAsync("Partner A");
        Assert.Equal(2, earnings.Count);
        Assert.Equal(70m, earnings.Sum(item => item.Amount));
        Assert.Equal(700m, earnings.Sum(item => item.GrossProfit));
        Assert.Equal(1500m, earnings.Sum(item => item.CostAmount));
        Assert.Equal(2200m, earnings.Sum(item => item.RevenueAmount));

        var settlement = await retail.PartnerLedger.SettleAsync(new SettlementRequest("Partner A", 70m, "Cash", "REF-1", "Settled"), Actor);
        Assert.Equal(70m, settlement.Amount);
        Assert.All(await retail.PartnerLedger.ListEarningsAsync("Partner A"), item => Assert.Equal(EarningStatus.Paid, item.Status));

        // A sold unit cannot be sold or assembled again.
        await Assert.ThrowsAsync<RetailException>(() =>
            retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Duplicate", [units[0].Id])));
    }

    [Fact]
    public async Task Confirming_without_enough_stock_is_rejected_without_partial_deduction()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(
            Guid.NewGuid(), null, [Line(productId, "Only One", "ONE-1", 100m)]), Actor);
        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Only One", "ONE-1", 3, 150m)], LeadSource.Website), null);
        var error = await Assert.ThrowsAsync<RetailException>(() =>
            retail.OrderStatus.UpdateStatusAsync(order.Id, new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor));
        Assert.Equal(409, error.StatusCode);
        Assert.Single((await retail.Units.ListUnitsAsync(UnitStatus.Available, productId)).Where(unit => unit.ProductId == productId));
    }

    [Fact]
    public async Task Net_profit_uses_all_units_discounts_and_direct_expenses()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [new ReceiptLine(productId, null, "Fan", "FAN-1", "New", null, 100m, 3, null, null, 150m)]), Actor);
        await AgreementAsync(retail, "Partner B", 10m);

        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Fan", "FAN-1", 3, 150m, 30m, 20m)], LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
        var item = Assert.Single(confirmed.Items);
        Assert.Equal(3, item.Components.Count);
        Assert.Equal(450m, item.Components.Sum(component => component.RevenueAmount));
        Assert.Equal(300m, item.Components.Sum(component => component.CostAmount));
        Assert.Equal(30m, item.Components.Sum(component => component.DiscountAmount));
        Assert.Equal(20m, item.Components.Sum(component => component.DirectExpenseAmount));
        Assert.Equal(100m, item.NetProfit);

        var completed = await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var earnings = await retail.PartnerLedger.ListEarningsAsync("Partner B");
        Assert.Equal(3, earnings.Count);
        Assert.Equal(100m, earnings.Sum(earning => earning.GrossProfit));
        Assert.Equal(10m, earnings.Sum(earning => earning.Amount));
    }

    [Fact]
    public async Task Loss_making_sale_is_zero_unless_loss_sharing_is_explicit()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Loss Item", "LOSS-1", 200m), Line(productId, "Loss Item", "LOSS-1", 200m)]), Actor);
        await AgreementAsync(retail, "Partner C", 10m);

        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Loss Item", "LOSS-1", 1, 150m)], LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var earning = Assert.Single(await retail.PartnerLedger.ListEarningsAsync("Partner C"));
        Assert.Equal(-50m, earning.GrossProfit);
        Assert.Equal(0m, earning.Amount);

        await AgreementAsync(retail, "Partner C", 10m, lossSharing: true);
        var second = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Loss Item", "LOSS-1", 1, 150m)], LeadSource.Website), null);
        var secondConfirmed = await retail.OrderStatus.UpdateStatusAsync(second.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, second.RowVersion), Actor);
        await CompleteAsync(retail, await DeliverAsync(retail, secondConfirmed));
        var lossEarning = (await retail.PartnerLedger.ListEarningsAsync("Partner C")).Single(item => item.Amount < 0);
        Assert.Equal(-5m, lossEarning.Amount);
    }

    [Fact]
    public async Task Assembly_allocations_are_editable_validated_and_used_for_partner_profit()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productCpu = Guid.NewGuid();
        var productGpu = Guid.NewGuid();
        var units = await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productCpu, "CPU", "CPU-A", 1000m, 1000m), Line(productGpu, "GPU", "GPU-A", 500m, 500m)]), Actor);
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
        await AgreementAsync(retail, "Partner D", 10m);

        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [new OrderLine(null, priced.Id, "Allocated Build", null, 1, 2000m)], LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
        var components = confirmed.Items.Single().Components;
        Assert.Equal(1400m, components.Single(item => item.UnitId == units[0].Id).RevenueAmount);
        Assert.Equal(600m, components.Single(item => item.UnitId == units[1].Id).RevenueAmount);
        Assert.Equal(500m, components.Sum(item => item.NetProfit));

        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var earnings = await retail.PartnerLedger.ListEarningsAsync("Partner D");
        Assert.Equal(40m, earnings.Single(item => item.UnitId == units[0].Id).Amount);
        Assert.Equal(10m, earnings.Single(item => item.UnitId == units[1].Id).Amount);
    }

    [Fact]
    public async Task Return_before_payout_reverses_accrual_and_waits_for_inspection()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Returnable", "RET-1", 100m)]), Actor);
        await AgreementAsync(retail, "Partner E", 10m);
        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Returnable", "RET-1", 1, 200m)], LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
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

        var earning = Assert.Single(await retail.PartnerLedger.ListEarningsAsync("Partner E"));
        Assert.Equal(EarningStatus.Reversed, earning.Status);
        Assert.Equal(0m, earning.PaidAmount);
        Assert.Empty(await retail.PartnerLedger.ListAdjustmentsAsync("Partner E"));

        var replay = await Assert.ThrowsAsync<RetailException>(() => retail.Returns.CreateReturnAsync(
            new ReturnCreateRequest(completed.Id, completed.Items.Single().Id, unitId, 200m, "again"), Actor));
        Assert.Equal(409, replay.StatusCode);
    }

    [Fact]
    public async Task Return_after_payout_creates_recoverable_balance_and_offsets_future_earnings()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Unit One", "U-1", 100m), Line(productId, "Unit Two", "U-2", 100m)]), Actor);
        await AgreementAsync(retail, "Partner F", 10m);

        var first = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Unit One", "U-1", 1, 200m)], LeadSource.Website), null);
        var firstConfirmed = await retail.OrderStatus.UpdateStatusAsync(first.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, first.RowVersion), Actor);
        var firstCompleted = await CompleteAsync(retail, await DeliverAsync(retail, firstConfirmed));
        await retail.PartnerLedger.SettleAsync(new SettlementRequest("Partner F", 10m, "Cash", null, null), Actor);
        var returnedUnit = firstCompleted.Items.Single().Components.Single().UnitId;

        var entity = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(
            firstCompleted.Id, firstCompleted.Items.Single().Id, returnedUnit, 200m, "Defective"), Actor);
        await retail.Returns.InspectReturnAsync(entity.Id, new ReturnInspectRequest("New", "Resellable", entity.RowVersion), Actor);

        var adjustment = Assert.Single(await retail.PartnerLedger.ListAdjustmentsAsync("Partner F"));
        Assert.Equal(-10m, adjustment.Amount);
        Assert.Equal(-10m, adjustment.RemainingAmount);

        var second = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Unit Two", "U-2", 1, 300m)], LeadSource.Website), null);
        var secondConfirmed = await retail.OrderStatus.UpdateStatusAsync(second.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, second.RowVersion), Actor);
        await CompleteAsync(retail, await DeliverAsync(retail, secondConfirmed));

        var future = (await retail.PartnerLedger.ListEarningsAsync("Partner F")).Single(item => item.Amount == 20m);
        Assert.Equal(10m, future.OffsetAmount);
        Assert.Equal(EarningStatus.Outstanding, future.Status);
        Assert.Equal(AdjustmentStatus.Applied, (await retail.PartnerLedger.ListAdjustmentsAsync("Partner F")).Single().Status);

        var ledger = (await retail.PartnerLedger.ListPartnerLedgerAsync()).Single(item => item.PartnerName == "Partner F");
        Assert.Equal(10m, ledger.CashDue);
        Assert.Equal(0m, ledger.Recoverable);

        var settlement = await retail.PartnerLedger.SettleAsync(new SettlementRequest("Partner F", 10m, "Cash", null, null), Actor);
        Assert.Equal(10m, settlement.Amount);
        Assert.Equal(EarningStatus.Paid, (await retail.PartnerLedger.ListEarningsAsync("Partner F")).Single(item => item.Amount == 20m).Status);
    }

    [Fact]
    public async Task Completion_requires_delivered_status_and_paid_or_explicitly_waived_payment()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Complete Me", "C-1", 100m), Line(productId, "Complete Me", "C-2", 100m)]), Actor);

        var required = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Complete Me", "C-1", 1, 200m)], LeadSource.Website), null);
        var requiredConfirmed = await retail.OrderStatus.UpdateStatusAsync(required.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, required.RowVersion), Actor);
        var requiredDelivered = await DeliverAsync(retail, requiredConfirmed, paid: false);
        var blocked = await Assert.ThrowsAsync<RetailException>(() => CompleteAsync(retail, requiredDelivered));
        Assert.Equal(409, blocked.StatusCode);
        var paid = await retail.Payments.UpdatePaymentAsync(requiredDelivered.Id,
            new PaymentStatusRequest(PaymentStatus.Paid, "cash", requiredDelivered.RowVersion), Actor);
        Assert.Equal(OrderStatus.Completed, (await CompleteAsync(retail, paid)).Status);

        var notRequired = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Complete Me", "C-2", 1, 200m)], LeadSource.Website, PaymentRequired: false), null);
        var waivedConfirmed = await retail.OrderStatus.UpdateStatusAsync(notRequired.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, notRequired.RowVersion), Actor);
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
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
        [
            new ReceiptLine(productId, null, "Snapshot CPU", "CPU-SNAP", "Open Box", "SN-SECRET", 900m, 1, 7.5m, "14 days",
                1200m, "QA Supplier", "AMD", "Ryzen 7", "CPU", "Snapshot description", "EXT-1", "{\"cores\":8}",
                "Component", "Supplier", date, "[{\"storageKey\":\"secure/key.webp\"}]")
        ]), Actor);

        var full = Assert.Single(await retail.Units.ListUnitsAsync(null, productId, true, true));
        Assert.Equal("AMD", full.Brand);
        Assert.Equal("Ryzen 7", full.Model);
        Assert.Equal("CPU", full.Category);
        Assert.Equal("Snapshot description", full.Description);
        Assert.Equal("EXT-1", full.ExternalCode);
        Assert.Equal("{\"cores\":8}", full.SpecificationsJson);
        Assert.Equal("Component", full.PurchaseKind);
        Assert.Equal("Supplier", full.SourceType);
        Assert.Equal(date, full.PurchaseDate);
        Assert.Equal("QA Supplier", full.SourceName);
        Assert.Equal("SN-SECRET", full.SerialNumber);
        Assert.Equal(900m, full.AcquisitionCost);

        var limited = Assert.Single(await retail.Units.ListUnitsAsync(null, productId, false, false));
        Assert.Null(limited.AcquisitionCost);
        Assert.Null(limited.StandalonePrice);
        Assert.Null(limited.PartnerPercentOverride);
        Assert.Null(limited.SourceName);
        Assert.Null(limited.SourceType);
        Assert.Null(limited.SerialNumber);
        Assert.Equal("Snapshot CPU", limited.Name);
        Assert.Equal("Open Box", limited.Condition);
    }

    [Fact]
    public async Task Multiple_partners_can_own_different_components_in_one_order()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var units = await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
        [
            Line(productA, "Partner CPU", "PCPU", 100m),
            Line(productB, "Partner GPU", "PGPU", 200m)
        ]), Actor);
        await retail.Allocations.UpsertAllocationAsync(new PartnerAllocationRequest("Partner X", units[0].Id, null, null, 100m, false, true));
        await retail.Allocations.UpsertAllocationAsync(new PartnerAllocationRequest("Partner Y", units[1].Id, null, null, 100m, false, true));

        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productA, "Partner CPU", "PCPU", 1, 300m), OrderLineFor(productB, "Partner GPU", "PGPU", 1, 500m)],
            LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));

        var earnings = await retail.PartnerLedger.ListEarningsAsync();
        Assert.Equal(200m, earnings.Single(item => item.PartnerName == "Partner X").Amount);
        Assert.Equal(300m, earnings.Single(item => item.PartnerName == "Partner Y").Amount);
        Assert.Equal(500m, earnings.Sum(item => item.Amount));
    }

    [Fact]
    public async Task Two_partners_can_split_one_unit_and_returns_reverse_both_after_payout()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        var unit = Assert.Single(await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Split Unit", "SPLIT", 100m)]), Actor));
        await retail.Allocations.UpsertAllocationAsync(new PartnerAllocationRequest("Partner A", unit.Id, null, null, 60m, false, true));
        await retail.Allocations.UpsertAllocationAsync(new PartnerAllocationRequest("Partner B", unit.Id, null, null, 40m, false, true));
        var overflow = await Assert.ThrowsAsync<RetailException>(() => retail.Allocations.UpsertAllocationAsync(
            new PartnerAllocationRequest("Partner C", unit.Id, null, null, 10m, false, true)));
        Assert.Equal(400, overflow.StatusCode);

        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Split Unit", "SPLIT", 1, 300m)], LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
        var completed = await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var earnings = await retail.PartnerLedger.ListEarningsAsync();
        Assert.Equal(120m, earnings.Single(item => item.PartnerName == "Partner A").Amount);
        Assert.Equal(80m, earnings.Single(item => item.PartnerName == "Partner B").Amount);
        await retail.PartnerLedger.SettleAsync(new SettlementRequest("Partner A", 120m, "Cash", null, null), Actor);
        await retail.PartnerLedger.SettleAsync(new SettlementRequest("Partner B", 80m, "Cash", null, null), Actor);

        var entity = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(
            completed.Id, completed.Items.Single().Id, unit.Id, 300m, "Return split unit"), Actor);
        await retail.Returns.InspectReturnAsync(entity.Id, new ReturnInspectRequest("New", "Resellable", entity.RowVersion), Actor);

        var adjustments = await retail.PartnerLedger.ListAdjustmentsAsync();
        Assert.Equal(-120m, adjustments.Single(item => item.PartnerName == "Partner A").Amount);
        Assert.Equal(-80m, adjustments.Single(item => item.PartnerName == "Partner B").Amount);
        Assert.Equal(2, (await retail.PartnerLedger.ListEarningsAsync()).Count(item => item.Status == EarningStatus.Reversed));
    }

    [Fact]
    public async Task Allocation_admin_contract_enforces_concurrency_audit_and_unit_precedence()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        var unit = Assert.Single(await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Allocation Unit", "ALLOC-1", 100m)]), Actor));
        var productWide = await retail.Allocations.UpsertAllocationAsync(new PartnerAllocationRequest(
            "Partner P", null, productId, null, 40m, false, true), Actor);
        var unitLevel = await retail.Allocations.UpsertAllocationAsync(new PartnerAllocationRequest(
            "Partner P", unit.Id, null, null, 60m, false, true), Actor);
        var second = await retail.Allocations.UpsertAllocationAsync(new PartnerAllocationRequest(
            "Partner Q", unit.Id, null, null, 40m, false, true), Actor);
        var overflow = await Assert.ThrowsAsync<RetailException>(() => retail.Allocations.UpsertAllocationAsync(
            new PartnerAllocationRequest("Partner R", unit.Id, null, null, 1m, false, true), Actor));
        Assert.Equal(400, overflow.StatusCode);

        var missingVersion = await Assert.ThrowsAsync<RetailException>(() => retail.Allocations.UpsertAllocationAsync(
            new PartnerAllocationRequest("Partner P", unit.Id, null, null, 50m, false, true), Actor));
        Assert.Equal(409, missingVersion.StatusCode);
        var stale = await Assert.ThrowsAsync<RetailException>(() => retail.Allocations.UpsertAllocationAsync(
            new PartnerAllocationRequest("Partner P", unit.Id, null, null, 50m, false, true,
                unitLevel.RowVersion - 1, "Change split"), Actor));
        Assert.Equal(409, stale.StatusCode);
        var updated = await retail.Allocations.UpsertAllocationAsync(new PartnerAllocationRequest(
            "Partner P", unit.Id, null, null, 55m, false, true, unitLevel.RowVersion, "Adjust split"), Actor);
        Assert.Equal(55m, updated.Percent);

        var preview = await retail.Allocations.PreviewAllocationAsync(unit.Id, null, null);
        Assert.Equal("Unit", preview.Source);
        Assert.Equal(2, preview.Allocations.Count);
        Assert.Equal(95m, preview.TotalPercent);
        Assert.Equal(5m, preview.UnallocatedPercent);

        var deactivated = await retail.Allocations.DeactivateAllocationAsync(second.Id, second.RowVersion, "Partner left", Actor);
        Assert.False(deactivated.IsActive);
        var after = await retail.Allocations.PreviewAllocationAsync(unit.Id, null, null);
        Assert.Single(after.Allocations);
        Assert.Equal(55m, after.TotalPercent);

        var history = await retail.Allocations.ListAllocationHistoryAsync(unitLevel.Id);
        Assert.True(history.Count >= 2);
        Assert.Contains(history, entry => entry.Action == "Upsert" && entry.Reason == "Adjust split");
        Assert.All(history, entry => Assert.Contains("Partner P", entry.SnapshotJson));
        Assert.NotEqual(Guid.Empty, productWide.Id);
    }

    [Fact]
    public async Task Expenses_stay_separate_and_business_report_reconciles_orders_direct_and_overhead()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Report Unit", "REP-1", 100m)]), Actor);
        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Report Unit", "REP-1", 1, 300m, 10m, 20m)], LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
        await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await retail.Expenses.CreateExpenseAsync(new ExpenseCreateRequest(today, 50m, "Packaging", "Direct", null, order.Id, null, null), Actor);
        await retail.Expenses.CreateExpenseAsync(new ExpenseCreateRequest(today, 100m, "Internet", "GeneralOverhead", null, null, null, null), Actor);

        var report = await retail.Reports.BuildReportAsync(today, today);
        Assert.Equal(290m, report.GrossSales);
        Assert.Equal(10m, report.Discounts);
        Assert.Equal(100m, report.CostOfGoods);
        Assert.Equal(20m, report.ItemDirectExpenses);
        Assert.Equal(170m, report.NetItemProfit);
        Assert.Equal(50m, report.AdditionalDirectExpenses);
        Assert.Equal(100m, report.GeneralOverhead);
        Assert.Equal(20m, report.StoreNetResult);
        Assert.Equal(1, report.CompletedOrders);
        Assert.Equal(2, (await retail.Expenses.ListExpensesAsync(today, today)).Count);
        await Assert.ThrowsAsync<RetailException>(() => retail.Expenses.CreateExpenseAsync(
            new ExpenseCreateRequest(today, -1m, "Bad", "GeneralOverhead", null, null, null, null), Actor));
    }

    [Fact]
    public async Task Order_detail_is_owner_scoped_and_admin_component_serials_are_permission_safe()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
        [
            new ReceiptLine(productId, null, "Detail Unit", "DET-1", "New", "SERIAL-DETAIL", 100m, 1, null, null)
        ]), Actor);
        var customerId = Guid.NewGuid();
        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Detail Unit", "DET-1", 1, 300m)], LeadSource.Website), customerId);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);

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
    public async Task Fulfillment_payment_matrix_terms_and_variant_snapshots_are_server_enforced()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Matrix Unit", "MATRIX-1", 100m)]), Actor);
        var delivery = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [new OrderLine(productId, null, "Matrix Unit", "MATRIX-1", 1, 300m, null, null, null,
                Guid.NewGuid(), "OC Edition", "thumb/key.webp")], LeadSource.Website, TermsAccepted: true,
            TermsPolicyVersion: "3"), null);
        Assert.Equal("Delivery", delivery.FulfillmentMethod);
        Assert.Equal("CashOnDelivery", delivery.PaymentMethod);
        Assert.True(delivery.PaymentRequired);
        Assert.Equal("OC Edition", delivery.Items[0].VariantName);
        Assert.Equal("thumb/key.webp", delivery.Items[0].ThumbnailStorageKeySnapshot);
        await using (var db = database.CreateRetailContext())
        {
            var stored = await db.Orders.SingleAsync(order => order.Id == delivery.Id);
            Assert.NotNull(stored.TermsAcceptedAtUtc);
            Assert.Equal("3", stored.TermsPolicyVersion);
        }

        var pickup = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", null, "Handover", "BankTransfer", null,
            [new OrderLine(productId, null, "Matrix Unit", "MATRIX-1", 1, 300m)], LeadSource.Website), null);
        Assert.Equal("Pickup", pickup.FulfillmentMethod);
        Assert.Equal("ToBeArranged", pickup.PaymentMethod);
        Assert.False(pickup.PaymentRequired);
        Assert.Null(pickup.AddressText);

        await Assert.ThrowsAsync<RetailException>(() => retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnHandover", null,
            [new OrderLine(productId, null, "Matrix Unit", "MATRIX-1", 1, 300m)], LeadSource.Website), null));
        await Assert.ThrowsAsync<RetailException>(() => retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", null, "Delivery", "CashOnDelivery", null,
            [new OrderLine(productId, null, "Matrix Unit", "MATRIX-1", 1, 300m)], LeadSource.Website), null));
    }

    [Fact]
    public async Task Return_rejection_restores_the_sold_unit_without_reversing_the_sale()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
            [Line(productId, "Reject Me", "R-1", 100m)]), Actor);
        await AgreementAsync(retail, "Partner G", 10m);
        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Reject Me", "R-1", 1, 200m)], LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
        var completed = await CompleteAsync(retail, await DeliverAsync(retail, confirmed));
        var unitId = completed.Items.Single().Components.Single().UnitId;
        var entity = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(
            completed.Id, completed.Items.Single().Id, unitId, 200m, "Claimed defect"), Actor);

        var rejected = await retail.Returns.RejectReturnAsync(entity.Id, new ReturnRejectRequest("No defect found", entity.RowVersion), Actor);
        Assert.Equal(ReturnStatus.Rejected, rejected.Status);
        Assert.Contains(await retail.Units.ListUnitsAsync(UnitStatus.Sold, productId), unit => unit.Id == unitId);
        Assert.Equal(EarningStatus.Outstanding, Assert.Single(await retail.PartnerLedger.ListEarningsAsync("Partner G")).Status);
    }

    [Fact]
    public async Task Concurrent_confirmation_cannot_sell_the_same_unit_twice()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), "Concurrency receipt",
            [Line(productId, "Single Unit", "CONC-1", 100m)]), Actor);

        var first = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer A", "01000000001", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Single Unit", "CONC-1", 1, 300m)], LeadSource.Website), null);
        var second = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer B", "01000000002", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Single Unit", "CONC-1", 1, 300m)], LeadSource.Website), null);

        async Task<bool> TryConfirm(OrderView order)
        {
            try
            {
                await retail.OrderStatus.UpdateStatusAsync(order.Id,
                    new OrderStatusRequest(OrderStatus.Confirmed, "concurrent", order.RowVersion), Actor);
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
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), "Two units",
            [Line(productId, "Unit A", "CONC-2A", 100m), Line(productId, "Unit B", "CONC-2B", 100m)]), Actor);

        var first = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer A", "01000000001", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Unit", "CONC-2", 1, 300m)], LeadSource.Website), null);
        var second = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer B", "01000000002", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(productId, "Unit", "CONC-2", 1, 300m)], LeadSource.Website), null);

        var outcomes = await Task.WhenAll(
            retail.OrderStatus.UpdateStatusAsync(first.Id, new OrderStatusRequest(OrderStatus.Confirmed, null, first.RowVersion), Actor),
            retail.OrderStatus.UpdateStatusAsync(second.Id, new OrderStatusRequest(OrderStatus.Confirmed, null, second.RowVersion), Actor));

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
        var units = await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), "Assembly units",
            [Line(productId, "Assembly CPU", "ASM-1", 400m), Line(productId, "Assembly Board", "ASM-2", 300m)]), Actor);
        var assembly = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Concurrent Build", units.Select(unit => unit.Id).ToList()));
        var priced = await retail.Assemblies.UpdateAssemblyAsync(assembly.Id,
            new AssemblyUpdateRequest("Concurrent Build", "بناء", "Concurrent Build", 1200m, assembly.RowVersion));
        await retail.Assemblies.SetAssemblyPublishedAsync(priced.Id, true, priced.RowVersion);

        var first = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer A", "01000000001", "Cairo", "Delivery", "CashOnDelivery", null,
            [new OrderLine(null, assembly.Id, "Concurrent Build", null, 1, 1200m)], LeadSource.Website), null);
        var second = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer B", "01000000002", "Cairo", "Delivery", "CashOnDelivery", null,
            [new OrderLine(null, assembly.Id, "Concurrent Build", null, 1, 1200m)], LeadSource.Website), null);

        async Task<bool> TryConfirm(OrderView order)
        {
            try
            {
                await retail.OrderStatus.UpdateStatusAsync(order.Id,
                    new OrderStatusRequest(OrderStatus.Confirmed, "concurrent", order.RowVersion), Actor);
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
        var cpuId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var units = await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), "Cancel build",
        [
            Line(cpuId, "Cancel CPU", "CAN-CPU", 400m),
            Line(boardId, "Cancel Board", "CAN-MB", 300m)
        ]), Actor);
        var assembly = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Cancel Build", units.Select(unit => unit.Id).ToList()));
        var priced = await retail.Assemblies.UpdateAssemblyAsync(assembly.Id,
            new AssemblyUpdateRequest("Cancel Build", "بناء", "Cancel Build", 1200m, assembly.RowVersion));
        await retail.Assemblies.SetAssemblyPublishedAsync(priced.Id, true, priced.RowVersion);

        var order = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [new OrderLine(null, assembly.Id, "Cancel Build", null, 1, 1200m)], LeadSource.Website), null);
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor);
        var cancelled = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Cancelled, "customer request", confirmed.RowVersion), Actor);
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);

        await using var db = database.CreateRetailContext();
        var unitIds = units.Select(unit => unit.Id).ToList();
        Assert.All(await db.Units.Where(unit => unitIds.Contains(unit.Id)).ToListAsync(),
            unit => Assert.Equal(UnitStatus.InAssembly, unit.Status));
        Assert.Equal(AssemblyStatus.Published, (await db.Assemblies.SingleAsync(item => item.Id == assembly.Id)).Status);

        var standalone = await retail.Orders.CreateOrderAsync(new OrderCreateRequest(
            "Customer", "01000000000", "Cairo", "Delivery", "CashOnDelivery", null,
            [OrderLineFor(cpuId, "Cancel CPU", "CAN-CPU", 1, 300m)], LeadSource.Website), null);
        await Assert.ThrowsAsync<RetailException>(() => retail.OrderStatus.UpdateStatusAsync(standalone.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, standalone.RowVersion), Actor));
    }

    [Fact]
    public async Task Removing_swapping_and_disassembling_return_units_and_rebalance_allocations()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var units = await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
        [
            Line(Guid.NewGuid(), "Edit CPU", "EDIT-CPU", 500m, 600m),
            Line(Guid.NewGuid(), "Edit GPU", "EDIT-GPU", 200m, 300m),
            Line(Guid.NewGuid(), "Edit Fan", "EDIT-FAN", 50m, 100m),
            Line(Guid.NewGuid(), "Edit PSU", "EDIT-PSU", 80m, 100m)
        ]), Actor);
        var (cpu, gpu, fan, psu) = (units[0].Id, units[1].Id, units[2].Id, units[3].Id);
        var first = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("First", [cpu, gpu, fan]));
        var second = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Second", [psu]));
        Assert.Equal(10m, first.Units.Single(unit => unit.UnitId == fan).AllocationPercent);

        var stale = await Assert.ThrowsAsync<RetailException>(() => retail.Assemblies.RemoveUnitAsync(first.Id, fan, first.RowVersion - 1));
        Assert.Equal(409, stale.StatusCode);
        var removed = await retail.Assemblies.RemoveUnitAsync(first.Id, fan, first.RowVersion);
        Assert.Equal(2, removed.Units.Count);
        Assert.Equal(100m, removed.Units.Sum(unit => unit.AllocationPercent));
        Assert.Equal(66.67m, removed.Units.Single(unit => unit.UnitId == cpu).AllocationPercent);

        var sameAssembly = await Assert.ThrowsAsync<RetailException>(() => retail.Assemblies.SwapUnitAsync(
            new SwapRequest(first.Id, first.Id, gpu, removed.RowVersion, removed.RowVersion)));
        Assert.Equal(400, sameAssembly.StatusCode);
        var swapped = await retail.Assemblies.SwapUnitAsync(new SwapRequest(first.Id, second.Id, gpu, removed.RowVersion, second.RowVersion));
        Assert.Equal(100m, Assert.Single(swapped[0].Units).AllocationPercent);
        Assert.Equal(33.33m, swapped[1].Units.Single(unit => unit.UnitId == gpu).AllocationPercent);

        var disassembled = await retail.Assemblies.DisassembleAsync(second.Id, swapped[1].RowVersion);
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

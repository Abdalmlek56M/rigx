using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Api.Tests;

/// <summary>All Retail feature services over one database, as the tests use them.</summary>
public sealed record RetailTestServices(
    InventoryUnitService Units,
    AssemblyService Assemblies,
    OrderService Orders,
    OrderStatusService OrderStatus,
    OrderPaymentService Payments,
    ReturnService Returns,
    PartnerAgreementService Agreements,
    PartnerAllocationService Allocations,
    PartnerLedgerService PartnerLedger,
    ExpenseService Expenses,
    BusinessReportService Reports,
    StockItemAdminService StockItems,
    StoreItemService Store)
{
    public static RetailTestServices Create(IDbContextFactory<RetailDbContext> factory, TimeProvider time) => new(
        new(factory, time), new(factory, time), new(factory, time), new(factory, time), new(factory, time),
        new(factory, time), new(factory, time), new(factory, time), new(factory, time), new(factory, time),
        new(factory), new(factory, time), new(factory));
}

/// <summary>Receipt helpers for tests: build a request without a source and read the received units directly.</summary>
public static class RetailReceipts
{
    private static readonly ReceiptSource NoSource = new(null, null, null, null);

    public static ReceiveRequest Receipt(Guid draftId, string? notes, IReadOnlyList<ReceiptLine> lines) =>
        new(draftId, notes, NoSource, lines);

    public static async Task<List<StockUnitView>> ReceiveUnitsAsync(this RetailTestServices retail, ReceiveRequest request, Guid actorId) =>
        (await retail.Units.ReceiveAsync(request, actorId)).Items.SelectMany(item => item.Units).ToList();
}

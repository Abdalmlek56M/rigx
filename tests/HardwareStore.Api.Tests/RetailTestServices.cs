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
    BusinessReportService Reports)
{
    public static RetailTestServices Create(IDbContextFactory<RetailDbContext> factory, TimeProvider time) => new(
        new(factory, time), new(factory, time), new(factory, time), new(factory, time), new(factory, time),
        new(factory, time), new(factory, time), new(factory, time), new(factory, time), new(factory, time),
        new(factory));
}

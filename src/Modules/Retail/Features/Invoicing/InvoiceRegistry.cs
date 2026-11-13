using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Issues the invoice of an order from the delivery workflow (runs inside the caller's transaction).</summary>
internal static class InvoiceRegistry
{
    /// <summary>
    /// Gives the order its invoice number (gap-free per year: the counter row stays locked until the delivery commits).
    /// Does nothing when the order already has an invoice.
    /// </summary>
    public static async Task IssueForOrderAsync(RetailDbContext db, StoreOrder order, InvoicePolicy policy, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Invoices.AnyAsync(invoice => invoice.OrderId == order.Id, cancellationToken)) return;
        var year = InvoiceRules.YearOf(now);
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO invoice_counters (year, last_number) VALUES ({0}, 1) ON CONFLICT (year) DO UPDATE SET last_number = invoice_counters.last_number + 1",
            new object[] { year }, cancellationToken);
        var sequence = await db.Database.SqlQueryRaw<int>("SELECT last_number AS \"Value\" FROM invoice_counters WHERE year = {0}", year)
            .SingleAsync(cancellationToken);
        db.Invoices.Add(new Invoice
        {
            Id = Guid.CreateVersion7(), OrderId = order.Id, Number = InvoiceRules.FormatNumber(policy.NumberPrefix, year, sequence),
            Year = year, Sequence = sequence, AccessToken = InvoiceRules.NewAccessToken(), SellerName = policy.SellerName,
            IssuedAtUtc = now, CreatedAtUtc = now
        });
    }
}

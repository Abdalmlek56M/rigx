using System.Text.Json;
using HardwareStore.Modules.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Storefront;

/// <summary>Reads store settings (admin and public views, checkout quote) and replaces one section at a time.</summary>
public sealed class StoreSettingsService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    public async Task<StoreSettingsAdminView> GetAdminAsync(CancellationToken cancellationToken = default)
    {
        var (row, rowVersion) = await LoadAsync(cancellationToken);
        return AdminView(row, rowVersion);
    }

    public async Task<StoreSettingsPublicView> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var (row, _) = await LoadAsync(cancellationToken);
        return new StoreSettingsPublicView(row.StoreInfo, row.ContactLinks.Links.Where(link => link.ShowOnSite).ToArray(),
            row.Shipping, row.Payment, row.Controls, row.WhatsAppTemplates, row.Reviews, row.Language,
            row.LeadSources.Sources);
    }

    public async Task<CheckoutQuoteView> QuoteCheckoutAsync(string? governorate, decimal subtotal, bool pickup,
        CancellationToken cancellationToken = default)
    {
        if (subtotal is < 0 or > 10_000_000m)
            throw new StorefrontException(400, "Order subtotal is out of range.");
        var (row, _) = await LoadAsync(cancellationToken);
        if (pickup && !row.Shipping.PickupEnabled)
            throw new StorefrontException(400, "Pickup from the store is not available.");
        var shipping = pickup
            ? new ShippingQuote(0m, null)
            : row.Shipping.QuoteFor(governorate?.Trim().ToUpperInvariant() ?? string.Empty);
        var upfront = row.Payment.UpfrontFor(subtotal, shipping.Fee, pickup);
        return new CheckoutQuoteView(shipping.Fee, shipping.DeliveryTime, upfront.Shipping, upfront.Deposit, upfront.Total);
    }

    public async Task<StoreSettingsAdminView> UpdateSectionAsync<T>(
        T? value, long rowVersion, Guid actorUserId, CancellationToken cancellationToken = default)
        where T : class, IStoreSettingsSection<T>
    {
        if (value is null)
            throw new StorefrontException(400, "Settings value is required.");
        var normalized = value.Normalize();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var row = await db.StoreSettings.SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new StoreSettings { CreatedAtUtc = now, RowVersion = 0 };
            db.StoreSettings.Add(row);
        }
        if (rowVersion != row.RowVersion)
            throw new StorefrontException(409, "The store settings changed by another user.");

        T.Write(row, normalized);
        row.RowVersion++;
        row.UpdatedAtUtc = now;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new StorefrontException(409, "The store settings changed by another user.");
        }

        await auditWriter.WriteAsync(
            new SecurityAuditEntry(
                "Storefront.SettingsUpdated",
                "Succeeded",
                "User",
                ActorUserAccountId: actorUserId,
                SafeMetadata: JsonSerializer.Serialize(new { section = T.Key })),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AdminView(row, row.RowVersion);
    }

    private static StoreSettingsAdminView AdminView(StoreSettings row, long rowVersion) =>
        new(row.StoreInfo, row.ContactLinks, row.Shipping, row.Payment, row.Watermark, row.Controls,
            row.WhatsAppTemplates, row.Reviews, row.Language, row.Invoice, row.LeadSources, row.WarrantyOptions,
            rowVersion);

    private async Task<(StoreSettings Row, long RowVersion)> LoadAsync(CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.StoreSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return row is null ? (new StoreSettings(), 0) : (row, row.RowVersion);
    }
}

using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Reset;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// The Storefront part of the reset. "business": customer requests. "content": articles with their revisions and the FAQ.
/// Both clear the customers' favourites and notifications: they point at stock and orders (business) and belong to the
/// customer accounts that "content" removes. Kept: settings, policies, offers.
/// </summary>
public sealed class StorefrontDataReset(IDbContextFactory<StorefrontDbContext> contextFactory) : IDataResetSource
{
    private static readonly string[] BusinessTables = ["customer_requests"];
    private static readonly string[] ContentTables = ["content_article_revisions", "content_articles", "content_faqs"];
    private static readonly string[] CustomerTables = ["favourites", "customer_notifications"];

    public async Task<int> ResetAsync(IReadOnlySet<string> sections, CancellationToken cancellationToken = default)
    {
        var tables = new List<string>();
        if (sections.Contains(DataResetRules.Business)) tables.AddRange(BusinessTables);
        if (sections.Contains(DataResetRules.Content)) tables.AddRange(ContentTables);
        if (tables.Count > 0) tables.AddRange(CustomerTables);
        if (tables.Count == 0) return 0;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await TableReset.DeleteAllAsync(db, tables, cancellationToken);
    }
}

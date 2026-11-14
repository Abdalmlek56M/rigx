using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Reset;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// The Storefront part of the reset. "business": customer requests and wishlists. "content": articles with their revisions
/// and the FAQ. Kept: settings, policies, offers.
/// </summary>
public sealed class StorefrontDataReset(IDbContextFactory<StorefrontDbContext> contextFactory) : IDataResetSource
{
    private static readonly string[] BusinessTables = ["customer_requests", "wishlist_items"];
    private static readonly string[] ContentTables = ["content_article_revisions", "content_articles", "content_faqs"];

    public async Task<int> ResetAsync(IReadOnlySet<string> sections, CancellationToken cancellationToken = default)
    {
        var tables = new List<string>();
        if (sections.Contains(DataResetRules.Business)) tables.AddRange(BusinessTables);
        if (sections.Contains(DataResetRules.Content)) tables.AddRange(ContentTables);
        if (tables.Count == 0) return 0;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await TableReset.DeleteAllAsync(db, tables, cancellationToken);
    }
}

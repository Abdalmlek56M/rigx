using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

public sealed record WishlistItemView(
    Guid ProductId,
    string Name,
    string Slug,
    string Sku,
    decimal BasePrice,
    string? PrimaryImageStorageKey,
    string? CategorySlug,
    bool IsAvailable,
    DateTimeOffset AddedAtUtc);

public sealed class WishlistService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<WishlistItemView>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await db.WishlistItems.AsNoTracking()
            .Where(item => item.UserAccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
            return [];
        var productIds = items.Select(item => item.ProductId).ToArray();
        var products = await db.WishlistProducts.AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        var images = await db.WishlistProductImages.AsNoTracking()
            .Where(image => productIds.Contains(image.ProductId))
            .OrderByDescending(image => image.IsPrimary)
            .ToListAsync(cancellationToken);
        var imageKeys = images
            .GroupBy(image => image.ProductId)
            .ToDictionary(group => group.Key, group => group.First().ThumbnailStorageKey);
        var categoryIds = products.Values.Select(product => product.CategoryId).Distinct().ToArray();
        var categories = await db.WishlistCategories.AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, category => category.Slug, cancellationToken);
        return items
            .Where(item => products.ContainsKey(item.ProductId))
            .Select(item =>
            {
                var product = products[item.ProductId];
                return new WishlistItemView(
                    item.ProductId,
                    product.Name,
                    product.Slug,
                    product.Sku,
                    product.BasePrice,
                    imageKeys.GetValueOrDefault(item.ProductId),
                    categories.GetValueOrDefault(product.CategoryId),
                    string.Equals(product.Status, "Published", StringComparison.Ordinal),
                    item.CreatedAtUtc);
            })
            .ToList();
    }

    public async Task AddAsync(
        Guid accountId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new StorefrontException(400, "A product is required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.WishlistProducts.AsNoTracking().AnyAsync(
                product => product.Id == productId, cancellationToken))
            throw new StorefrontException(404, "Product was not found.");
        if (await db.WishlistItems.AnyAsync(
                item => item.UserAccountId == accountId && item.ProductId == productId,
                cancellationToken))
            return;
        db.WishlistItems.Add(new WishlistItem
        {
            Id = Guid.CreateVersion7(),
            UserAccountId = accountId,
            ProductId = productId,
            CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        Guid accountId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.WishlistItems
            .Where(item => item.UserAccountId == accountId && item.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task MergeAsync(
        Guid accountId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var requested = productIds.Where(id => id != Guid.Empty).Distinct().Take(200).ToArray();
        if (requested.Length == 0)
            return;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var validIds = await db.WishlistProducts.AsNoTracking()
            .Where(product => requested.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);
        var existing = await db.WishlistItems.AsNoTracking()
            .Where(item => item.UserAccountId == accountId && validIds.Contains(item.ProductId))
            .Select(item => item.ProductId)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        foreach (var productId in validIds.Except(existing))
        {
            db.WishlistItems.Add(new WishlistItem
            {
                Id = Guid.CreateVersion7(),
                UserAccountId = accountId,
                ProductId = productId,
                CreatedAtUtc = now
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.WishlistItems
            .Where(item => item.UserAccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

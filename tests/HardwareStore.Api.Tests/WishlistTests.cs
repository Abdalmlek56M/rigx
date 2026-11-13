using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class WishlistPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Adds_once_and_isolates_accounts()
    {
        var database = await fixture.CreateDatabaseAsync();
        var seed = await SeedAsync(database);
        var service = database.CreateWishlistService();
        var account = Guid.NewGuid();
        var other = Guid.NewGuid();

        await service.AddAsync(account, seed.PublishedId);
        await service.AddAsync(account, seed.PublishedId);
        await service.AddAsync(account, seed.DraftId);

        var items = await service.ListAsync(account);
        Assert.Equal(2, items.Count);
        Assert.Single(items, item => item.ProductId == seed.PublishedId && item.IsAvailable);
        var draft = Assert.Single(items, item => item.ProductId == seed.DraftId);
        Assert.False(draft.IsAvailable);
        Assert.Equal("processors", draft.CategorySlug);
        Assert.Equal(seed.PrimaryImageStorageKey, draft.PrimaryImageStorageKey);

        Assert.Empty(await service.ListAsync(other));

        var foreign = await Assert.ThrowsAsync<StorefrontException>(() =>
            service.AddAsync(account, Guid.NewGuid()));
        Assert.Equal(404, foreign.StatusCode);
    }

    [Fact]
    public async Task Merges_without_duplicates_and_drops_deleted_products()
    {
        var database = await fixture.CreateDatabaseAsync();
        var seed = await SeedAsync(database);
        var service = database.CreateWishlistService();
        var account = Guid.NewGuid();

        await service.AddAsync(account, seed.PublishedId);
        await service.MergeAsync(account, [seed.PublishedId, seed.SecondPublishedId, seed.PublishedId, Guid.NewGuid()]);
        var items = await service.ListAsync(account);
        Assert.Equal(2, items.Count);
        Assert.Single(items, item => item.ProductId == seed.PublishedId);
        Assert.Single(items, item => item.ProductId == seed.SecondPublishedId);

        await using (var catalog = database.CreateCatalogContext())
        {
            await catalog.Products
                .Where(product => product.Id == seed.SecondPublishedId)
                .ExecuteDeleteAsync();
        }
        items = await service.ListAsync(account);
        Assert.Single(items);
        Assert.Equal(seed.PublishedId, items[0].ProductId);

        await service.RemoveAsync(account, seed.PublishedId);
        Assert.Empty(await service.ListAsync(account));
    }

    [Fact]
    public async Task Clear_removes_only_the_requested_account()
    {
        var database = await fixture.CreateDatabaseAsync();
        var seed = await SeedAsync(database);
        var service = database.CreateWishlistService();
        var account = Guid.NewGuid();
        var other = Guid.NewGuid();

        await service.AddAsync(account, seed.PublishedId);
        await service.AddAsync(other, seed.PublishedId);
        await service.ClearAsync(account);

        Assert.Empty(await service.ListAsync(account));
        Assert.Single(await service.ListAsync(other));
    }

    private static async Task<SeedIds> SeedAsync(OwnerBootstrapDatabase database)
    {
        await using var catalog = database.CreateCatalogContext();
        var category = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Processors",
            Slug = "processors",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var published = CreateProduct(category.Id, "Ryzen Test", "ryzen-test", "WL-001", CatalogProductStatus.Published);
        var second = CreateProduct(category.Id, "Intel Test", "intel-test", "WL-002", CatalogProductStatus.Published);
        var draft = CreateProduct(category.Id, "Draft Test", "draft-test", "WL-003", CatalogProductStatus.Draft);
        catalog.Categories.Add(category);
        catalog.Products.AddRange(published, second, draft);
        const string primaryKey = "products/wishlist-primary-thumb.png";
        catalog.ProductImages.Add(new CatalogProductImage
        {
            Id = Guid.CreateVersion7(),
            ProductId = draft.Id,
            StorageKey = "products/wishlist-primary.png",
            ThumbnailStorageKey = primaryKey,
            AltText = "Wishlist test image",
            FileSizeBytes = 1024,
            Width = 800,
            Height = 600,
            ThumbnailWidth = 400,
            ThumbnailHeight = 300,
            SortOrder = 0,
            IsPrimary = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await catalog.SaveChangesAsync();
        return new SeedIds(published.Id, second.Id, draft.Id, primaryKey);
    }

    private static CatalogProduct CreateProduct(
        Guid categoryId,
        string name,
        string slug,
        string sku,
        CatalogProductStatus status) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CategoryId = categoryId,
            Name = name,
            Slug = slug,
            Sku = sku,
            BasePrice = 1000m,
            Status = status,
            Condition = CatalogProductCondition.New,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

    private sealed record SeedIds(
        Guid PublishedId,
        Guid SecondPublishedId,
        Guid DraftId,
        string PrimaryImageStorageKey);
}

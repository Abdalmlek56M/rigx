using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class ReviewsAndOffersPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Product_and_store_reviews_require_moderation_before_publication()
    {
        var database = await fixture.CreateDatabaseAsync();
        var (productId, _) = await SeedProductAsync(database, "reviews");
        var reviews = new ReviewService(new CatalogFactory(database.ConnectionString), TimeProvider.System);
        var user = Guid.NewGuid();

        var product = await reviews.UpsertProductReviewAsync(
            user,
            productId,
            new ReviewInput(5, "Excellent", "Very solid component and fast delivery.", true));
        Assert.Equal(ReviewStatus.Pending, product.Status);
        Assert.Empty((await reviews.GetProductReviewsAsync(productId)).Items);

        var store = await reviews.UpsertStoreReviewAsync(
            user,
            new ReviewInput(4, null, "The store experience was clear and helpful.", false));
        Assert.Equal(ReviewStatus.Pending, store.Status);
        Assert.Empty((await reviews.GetStoreReviewsAsync()).Items);

        var moderated = await reviews.ModerateAsync(
            "Product",
            product.Id,
            new ModerationRequest("Publish", null, product.RowVersion),
            Guid.NewGuid());
        Assert.Equal(ReviewStatus.Published, moderated.Status);
        var storeModerated = await reviews.ModerateAsync(
            "Store",
            store.Id,
            new ModerationRequest("Publish", null, store.RowVersion),
            Guid.NewGuid());

        var productResponse = await reviews.GetProductReviewsAsync(productId);
        Assert.Single(productResponse.Items);
        Assert.Equal(5m, productResponse.Aggregate.AverageRating);
        Assert.Equal(1, productResponse.Aggregate.PublishedCount);
        Assert.Equal(1, productResponse.Aggregate.Distribution.Single(item => item.Stars == 5).Count);

        var storeResponse = await reviews.GetStoreReviewsAsync();
        Assert.Equal(4m, storeResponse.Aggregate.AverageRating);

        // Editing an existing review returns it to moderation instead of silently staying public.
        var edited = await reviews.UpsertProductReviewAsync(
            user,
            productId,
            new ReviewInput(3, null, "Updated opinion after more testing.", true));
        Assert.Equal(ReviewStatus.Pending, edited.Status);
        Assert.Equal(moderated.Id, edited.Id);
        Assert.Empty((await reviews.GetProductReviewsAsync(productId)).Items);

        await reviews.DeleteProductReviewAsync(user, productId);
        await reviews.DeleteStoreReviewAsync(user);
        var mine = await reviews.GetMineAsync(user);
        Assert.Empty(mine.ProductReviews);
        Assert.Empty(mine.StoreReview);
        Assert.Equal(ReviewStatus.Published, storeModerated.Status);
    }

    [Fact]
    public async Task Offer_requires_published_products_and_respects_active_window()
    {
        var database = await fixture.CreateDatabaseAsync();
        var (productId, basePrice) = await SeedProductAsync(database, "offer");
        var offers = new OfferService(new StorefrontFactory(database.ConnectionString), TimeProvider.System);

        var invalid = await Assert.ThrowsAsync<StorefrontException>(() =>
            offers.CreateAsync(
                new OfferInput(
                    "عرض", "Offer", null, null, DateTimeOffset.UtcNow.AddHours(-1), null, 0,
                    [new OfferItemInput(productId, basePrice)])));
        Assert.Equal(400, invalid.StatusCode);

        var created = await offers.CreateAsync(
            new OfferInput(
                "عرض", "Offer", "وصف", "Description",
                DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddDays(2), 5,
                [new OfferItemInput(productId, Math.Round(basePrice - 10, 2))]));
        Assert.Equal(OfferStatus.Draft, created.Status);
        Assert.Empty(await offers.GetPublicAsync());

        var published = await offers.SetPublishedAsync(created.Id, true, created.RowVersion);
        Assert.Equal(OfferStatus.Published, published.Status);
        var publicOffers = await offers.GetPublicAsync();
        Assert.Single(publicOffers);
        Assert.Equal(basePrice - 10, publicOffers[0].Items[0].OfferPrice);

        var expired = await offers.CreateAsync(
            new OfferInput(
                "منتهي", "Expired", null, null,
                DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(-1), 0,
                [new OfferItemInput(productId, basePrice - 5)]));
        await offers.SetPublishedAsync(expired.Id, true, expired.RowVersion);
        Assert.Single(await offers.GetPublicAsync());

        var cancelled = await offers.CancelAsync(published.Id, published.RowVersion);
        Assert.Equal(OfferStatus.Cancelled, cancelled.Status);
        Assert.Empty(await offers.GetPublicAsync());
        await Assert.ThrowsAsync<StorefrontException>(() =>
            offers.UpdateAsync(
                cancelled.Id,
                new OfferUpdateRequest("x", "x", null, null, null, null, 0,
                    [new OfferItemInput(productId, basePrice - 1)], cancelled.RowVersion)));
    }

    private static async Task<(Guid ProductId, decimal BasePrice)> SeedProductAsync(
        OwnerBootstrapDatabase database,
        string prefix)
    {
        await using var catalog = database.CreateCatalogContext();
        var category = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Reviews Category",
            Slug = $"{prefix}-category-{Guid.NewGuid():N}",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var product = new CatalogProduct
        {
            Id = Guid.CreateVersion7(),
            CategoryId = category.Id,
            Name = $"{prefix} product",
            Slug = $"{prefix}-product-{Guid.NewGuid():N}",
            Sku = $"{prefix.ToUpperInvariant()}-{Guid.NewGuid():N}",
            BasePrice = 2500m,
            Status = CatalogProductStatus.Published,
            Condition = CatalogProductCondition.New,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        catalog.Categories.Add(category);
        catalog.Products.Add(product);
        await catalog.SaveChangesAsync();
        return (product.Id, product.BasePrice);
    }

    private sealed class CatalogFactory(string connectionString) : IDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class StorefrontFactory(string connectionString) : IDbContextFactory<StorefrontDbContext>
    {
        public StorefrontDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
    }
}

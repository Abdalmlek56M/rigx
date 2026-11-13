using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Inventory;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;

public sealed record DashboardMetric(string Key, int Value);

public sealed record DashboardSeriesPoint(DateOnly Date, int Value);

public sealed record DashboardBreakdown(string Label, int Value);

public sealed record AdminDashboardSnapshot(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<DashboardMetric> Catalog,
    IReadOnlyList<DashboardMetric> Purchasing,
    IReadOnlyList<DashboardMetric> Customers,
    IReadOnlyList<DashboardMetric> Content,
    IReadOnlyList<DashboardMetric> Reviews,
    IReadOnlyList<DashboardMetric> Media,
    IReadOnlyList<DashboardBreakdown> ProductConditions,
    IReadOnlyList<DashboardBreakdown> DraftPurchaseSources,
    IReadOnlyList<DashboardBreakdown> RatingDistribution,
    IReadOnlyList<DashboardSeriesPoint> AccountsLast14Days,
    IReadOnlyList<DashboardSeriesPoint> ReviewsLast14Days,
    IReadOnlyList<SecurityAuditItemView> RecentAuditEvents);

public sealed class AdminAnalyticsService(
    IDbContextFactory<CatalogDbContext> catalogFactory,
    IDbContextFactory<PurchasingDbContext> purchasingFactory,
    IDbContextFactory<IdentityDbContext> identityFactory,
    IDbContextFactory<StorefrontDbContext> storefrontFactory,
    IDbContextFactory<InventoryDbContext> inventoryFactory,
    SecurityAuditQueryService auditQuery,
    TimeProvider timeProvider)
{
    public async Task<AdminDashboardSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var from = new DateTimeOffset(now.AddDays(-13).UtcDateTime.Date, TimeSpan.Zero);

        await using var catalog = await catalogFactory.CreateDbContextAsync(cancellationToken);
        var publishedProducts = await catalog.Products.CountAsync(
            product => product.Status == CatalogProductStatus.Published, cancellationToken);
        var draftProducts = await catalog.Products.CountAsync(
            product => product.Status == CatalogProductStatus.Draft, cancellationToken);
        var categories = await catalog.Categories.CountAsync(cancellationToken);
        var activeCategories = await catalog.Categories.CountAsync(category => category.IsActive, cancellationToken);
        var brands = await catalog.Brands.CountAsync(cancellationToken);
        var activeBrands = await catalog.Brands.CountAsync(brand => brand.IsActive, cancellationToken);
        var newProducts = await catalog.Products.CountAsync(product => product.Condition == CatalogProductCondition.New, cancellationToken);
        var usedProducts = await catalog.Products.CountAsync(product => product.Condition == CatalogProductCondition.Used, cancellationToken);
        var productsWithoutMedia = await catalog.Products.CountAsync(
            product => !catalog.ProductImages.Any(image => image.ProductId == product.Id), cancellationToken);
        var recentlyAdded = await catalog.Products.CountAsync(
            product => product.CreatedAtUtc >= now.AddDays(-30), cancellationToken);

        var pendingReviews = await catalog.ProductReviews.CountAsync(review => review.Status == ReviewStatus.Pending, cancellationToken)
            + await catalog.StoreReviews.CountAsync(review => review.Status == ReviewStatus.Pending, cancellationToken);
        var publishedProductReviews = await catalog.ProductReviews.CountAsync(
            review => review.Status == ReviewStatus.Published, cancellationToken);
        var publishedStoreReviews = await catalog.StoreReviews.CountAsync(
            review => review.Status == ReviewStatus.Published, cancellationToken);
        var productRatings = await catalog.ProductReviews.AsNoTracking()
            .Where(review => review.Status == ReviewStatus.Published)
            .Select(review => review.Rating)
            .ToListAsync(cancellationToken);
        var storeRatings = await catalog.StoreReviews.AsNoTracking()
            .Where(review => review.Status == ReviewStatus.Published)
            .Select(review => review.Rating)
            .ToListAsync(cancellationToken);
        var ratingRows = productRatings.Concat(storeRatings).ToList();
        var ratingDistribution = Enumerable.Range(1, 5)
            .Select(stars => new DashboardBreakdown($"{stars}★", ratingRows.Count(rating => rating == stars)))
            .ToList();
        var reviewsLast14 = await catalog.ProductReviews.AsNoTracking()
            .Where(review => review.CreatedAtUtc >= from)
            .Select(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var storeReviewsLast14 = await catalog.StoreReviews.AsNoTracking()
            .Where(review => review.CreatedAtUtc >= from)
            .Select(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var reviewDates = reviewsLast14.Concat(storeReviewsLast14).ToList();

        var mediaAssets = await catalog.ContentMediaAssets.CountAsync(asset => !asset.IsDeleted, cancellationToken);
        var mediaBytes = await catalog.ContentMediaAssets
            .Where(asset => !asset.IsDeleted)
            .SumAsync(asset => (long?)asset.FileSizeBytes, cancellationToken) ?? 0;
        var mediaUnused = await catalog.ContentMediaAssets.CountAsync(
            asset => !asset.IsDeleted &&
                     !catalog.ContentMediaAssetUsages.Any(usage => usage.AssetId == asset.Id),
            cancellationToken);

        await using var purchasing = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        var suppliers = await purchasing.SupplierProfiles.CountAsync(supplier => supplier.IsActive, cancellationToken);
        var supplierProducts = await purchasing.SupplierProducts.CountAsync(item => item.IsActive, cancellationToken);
        var draftPurchases = await purchasing.PurchaseDrafts.CountAsync(
            draft => draft.Status == PurchaseDraftStatus.Draft, cancellationToken);
        var cancelledPurchases = await purchasing.PurchaseDrafts.CountAsync(
            draft => draft.Status == PurchaseDraftStatus.Cancelled, cancellationToken);
        var draftSources = await purchasing.PurchaseDrafts.AsNoTracking()
            .Where(draft => draft.Status == PurchaseDraftStatus.Draft)
            .GroupBy(draft => draft.SourceType)
            .Select(group => new DashboardBreakdown(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        await using var identity = await identityFactory.CreateDbContextAsync(cancellationToken);
        var registeredCustomers = await identity.UserAccounts.CountAsync(account =>
            account.UserRoles.Any(role => role.Role.Code == "Customer"), cancellationToken);
        var activeAccounts = await identity.UserAccounts.CountAsync(account => account.IsActive, cancellationToken);
        var unverifiedAccounts = await identity.UserAccounts.CountAsync(
            account => account.IsActive && !account.IsEmailVerified, cancellationToken);
        var newAccounts = await identity.UserAccounts.CountAsync(
            account => account.CreatedAtUtc >= now.AddDays(-30), cancellationToken);
        var accountDates = await identity.UserAccounts.AsNoTracking()
            .Where(account => account.CreatedAtUtc >= from)
            .Select(account => account.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        await using var inventory = await inventoryFactory.CreateDbContextAsync(cancellationToken);
        var warehouses = await inventory.Warehouses.CountAsync(warehouse => warehouse.IsActive, cancellationToken);

        await using var storefront = await storefrontFactory.CreateDbContextAsync(cancellationToken);
        var publishedArticles = await storefront.ContentArticles.CountAsync(
            article => article.Status == ContentStatus.Published, cancellationToken);
        var draftArticles = await storefront.ContentArticles.CountAsync(
            article => article.Status == ContentStatus.Draft, cancellationToken);
        var faqs = await storefront.ContentFaqs.CountAsync(faq => faq.Status == ContentStatus.Published, cancellationToken);
        var policies = await storefront.ContentPolicies.CountAsync(policy => policy.Status == ContentStatus.Published, cancellationToken);
        var publishedOffers = await storefront.Offers.CountAsync(
            offer => offer.Status == OfferStatus.Published, cancellationToken);
        var draftOffers = await storefront.Offers.CountAsync(
            offer => offer.Status == OfferStatus.Draft, cancellationToken);

        var recentAudit = await auditQuery.QueryAsync(new SecurityAuditQuery(PageSize: 8), cancellationToken);

        return new AdminDashboardSnapshot(
            now,
            [
                new("publishedProducts", publishedProducts),
                new("draftProducts", draftProducts),
                new("categories", categories),
                new("activeCategories", activeCategories),
                new("brands", brands),
                new("activeBrands", activeBrands),
                new("productsWithoutMedia", productsWithoutMedia),
                new("recentProducts", recentlyAdded)
            ],
            [
                new("activeSuppliers", suppliers),
                new("supplierProducts", supplierProducts),
                new("draftPurchases", draftPurchases),
                new("cancelledPurchases", cancelledPurchases),
                new("warehouses", warehouses)
            ],
            [
                new("registeredCustomers", registeredCustomers),
                new("activeAccounts", activeAccounts),
                new("newAccounts30d", newAccounts),
                new("unverifiedAccounts", unverifiedAccounts)
            ],
            [
                new("publishedArticles", publishedArticles),
                new("draftArticles", draftArticles),
                new("publishedFaqs", faqs),
                new("publishedPolicies", policies),
                new("publishedOffers", publishedOffers),
                new("draftOffers", draftOffers)
            ],
            [
                new("pendingReviews", pendingReviews),
                new("publishedProductReviews", publishedProductReviews),
                new("publishedStoreReviews", publishedStoreReviews)
            ],
            [
                new("mediaAssets", mediaAssets),
                new("mediaUnused", mediaUnused),
                new("mediaBytes", (int)Math.Min(int.MaxValue, mediaBytes))
            ],
            [
                new("New", newProducts),
                new("Used", usedProducts)
            ],
            draftSources,
            ratingDistribution,
            BuildSeries(accountDates, from, now),
            BuildSeries(reviewDates, from, now),
            recentAudit.Items);
    }

    private static IReadOnlyList<DashboardSeriesPoint> BuildSeries(
        IEnumerable<DateTimeOffset> dates,
        DateTimeOffset from,
        DateTimeOffset to)
    {
        var start = from.UtcDateTime.Date;
        var end = to.UtcDateTime.Date;
        var counts = dates
            .Select(date => DateOnly.FromDateTime(date.UtcDateTime))
            .GroupBy(date => date)
            .ToDictionary(group => group.Key, group => group.Count());
        var points = new List<DashboardSeriesPoint>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var key = DateOnly.FromDateTime(date);
            points.Add(new DashboardSeriesPoint(key, counts.GetValueOrDefault(key)));
        }
        return points;
    }
}

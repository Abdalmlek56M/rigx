using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

public static class ReviewStatus
{
    public const string Pending = "Pending";
    public const string Published = "Published";
    public const string Rejected = "Rejected";
    public const string Hidden = "Hidden";
}

public sealed class ProductReview
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public required string Body { get; set; }
    public bool ShowAvatar { get; set; }
    public string Status { get; set; } = ReviewStatus.Pending;
    public string? ModerationReason { get; set; }
    public Guid? ModeratedByUserAccountId { get; set; }
    public DateTimeOffset? ModeratedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class StoreReview
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public int Rating { get; set; }
    public required string Body { get; set; }
    public bool ShowAvatar { get; set; }
    public string Status { get; set; } = ReviewStatus.Pending;
    public string? ModerationReason { get; set; }
    public Guid? ModeratedByUserAccountId { get; set; }
    public DateTimeOffset? ModeratedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed record ReviewInput(
    int Rating,
    string? Title,
    string? Body,
    bool ShowAvatar);

public sealed record ProductReviewView(
    Guid Id,
    Guid ProductId,
    Guid UserAccountId,
    int Rating,
    string? Title,
    string Body,
    bool ShowAvatar,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record StoreReviewView(
    Guid Id,
    Guid UserAccountId,
    int Rating,
    string Body,
    bool ShowAvatar,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record RatingDistribution(int Stars, int Count);

public sealed record ReviewAggregate(
    decimal AverageRating,
    int PublishedCount,
    IReadOnlyList<RatingDistribution> Distribution);

public sealed record ProductReviewsResponse(
    ReviewAggregate Aggregate,
    IReadOnlyList<ProductReviewView> Items);

public sealed record StoreReviewsResponse(
    ReviewAggregate Aggregate,
    IReadOnlyList<StoreReviewView> Items);

public sealed record MyReviewsResponse(
    IReadOnlyList<ProductReviewView> ProductReviews,
    IReadOnlyList<StoreReviewView> StoreReview);

public sealed record AdminReviewView(
    Guid Id,
    string Type,
    Guid UserAccountId,
    Guid? ProductId,
    int Rating,
    string? Title,
    string Body,
    string Status,
    string? ModerationReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record ModerationRequest(string? Action, string? Reason, long RowVersion);

public sealed class ReviewException(int statusCode, string message) : AppException(statusCode, message);

public sealed class ReviewService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<ProductReviewsResponse> GetProductReviewsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Products.AsNoTracking().AnyAsync(
                product => product.Id == productId && product.Status == CatalogProductStatus.Published,
                cancellationToken))
            throw new ReviewException(404, "Product was not found.");
        var reviews = await db.ProductReviews.AsNoTracking()
            .Where(review => review.ProductId == productId && review.Status == ReviewStatus.Published)
            .OrderByDescending(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return new ProductReviewsResponse(Aggregate(reviews.Select(review => review.Rating)), reviews.Select(MapProduct).ToList());
    }

    public async Task<StoreReviewsResponse> GetStoreReviewsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var reviews = await db.StoreReviews.AsNoTracking()
            .Where(review => review.Status == ReviewStatus.Published)
            .OrderByDescending(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return new StoreReviewsResponse(Aggregate(reviews.Select(review => review.Rating)), reviews.Select(MapStore).ToList());
    }

    public async Task<MyReviewsResponse> GetMineAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var productReviews = await db.ProductReviews.AsNoTracking()
            .Where(review => review.UserAccountId == userAccountId)
            .OrderByDescending(review => review.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        var storeReview = await db.StoreReviews.AsNoTracking()
            .Where(review => review.UserAccountId == userAccountId)
            .OrderByDescending(review => review.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return new MyReviewsResponse(
            productReviews.Select(MapProduct).ToList(),
            storeReview is null ? [] : [MapStore(storeReview)]);
    }

    public async Task<ProductReviewView> UpsertProductReviewAsync(
        Guid userAccountId,
        Guid productId,
        ReviewInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Products.AsNoTracking().AnyAsync(
                product => product.Id == productId && product.Status == CatalogProductStatus.Published,
                cancellationToken))
            throw new ReviewException(404, "Product was not found.");
        var review = await db.ProductReviews.SingleOrDefaultAsync(
            item => item.UserAccountId == userAccountId && item.ProductId == productId, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        if (review is null)
        {
            review = new ProductReview
            {
                Id = Guid.CreateVersion7(),
                UserAccountId = userAccountId,
                ProductId = productId,
                Body = normalized.Body,
                CreatedAtUtc = now
            };
            db.ProductReviews.Add(review);
        }
        review.Rating = normalized.Rating;
        review.Title = normalized.Title;
        review.Body = normalized.Body;
        review.ShowAvatar = input.ShowAvatar;
        review.Status = ReviewStatus.Pending;
        review.ModerationReason = null;
        review.ModeratedAtUtc = null;
        review.ModeratedByUserAccountId = null;
        review.UpdatedAtUtc = now;
        review.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return MapProduct(review);
    }

    public async Task<StoreReviewView> UpsertStoreReviewAsync(
        Guid userAccountId,
        ReviewInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var review = await db.StoreReviews.SingleOrDefaultAsync(
            item => item.UserAccountId == userAccountId, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        if (review is null)
        {
            review = new StoreReview
            {
                Id = Guid.CreateVersion7(),
                UserAccountId = userAccountId,
                CreatedAtUtc = now,
                Body = normalized.Body
            };
            db.StoreReviews.Add(review);
        }
        review.Rating = normalized.Rating;
        review.Body = normalized.Body;
        review.ShowAvatar = input.ShowAvatar;
        review.Status = ReviewStatus.Pending;
        review.ModerationReason = null;
        review.ModeratedAtUtc = null;
        review.ModeratedByUserAccountId = null;
        review.UpdatedAtUtc = now;
        review.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return MapStore(review);
    }

    public async Task DeleteProductReviewAsync(
        Guid userAccountId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.ProductReviews
            .Where(review => review.UserAccountId == userAccountId && review.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteStoreReviewAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.StoreReviews
            .Where(review => review.UserAccountId == userAccountId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminReviewView>> ListAdminAsync(
        string? type,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PageRequest.Of(page, pageSize);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var includeProducts = type is null or "Product";
        var includeStore = type is null or "Store";
        var items = new List<AdminReviewView>();

        if (includeProducts)
        {
            var query = db.ProductReviews.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                var value = status.Trim();
                query = query.Where(review => review.Status == value);
            }
            items.AddRange(await query
                .OrderByDescending(review => review.UpdatedAtUtc)
                .Select(review => new AdminReviewView(
                    review.Id, "Product", review.UserAccountId, review.ProductId, review.Rating,
                    review.Title, review.Body, review.Status, review.ModerationReason,
                    review.CreatedAtUtc, review.UpdatedAtUtc, review.RowVersion))
                .ToListAsync(cancellationToken));
        }
        if (includeStore)
        {
            var query = db.StoreReviews.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                var value = status.Trim();
                query = query.Where(review => review.Status == value);
            }
            items.AddRange(await query
                .OrderByDescending(review => review.UpdatedAtUtc)
                .Select(review => new AdminReviewView(
                    review.Id, "Store", review.UserAccountId, null, review.Rating,
                    null, review.Body, review.Status, review.ModerationReason,
                    review.CreatedAtUtc, review.UpdatedAtUtc, review.RowVersion))
                .ToListAsync(cancellationToken));
        }

        var ordered = items.OrderByDescending(item => item.UpdatedAtUtc).ToList();
        return new PagedResult<AdminReviewView>(
            ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page,
            pageSize,
            ordered.Count);
    }

    public async Task<AdminReviewView> ModerateAsync(
        string type,
        Guid id,
        ModerationRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var action = request.Action?.Trim();
        if (action is not ("Publish" or "Reject" or "Hide"))
            throw new ReviewException(400, "A supported moderation action is required.");
        var status = action switch
        {
            "Publish" => ReviewStatus.Published,
            "Reject" => ReviewStatus.Rejected,
            _ => ReviewStatus.Hidden
        };
        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? null
            : request.Reason.Trim();
        if (reason?.Length > 500)
            throw new ReviewException(400, "The moderation reason is too long.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (string.Equals(type, "Product", StringComparison.Ordinal))
        {
            var review = await db.ProductReviews.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new ReviewException(404, "Review was not found.");
            if (review.RowVersion != request.RowVersion)
                throw new ReviewException(409, "The review was changed by another user.");
            review.Status = status;
            review.ModerationReason = reason;
            review.ModeratedByUserAccountId = actorId;
            review.ModeratedAtUtc = now;
            review.UpdatedAtUtc = now;
            review.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            return new AdminReviewView(
                review.Id, "Product", review.UserAccountId, review.ProductId, review.Rating,
                review.Title, review.Body, review.Status, review.ModerationReason,
                review.CreatedAtUtc, review.UpdatedAtUtc, review.RowVersion);
        }
        if (string.Equals(type, "Store", StringComparison.Ordinal))
        {
            var review = await db.StoreReviews.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new ReviewException(404, "Review was not found.");
            if (review.RowVersion != request.RowVersion)
                throw new ReviewException(409, "The review was changed by another user.");
            review.Status = status;
            review.ModerationReason = reason;
            review.ModeratedByUserAccountId = actorId;
            review.ModeratedAtUtc = now;
            review.UpdatedAtUtc = now;
            review.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            return new AdminReviewView(
                review.Id, "Store", review.UserAccountId, null, review.Rating,
                null, review.Body, review.Status, review.ModerationReason,
                review.CreatedAtUtc, review.UpdatedAtUtc, review.RowVersion);
        }
        throw new ReviewException(400, "A supported review type is required.");
    }

    private static ReviewAggregate Aggregate(IEnumerable<int> ratings)
    {
        var values = ratings.ToList();
        var distribution = Enumerable.Range(1, 5)
            .Select(stars => new RatingDistribution(stars, values.Count(rating => rating == stars)))
            .ToList();
        var average = values.Count == 0
            ? 0m
            : Math.Round((decimal)values.Sum() / values.Count, 2, MidpointRounding.AwayFromZero);
        return new ReviewAggregate(average, values.Count, distribution);
    }

    private static NormalizedReview Normalize(ReviewInput input)
    {
        if (input.Rating is < 1 or > 5)
            throw new ReviewException(400, "Rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(input.Body) || input.Body.Trim().Length < 10)
            throw new ReviewException(400, "Review text must be at least 10 characters.");
        var body = input.Body.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (body.Length > 4000)
            throw new ReviewException(400, "Review text is too long.");
        if (body.Contains('<') || body.Contains('>'))
            throw new ReviewException(400, "Review text must be plain text without HTML.");
        if (body.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
            throw new ReviewException(400, "Review text contains unsupported characters.");
        var title = string.IsNullOrWhiteSpace(input.Title)
            ? null
            : input.Title.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (title?.Length > 120)
            throw new ReviewException(400, "Review title is too long.");
        if (title is not null && (title.Contains('<') || title.Contains('>')))
            throw new ReviewException(400, "Review title must be plain text.");
        return new NormalizedReview(input.Rating, title, body);
    }

    private static ProductReviewView MapProduct(ProductReview review) => new(
        review.Id, review.ProductId, review.UserAccountId, review.Rating, review.Title,
        review.Body, review.ShowAvatar, review.Status,
        review.CreatedAtUtc, review.UpdatedAtUtc, review.RowVersion);

    private static StoreReviewView MapStore(StoreReview review) => new(
        review.Id, review.UserAccountId, review.Rating, review.Body, review.ShowAvatar,
        review.Status, review.CreatedAtUtc, review.UpdatedAtUtc, review.RowVersion);

    private sealed record NormalizedReview(int Rating, string? Title, string Body);
}

public static class ReviewModelConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<ProductReview>(entity =>
        {
            entity.ToTable("product_reviews", table =>
            {
                table.HasCheckConstraint("ck_product_reviews_rating", "rating BETWEEN 1 AND 5");
                table.HasCheckConstraint("ck_product_reviews_status",
                    "status IN ('Pending','Published','Rejected','Hidden')");
                table.HasCheckConstraint("ck_product_reviews_body", "length(btrim(body)) BETWEEN 10 AND 4000");
            });
            entity.HasKey(review => review.Id);
            entity.Property(review => review.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(review => review.UserAccountId).HasColumnName("user_account_id");
            entity.Property(review => review.ProductId).HasColumnName("product_id");
            entity.Property(review => review.Rating).HasColumnName("rating");
            entity.Property(review => review.Title).HasColumnName("title").HasMaxLength(120);
            entity.Property(review => review.Body).HasColumnName("body").HasMaxLength(4000).IsRequired();
            entity.Property(review => review.ShowAvatar).HasColumnName("show_avatar");
            entity.Property(review => review.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(review => review.ModerationReason).HasColumnName("moderation_reason").HasMaxLength(500);
            entity.Property(review => review.ModeratedByUserAccountId).HasColumnName("moderated_by_user_account_id");
            entity.Property(review => review.ModeratedAtUtc).HasColumnName("moderated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(review => review.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(review => review.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(review => review.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(review => new { review.UserAccountId, review.ProductId }).IsUnique()
                .HasDatabaseName("ux_product_reviews_user_product");
            entity.HasIndex(review => new { review.ProductId, review.Status })
                .HasDatabaseName("ix_product_reviews_product_status");
            entity.HasIndex(review => new { review.Status, review.UpdatedAtUtc })
                .HasDatabaseName("ix_product_reviews_status_updated");
            entity.HasOne<CatalogProduct>().WithMany().HasForeignKey(review => review.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<StoreReview>(entity =>
        {
            entity.ToTable("store_reviews", table =>
            {
                table.HasCheckConstraint("ck_store_reviews_rating", "rating BETWEEN 1 AND 5");
                table.HasCheckConstraint("ck_store_reviews_status",
                    "status IN ('Pending','Published','Rejected','Hidden')");
                table.HasCheckConstraint("ck_store_reviews_body", "length(btrim(body)) BETWEEN 10 AND 4000");
            });
            entity.HasKey(review => review.Id);
            entity.Property(review => review.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(review => review.UserAccountId).HasColumnName("user_account_id");
            entity.Property(review => review.Rating).HasColumnName("rating");
            entity.Property(review => review.Body).HasColumnName("body").HasMaxLength(4000).IsRequired();
            entity.Property(review => review.ShowAvatar).HasColumnName("show_avatar");
            entity.Property(review => review.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(review => review.ModerationReason).HasColumnName("moderation_reason").HasMaxLength(500);
            entity.Property(review => review.ModeratedByUserAccountId).HasColumnName("moderated_by_user_account_id");
            entity.Property(review => review.ModeratedAtUtc).HasColumnName("moderated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(review => review.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(review => review.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(review => review.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(review => review.UserAccountId).IsUnique()
                .HasDatabaseName("ux_store_reviews_user");
            entity.HasIndex(review => new { review.Status, review.UpdatedAtUtc })
                .HasDatabaseName("ix_store_reviews_status_updated");
        });
    }
}

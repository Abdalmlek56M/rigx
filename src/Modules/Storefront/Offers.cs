using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

public static class OfferStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Cancelled = "Cancelled";
}

public sealed class Offer
{
    public Guid Id { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? EndsAtUtc { get; set; }
    public required string Status { get; set; } = OfferStatus.Draft;
    public int Priority { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<OfferItem> Items { get; set; } = new List<OfferItem>();
}

public sealed class OfferItem
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public Guid ProductId { get; set; }
    public decimal OfferPrice { get; set; }
    public int SortOrder { get; set; }
    public Offer Offer { get; set; } = null!;
}

public sealed record OfferItemView(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string ProductSku,
    decimal BasePrice,
    decimal OfferPrice,
    string? ThumbnailStorageKey,
    string? CategorySlug,
    int SortOrder);

public sealed record OfferView(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string Status,
    int Priority,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion,
    IReadOnlyList<OfferItemView> Items);

public sealed record OfferItemInput(Guid ProductId, decimal OfferPrice);

public sealed record OfferInput(
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    int Priority,
    IReadOnlyList<OfferItemInput>? Items);

public sealed record OfferUpdateRequest(
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    int Priority,
    IReadOnlyList<OfferItemInput>? Items,
    long RowVersion);

public sealed record PublicOfferView(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    IReadOnlyList<OfferItemView> Items);

public static class OfferModelConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<Offer>(entity =>
        {
            entity.ToTable("offers", table =>
            {
                table.HasCheckConstraint("ck_offers_status", "status IN ('Draft','Published','Cancelled')");
                table.HasCheckConstraint("ck_offers_window", "ends_at_utc IS NULL OR ends_at_utc > starts_at_utc");
            });
            entity.HasKey(offer => offer.Id);
            entity.Property(offer => offer.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(offer => offer.TitleAr).HasColumnName("title_ar").HasMaxLength(200).IsRequired();
            entity.Property(offer => offer.TitleEn).HasColumnName("title_en").HasMaxLength(200).IsRequired();
            entity.Property(offer => offer.DescriptionAr).HasColumnName("description_ar").HasMaxLength(1000);
            entity.Property(offer => offer.DescriptionEn).HasColumnName("description_en").HasMaxLength(1000);
            entity.Property(offer => offer.StartsAtUtc).HasColumnName("starts_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(offer => offer.EndsAtUtc).HasColumnName("ends_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(offer => offer.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(offer => offer.Priority).HasColumnName("priority").HasDefaultValue(0);
            entity.Property(offer => offer.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(offer => offer.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(offer => offer.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(offer => new { offer.Status, offer.StartsAtUtc })
                .HasDatabaseName("ix_offers_status_start");
            entity.HasMany(offer => offer.Items).WithOne(item => item.Offer)
                .HasForeignKey(item => item.OfferId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<OfferItem>(entity =>
        {
            entity.ToTable("offer_items", table =>
            {
                table.HasCheckConstraint("ck_offer_items_price", "offer_price > 0");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.OfferId).HasColumnName("offer_id");
            entity.Property(item => item.ProductId).HasColumnName("product_id");
            entity.Property(item => item.OfferPrice).HasColumnName("offer_price").HasPrecision(18, 2);
            entity.Property(item => item.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.HasIndex(item => new { item.OfferId, item.ProductId }).IsUnique()
                .HasDatabaseName("ux_offer_items_offer_product");
        });
    }
}

public sealed class OfferService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PublicOfferView>> GetPublicAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var offers = await db.Offers.AsNoTracking()
            .Include(offer => offer.Items)
            .Where(offer => offer.Status == OfferStatus.Published &&
                            offer.StartsAtUtc <= now &&
                            (offer.EndsAtUtc == null || offer.EndsAtUtc >= now))
            .OrderByDescending(offer => offer.Priority)
            .ThenByDescending(offer => offer.StartsAtUtc)
            .ToListAsync(cancellationToken);
        var views = new List<PublicOfferView>();
        foreach (var offer in offers)
        {
            var items = await MapItemsAsync(db, offer.Items, cancellationToken);
            if (items.Count > 0)
                views.Add(new PublicOfferView(
                    offer.Id, offer.TitleAr, offer.TitleEn, offer.DescriptionAr, offer.DescriptionEn,
                    offer.StartsAtUtc, offer.EndsAtUtc, items));
        }
        return views;
    }

    public async Task<IReadOnlyList<OfferView>> ListAdminAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var offers = await db.Offers.AsNoTracking()
            .Include(offer => offer.Items)
            .OrderByDescending(offer => offer.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        var views = new List<OfferView>();
        foreach (var offer in offers)
            views.Add(await MapAsync(db, offer, cancellationToken));
        return views;
    }

    public async Task<OfferView> CreateAsync(
        OfferInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = await NormalizeAsync(input, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var offer = new Offer
        {
            Id = Guid.CreateVersion7(),
            TitleAr = normalized.TitleAr,
            TitleEn = normalized.TitleEn,
            DescriptionAr = normalized.DescriptionAr,
            DescriptionEn = normalized.DescriptionEn,
            StartsAtUtc = normalized.StartsAtUtc,
            EndsAtUtc = normalized.EndsAtUtc,
            Priority = normalized.Priority,
            Status = OfferStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        foreach (var item in normalized.Items)
        {
            offer.Items.Add(new OfferItem
            {
                Id = Guid.CreateVersion7(),
                ProductId = item.ProductId,
                OfferPrice = item.OfferPrice,
                SortOrder = item.SortOrder
            });
        }
        db.Offers.Add(offer);
        await db.SaveChangesAsync(cancellationToken);
        return await MapAsync(db, offer, cancellationToken);
    }

    public async Task<OfferView> UpdateAsync(
        Guid id,
        OfferUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = await NormalizeAsync(new OfferInput(
            request.TitleAr, request.TitleEn, request.DescriptionAr, request.DescriptionEn,
            request.StartsAtUtc, request.EndsAtUtc, request.Priority, request.Items), cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var offer = await db.Offers.Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Offer was not found.");
        if (offer.Status == OfferStatus.Cancelled)
            throw new StorefrontException(409, "A cancelled offer cannot be edited.");
        if (offer.RowVersion != request.RowVersion)
            throw new StorefrontException(409, "The offer was changed by another user.");
        offer.TitleAr = normalized.TitleAr;
        offer.TitleEn = normalized.TitleEn;
        offer.DescriptionAr = normalized.DescriptionAr;
        offer.DescriptionEn = normalized.DescriptionEn;
        offer.StartsAtUtc = normalized.StartsAtUtc;
        offer.EndsAtUtc = normalized.EndsAtUtc;
        offer.Priority = normalized.Priority;
        offer.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        offer.RowVersion++;
        db.OfferItems.RemoveRange(offer.Items);
        offer.Items.Clear();
        foreach (var item in normalized.Items)
        {
            offer.Items.Add(new OfferItem
            {
                Id = Guid.CreateVersion7(),
                OfferId = offer.Id,
                ProductId = item.ProductId,
                OfferPrice = item.OfferPrice,
                SortOrder = item.SortOrder
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return await MapAsync(db, offer, cancellationToken);
    }

    public async Task<OfferView> SetPublishedAsync(
        Guid id,
        bool published,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var offer = await db.Offers.Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Offer was not found.");
        if (offer.Status == OfferStatus.Cancelled)
            throw new StorefrontException(409, "A cancelled offer cannot be published.");
        if (offer.RowVersion != rowVersion)
            throw new StorefrontException(409, "The offer was changed by another user.");
        if (published)
        {
            var publishedProducts = await db.WishlistProducts.AsNoTracking()
                .Where(product => product.Status == "Published")
                .Select(product => product.Id)
                .ToListAsync(cancellationToken);
            var missing = offer.Items
                .Where(item => !publishedProducts.Contains(item.ProductId))
                .Select(item => item.ProductId)
                .ToList();
            if (missing.Count > 0)
                throw new StorefrontException(400, "Every offer item must reference a published product.");
        }
        offer.Status = published ? OfferStatus.Published : OfferStatus.Draft;
        offer.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        offer.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return await MapAsync(db, offer, cancellationToken);
    }

    public async Task<OfferView> CancelAsync(
        Guid id,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var offer = await db.Offers.Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Offer was not found.");
        if (offer.Status == OfferStatus.Cancelled)
            return await MapAsync(db, offer, cancellationToken);
        if (offer.RowVersion != rowVersion)
            throw new StorefrontException(409, "The offer was changed by another user.");
        offer.Status = OfferStatus.Cancelled;
        offer.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        offer.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return await MapAsync(db, offer, cancellationToken);
    }

    private static async Task<OfferView> MapAsync(
        StorefrontDbContext db,
        Offer offer,
        CancellationToken cancellationToken) =>
        new(
            offer.Id,
            offer.TitleAr,
            offer.TitleEn,
            offer.DescriptionAr,
            offer.DescriptionEn,
            offer.StartsAtUtc,
            offer.EndsAtUtc,
            offer.Status,
            offer.Priority,
            offer.CreatedAtUtc,
            offer.UpdatedAtUtc,
            offer.RowVersion,
            await MapItemsAsync(db, offer.Items, cancellationToken));

    private static async Task<IReadOnlyList<OfferItemView>> MapItemsAsync(
        StorefrontDbContext db,
        IEnumerable<OfferItem> items,
        CancellationToken cancellationToken)
    {
        var list = items.OrderBy(item => item.SortOrder).ToList();
        if (list.Count == 0)
            return [];
        var productIds = list.Select(item => item.ProductId).ToArray();
        var products = await db.WishlistProducts.AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        var images = await db.WishlistProductImages.AsNoTracking()
            .Where(image => productIds.Contains(image.ProductId))
            .OrderByDescending(image => image.IsPrimary)
            .ToListAsync(cancellationToken);
        var imageKeys = images.GroupBy(image => image.ProductId)
            .ToDictionary(group => group.Key, group => group.First().ThumbnailStorageKey);
        var categoryIds = products.Values.Select(product => product.CategoryId).Distinct().ToArray();
        var categories = await db.WishlistCategories.AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, category => category.Slug, cancellationToken);
        return list
            .Where(item => products.ContainsKey(item.ProductId))
            .Select(item =>
            {
                var product = products[item.ProductId];
                return new OfferItemView(
                    product.Id,
                    product.Name,
                    product.Slug,
                    product.Sku,
                    product.BasePrice,
                    item.OfferPrice,
                    imageKeys.GetValueOrDefault(product.Id),
                    categories.GetValueOrDefault(product.CategoryId),
                    item.SortOrder);
            })
            .ToList();
    }

    private async Task<NormalizedOffer> NormalizeAsync(
        OfferInput input,
        CancellationToken cancellationToken)
    {
        var titleAr = Required(input.TitleAr, 200, "Arabic title");
        var titleEn = Required(input.TitleEn, 200, "English title");
        var descriptionAr = Optional(input.DescriptionAr, 1000, "Arabic description");
        var descriptionEn = Optional(input.DescriptionEn, 1000, "English description");
        var startsAt = (input.StartsAtUtc ?? timeProvider.GetUtcNow()).ToUniversalTime();
        var endsAt = input.EndsAtUtc?.ToUniversalTime();
        if (endsAt is not null && endsAt <= startsAt)
            throw new StorefrontException(400, "The offer end must be after its start.");
        var items = input.Items ?? [];
        if (items.Count is < 1 or > 100)
            throw new StorefrontException(400, "An offer needs between 1 and 100 products.");
        if (items.Select(item => item.ProductId).Distinct().Count() != items.Count)
            throw new StorefrontException(400, "An offer cannot contain the same product twice.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var ids = items.Select(item => item.ProductId).ToArray();
        var products = await db.WishlistProducts.AsNoTracking()
            .Where(product => ids.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        var normalizedItems = new List<NormalizedOfferItem>();
        var sort = 0;
        foreach (var item in items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new StorefrontException(400, "An offer item references a product that does not exist.");
            if (item.OfferPrice <= 0 || item.OfferPrice >= product.BasePrice)
                throw new StorefrontException(400, "The offer price must be positive and below the product base price.");
            normalizedItems.Add(new NormalizedOfferItem(item.ProductId, Math.Round(item.OfferPrice, 2), sort++));
        }
        return new NormalizedOffer(
            titleAr, titleEn, descriptionAr, descriptionEn, startsAt, endsAt,
            Math.Clamp(input.Priority, -1000, 1000), normalizedItems);
    }

    private static string Required(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new StorefrontException(400, $"{field} is required.");
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > max)
            throw new StorefrontException(400, $"{field} is too long.");
        if (text.Contains('<') || text.Contains('>'))
            throw new StorefrontException(400, $"{field} must be plain text.");
        return text;
    }

    private static string? Optional(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > max)
            throw new StorefrontException(400, $"{field} is too long.");
        if (text.Contains('<') || text.Contains('>'))
            throw new StorefrontException(400, $"{field} must be plain text.");
        return text;
    }

    private sealed record NormalizedOffer(
        string TitleAr,
        string TitleEn,
        string? DescriptionAr,
        string? DescriptionEn,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset? EndsAtUtc,
        int Priority,
        IReadOnlyList<NormalizedOfferItem> Items);

    private sealed record NormalizedOfferItem(Guid ProductId, decimal OfferPrice, int SortOrder);
}

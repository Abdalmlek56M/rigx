using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

public sealed class WishlistItem
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public Guid ProductId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class WishlistProductReference
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Sku { get; set; }
    public decimal BasePrice { get; set; }
    public string Status { get; set; } = "Draft";
}

public sealed class WishlistCategoryReference
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
}

public sealed class WishlistProductImageReference
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string StorageKey { get; set; }
    public required string ThumbnailStorageKey { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class ArticleVideoReference
{
    public Guid ArticleId { get; set; }
    public Guid VideoId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ContactMessage
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Subject { get; set; }
    public required string Message { get; set; }
    public required string Status { get; set; } = "New";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed record ContactMessageView(Guid Id, string FullName, string Email, string? Phone, string Subject,
    string Message, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion);
public sealed record ContactMessagePage(IReadOnlyList<ContactMessageView> Items, int Page, int PageSize, int Total);

public sealed class ContactService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task SubmitAsync(string? fullName, string? email, string? phone, string? subject, string? message,
        string? honeypot, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(honeypot)) return;
        var name = Required(fullName, 2, 120, "name");
        var address = Email(email);
        var topic = Required(subject, 3, 200, "subject");
        var body = Required(message, 10, 4000, "message");
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        if (normalizedPhone is { Length: > 32 }) throw new StorefrontException(400, "Phone is too long.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var since = timeProvider.GetUtcNow().ToUniversalTime().AddMinutes(-10);
        var duplicate = await db.ContactMessages.AnyAsync(item =>
            item.Email == address && item.Message == body && item.CreatedAtUtc >= since, cancellationToken);
        if (duplicate) return;
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        db.ContactMessages.Add(new ContactMessage
        {
            Id = Guid.CreateVersion7(), FullName = name, Email = address, Phone = normalizedPhone,
            Subject = topic, Message = body, Status = "New", CreatedAtUtc = now, UpdatedAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContactMessagePage> ListAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ContactMessages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status.Trim());
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new ContactMessageView(item.Id, item.FullName, item.Email, item.Phone, item.Subject,
                item.Message, item.Status, item.CreatedAtUtc, item.UpdatedAtUtc, item.RowVersion))
            .ToListAsync(cancellationToken);
        return new ContactMessagePage(items, page, pageSize, total);
    }

    public async Task<ContactMessageView> SetStatusAsync(Guid id, string? status, long rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (status is not ("New" or "Read" or "Closed"))
            throw new StorefrontException(400, "Status must be New, Read, or Closed.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ContactMessages.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Message was not found.");
        if (entity.RowVersion != rowVersion)
            throw new StorefrontException(409, "The message was changed by another user.");
        entity.Status = status;
        entity.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        entity.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return new ContactMessageView(entity.Id, entity.FullName, entity.Email, entity.Phone, entity.Subject,
            entity.Message, entity.Status, entity.CreatedAtUtc, entity.UpdatedAtUtc, entity.RowVersion);
    }

    private static string Required(string? value, int min, int max, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < min || trimmed.Length > max)
            throw new StorefrontException(400, $"{field} must be between {min} and {max} characters.");
        return trimmed;
    }

    private static string Email(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 255 ||
            !System.Net.Mail.MailAddress.TryCreate(trimmed, out _))
            throw new StorefrontException(400, "A valid email address is required.");
        return trimmed;
    }
}

public sealed class ContactMessageConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<ContactMessage>(entity =>
        {
            entity.ToTable("contact_messages", table =>
                table.HasCheckConstraint("ck_contact_messages_status", "status IN ('New','Read','Closed')"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.FullName).HasColumnName("full_name").HasMaxLength(120).IsRequired();
            entity.Property(item => item.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(item => item.Phone).HasColumnName("phone").HasMaxLength(32);
            entity.Property(item => item.Subject).HasColumnName("subject").HasMaxLength(200).IsRequired();
            entity.Property(item => item.Message).HasColumnName("message").HasMaxLength(4000).IsRequired();
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(item => item.CreatedAtUtc).HasDatabaseName("ix_contact_messages_created");
            entity.HasIndex(item => new { item.Email, item.CreatedAtUtc }).HasDatabaseName("ix_contact_messages_email_created");
        });
    }
}

public sealed class StorefrontDbContext(DbContextOptions<StorefrontDbContext> options) : DbContext(options)
{
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<WishlistProductReference> WishlistProducts => Set<WishlistProductReference>();
    public DbSet<WishlistProductImageReference> WishlistProductImages => Set<WishlistProductImageReference>();
    public DbSet<WishlistCategoryReference> WishlistCategories => Set<WishlistCategoryReference>();
    public DbSet<ContentArticle> ContentArticles => Set<ContentArticle>();
    public DbSet<ContentArticleRevision> ContentArticleRevisions => Set<ContentArticleRevision>();
    public DbSet<ContentFaq> ContentFaqs => Set<ContentFaq>();
    public DbSet<ContentPolicy> ContentPolicies => Set<ContentPolicy>();
    public DbSet<ContentBrandReference> ContentBrands => Set<ContentBrandReference>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferItem> OfferItems => Set<OfferItem>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<ArticleVideoReference> ArticleVideos => Set<ArticleVideoReference>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        ContactMessageConfiguration.Configure(model);
        model.Entity<ArticleVideoReference>(entity =>
        {
            entity.ToTable("article_video_references");
            entity.HasKey(item => new { item.ArticleId, item.VideoId });
            entity.Property(item => item.ArticleId).HasColumnName("article_id");
            entity.Property(item => item.VideoId).HasColumnName("video_id");
            entity.Property(item => item.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(item => item.VideoId).HasDatabaseName("ix_article_video_references_video");
            entity.HasOne<ContentArticle>().WithMany().HasForeignKey(item => item.ArticleId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<WishlistItem>(entity =>
        {
            entity.ToTable("wishlist_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.UserAccountId).HasColumnName("user_account_id");
            entity.Property(item => item.ProductId).HasColumnName("product_id");
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.HasIndex(item => new { item.UserAccountId, item.ProductId }).IsUnique()
                .HasDatabaseName("ux_wishlist_items_account_product");
            entity.HasIndex(item => item.UserAccountId).HasDatabaseName("ix_wishlist_items_account");
        });

        model.Entity<WishlistProductReference>(entity =>
        {
            entity.ToTable("products", table => table.ExcludeFromMigrations());
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(product => product.CategoryId).HasColumnName("category_id");
            entity.Property(product => product.Name).HasColumnName("name");
            entity.Property(product => product.Slug).HasColumnName("slug");
            entity.Property(product => product.Sku).HasColumnName("sku");
            entity.Property(product => product.BasePrice).HasColumnName("base_price");
            entity.Property(product => product.Status).HasColumnName("status");
        });

        model.Entity<WishlistProductImageReference>(entity =>
        {
            entity.ToTable("product_images", table => table.ExcludeFromMigrations());
            entity.HasKey(image => image.Id);
            entity.Property(image => image.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(image => image.ProductId).HasColumnName("product_id");
            entity.Property(image => image.StorageKey).HasColumnName("storage_key");
            entity.Property(image => image.ThumbnailStorageKey).HasColumnName("thumbnail_storage_key");
            entity.Property(image => image.IsPrimary).HasColumnName("is_primary");
        });

        model.Entity<WishlistCategoryReference>(entity =>
        {
            entity.ToTable("categories", table => table.ExcludeFromMigrations());
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(category => category.Slug).HasColumnName("slug");
        });

        StoreSettingsConfiguration.Configure(model);
        ContentModelConfiguration.Configure(model);
        OfferModelConfiguration.Configure(model);
    }
}

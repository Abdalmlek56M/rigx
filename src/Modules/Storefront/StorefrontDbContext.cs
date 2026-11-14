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
    public DbSet<CustomerRequest> CustomerRequests => Set<CustomerRequest>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        CustomerRequestConfiguration.Configure(model);
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

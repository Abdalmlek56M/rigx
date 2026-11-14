using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

public sealed class ContentMediaAsset : ISoftDeletable
{
    public Guid Id { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public required string AltAr { get; set; }
    public required string AltEn { get; set; }
    public string? CaptionAr { get; set; }
    public string? CaptionEn { get; set; }
    public string TagsCsv { get; set; } = "";
    public required string StorageKey { get; set; }
    public required string ThumbnailStorageKey { get; set; }
    public required string OriginalStorageKey { get; set; }
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public Guid? UploadedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class ContentMediaAssetUsage
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public required string ReferenceType { get; set; }
    public Guid ReferenceId { get; set; }
    public required string ReferenceTitle { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public static class ContentMediaModelConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<ContentMediaAsset>(entity =>
        {
            entity.ToTable("content_media_assets", table =>
            {
                table.HasCheckConstraint("ck_content_media_assets_dimensions",
                    "width > 0 AND height > 0 AND thumbnail_width > 0 AND thumbnail_height > 0");
                table.HasCheckConstraint("ck_content_media_assets_size", "file_size_bytes > 0");
                table.HasCheckConstraint("ck_content_media_assets_storage_not_blank",
                    "length(btrim(storage_key)) > 0 AND length(btrim(thumbnail_storage_key)) > 0");
            });
            entity.HasKey(asset => asset.Id);
            entity.Property(asset => asset.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(asset => asset.TitleAr).HasColumnName("title_ar").HasMaxLength(200).IsRequired();
            entity.Property(asset => asset.TitleEn).HasColumnName("title_en").HasMaxLength(200).IsRequired();
            entity.Property(asset => asset.AltAr).HasColumnName("alt_ar").HasMaxLength(300).IsRequired();
            entity.Property(asset => asset.AltEn).HasColumnName("alt_en").HasMaxLength(300).IsRequired();
            entity.Property(asset => asset.CaptionAr).HasColumnName("caption_ar").HasMaxLength(500);
            entity.Property(asset => asset.CaptionEn).HasColumnName("caption_en").HasMaxLength(500);
            entity.Property(asset => asset.TagsCsv).HasColumnName("tags_csv").HasMaxLength(512).IsRequired();
            entity.Property(asset => asset.StorageKey).HasColumnName("storage_key").HasMaxLength(512).IsRequired();
            entity.HasIndex(asset => asset.StorageKey).IsUnique().HasDatabaseName("ux_content_media_assets_storage_key");
            entity.Property(asset => asset.ThumbnailStorageKey).HasColumnName("thumbnail_storage_key").HasMaxLength(512).IsRequired();
            entity.Property(asset => asset.OriginalStorageKey).HasColumnName("original_storage_key").HasMaxLength(512).IsRequired();
            entity.Property(asset => asset.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(asset => asset.Width).HasColumnName("width");
            entity.Property(asset => asset.Height).HasColumnName("height");
            entity.Property(asset => asset.ThumbnailWidth).HasColumnName("thumbnail_width");
            entity.Property(asset => asset.ThumbnailHeight).HasColumnName("thumbnail_height");
            entity.Property(asset => asset.UploadedByUserAccountId).HasColumnName("uploaded_by_user_account_id");
            entity.Property(asset => asset.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(asset => asset.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(asset => asset.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(asset => new { asset.DeletedAtUtc, asset.UpdatedAtUtc })
                .HasDatabaseName("ix_content_media_assets_active");
        });

        model.Entity<ContentMediaAssetUsage>(entity =>
        {
            entity.ToTable("content_media_asset_usages");
            entity.HasKey(usage => usage.Id);
            entity.Property(usage => usage.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(usage => usage.AssetId).HasColumnName("asset_id");
            entity.Property(usage => usage.ReferenceType).HasColumnName("reference_type").HasMaxLength(32).IsRequired();
            entity.Property(usage => usage.ReferenceId).HasColumnName("reference_id");
            entity.Property(usage => usage.ReferenceTitle).HasColumnName("reference_title").HasMaxLength(220).IsRequired();
            entity.Property(usage => usage.IsPublished).HasColumnName("is_published");
            entity.Property(usage => usage.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.HasIndex(usage => new { usage.AssetId, usage.IsPublished })
                .HasDatabaseName("ix_content_media_asset_usages_asset");
            entity.HasIndex(usage => new { usage.ReferenceType, usage.ReferenceId, usage.AssetId })
                .HasDatabaseName("ux_content_media_asset_usages_reference_asset")
                .IsUnique();
            entity.HasOne<ContentMediaAsset>().WithMany().HasForeignKey(usage => usage.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

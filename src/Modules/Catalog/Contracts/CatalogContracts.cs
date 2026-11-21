namespace HardwareStore.Modules.Catalog;

public sealed record CategoryInput(
    string? Name,
    string? Slug,
    Guid? ParentId,
    string? Description,
    string? IconStorageKey,
    int SortOrder,
    bool IsActive,
    int? RowVersion,
    string? Code = null,
    bool ShowInMenu = true,
    bool ShowInBuilder = false,
    string? NameEn = null);

/// <summary>Replaces a category's own spec definitions; children inherit them.</summary>
public sealed record CategorySpecsInput(IReadOnlyList<SpecDefinition>? Specs, int? RowVersion);

public sealed record BrandInput(
    string? Name,
    string? Slug,
    string? Description,
    string? LogoStorageKey,
    string? SupportUrl,
    bool IsActive,
    int? RowVersion);

public sealed record ProductSpecificationInput(
    Guid? Id,
    string? GroupName,
    string? Name,
    string? Value,
    int SortOrder);

public sealed record ProductInput(
    string? Name,
    string? Slug,
    string? Sku,
    string? Barcode,
    Guid CategoryId,
    Guid? BrandId,
    string? ShortDescription,
    string? Description,
    decimal BasePrice,
    CatalogProductStatus Status,
    CatalogProductCondition Condition,
    IReadOnlyList<ProductSpecificationInput>? Specifications,
    int? RowVersion);

public sealed record CategoryView(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? IconStorageKey,
    int SortOrder,
    bool IsActive,
    bool IsEffectivelyActive,
    int RowVersion,
    string? Code,
    bool ShowInMenu,
    bool ShowInBuilder,
    IReadOnlyList<SpecDefinition> Specs,
    IReadOnlyList<CategoryView> Children,
    string? NameEn = null);

public sealed record BrandView(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoStorageKey,
    string? SupportUrl,
    bool IsActive,
    int RowVersion);

public sealed record ProductSpecificationView(
    Guid Id,
    string GroupName,
    string Name,
    string Value,
    int SortOrder);

public sealed record ProductSpecificationSummaryView(
    string GroupName,
    string Name,
    string Value);

public sealed record ProductImageView(
    Guid Id,
    string StorageKey,
    string ThumbnailStorageKey,
    string AltText,
    int SortOrder,
    bool IsPrimary,
    int Width,
    int Height,
    int ThumbnailWidth,
    int ThumbnailHeight);

public sealed record ProductSummaryView(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    decimal BasePrice,
    CatalogProductStatus Status,
    CatalogProductCondition Condition,
    string CategoryName,
    string CategorySlug,
    string? BrandName,
    IReadOnlyList<ProductSpecificationSummaryView> Specifications,
    string? PrimaryImageStorageKey);

public sealed record ProductDetailView(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    string? Barcode,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    Guid? BrandId,
    string? BrandName,
    string? ShortDescription,
    string? Description,
    decimal BasePrice,
    CatalogProductStatus Status,
    CatalogProductCondition Condition,
    int RowVersion,
    IReadOnlyList<ProductSpecificationView> Specifications,
    IReadOnlyList<ProductImageView> Images);

public sealed record ProductSuggestionView(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    decimal BasePrice,
    string Condition,
    string CategorySlug,
    string? BrandName,
    string? ThumbnailStorageKey);

public sealed record SpecificationFilterView(
    string GroupName,
    string Name,
    IReadOnlyList<string> Values);

public enum CatalogProductSort
{
    Newest,
    Oldest,
    PriceAscending,
    PriceDescending,
    NameAscending,
    NameDescending
}

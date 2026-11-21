namespace HardwareStore.Modules.Catalog;

/// <summary>Entity → view mapping reused after writes.</summary>
internal static class CatalogViews
{
    public static CategoryView ToView(this CatalogCategory category, bool isEffectivelyActive) => new(
        category.Id, category.ParentId, category.Name, category.Slug,
        category.Description, category.IconStorageKey, category.SortOrder,
        category.IsActive, isEffectivelyActive, category.RowVersion, category.Code, category.ShowInMenu, category.ShowInBuilder,
        category.SpecDefinitions, Array.Empty<CategoryView>(), category.NameEn,
        new CategoryPriceOptions(category.PriceAlone, category.PriceWithBoard, category.PriceInBuild));

    public static BrandView ToView(this CatalogBrand brand) => new(
        brand.Id, brand.Name, brand.Slug, brand.Description,
        brand.LogoStorageKey, brand.SupportUrl, brand.IsActive, brand.RowVersion);
}

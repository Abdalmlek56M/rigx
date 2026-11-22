using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>
/// Category icons and brand logos: store the new image, then delete the previous one (new file is removed if saving fails).
/// </summary>
public sealed class TaxonomyImageService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    ICatalogMediaStorage mediaStorage,
    TimeProvider timeProvider)
{
    public async Task<CategoryView> SetCategoryImageAsync(
        Guid categoryId,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.SingleOrDefaultAsync(
            item => item.Id == categoryId, cancellationToken)
            ?? throw new CatalogNotFoundException("Category was not found.");
        var previous = category.IconStorageKey;
        var assetId = Guid.CreateVersion7();
        category.IconStorageKey = await mediaStorage.SaveAssetImageAsync(
            "categories", categoryId, assetId, image, cancellationToken);
        category.RowVersion++;
        category.UpdatedAtUtc = UtcNow();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await mediaStorage.DeleteStorageKeyAsync(category.IconStorageKey, CancellationToken.None);
            throw;
        }
        if (!string.IsNullOrWhiteSpace(previous))
            await mediaStorage.DeleteStorageKeyAsync(previous, CancellationToken.None);
        return category.ToView(await CategoryHierarchy.IsEffectivelyActiveAsync(db, category.Id, cancellationToken));
    }

    public async Task<CategoryView> DeleteCategoryImageAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.SingleOrDefaultAsync(
            item => item.Id == categoryId, cancellationToken)
            ?? throw new CatalogNotFoundException("Category was not found.");
        var previous = category.IconStorageKey;
        category.IconStorageKey = null;
        category.RowVersion++;
        category.UpdatedAtUtc = UtcNow();
        await db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(previous))
            await mediaStorage.DeleteStorageKeyAsync(previous, CancellationToken.None);
        return category.ToView(await CategoryHierarchy.IsEffectivelyActiveAsync(db, category.Id, cancellationToken));
    }

    public async Task<BrandView> SetBrandLogoAsync(
        Guid brandId,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var brand = await db.Brands.Include(item => item.Categories).SingleOrDefaultAsync(item => item.Id == brandId, cancellationToken)
            ?? throw new CatalogNotFoundException("Brand was not found.");
        var previous = brand.LogoStorageKey;
        var assetId = Guid.CreateVersion7();
        brand.LogoStorageKey = await mediaStorage.SaveAssetImageAsync(
            "brands", brandId, assetId, image, cancellationToken);
        brand.RowVersion++;
        brand.UpdatedAtUtc = UtcNow();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await mediaStorage.DeleteStorageKeyAsync(brand.LogoStorageKey, CancellationToken.None);
            throw;
        }
        if (!string.IsNullOrWhiteSpace(previous))
            await mediaStorage.DeleteStorageKeyAsync(previous, CancellationToken.None);
        return brand.ToView();
    }

    public async Task<BrandView> DeleteBrandLogoAsync(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var brand = await db.Brands.Include(item => item.Categories).SingleOrDefaultAsync(item => item.Id == brandId, cancellationToken)
            ?? throw new CatalogNotFoundException("Brand was not found.");
        var previous = brand.LogoStorageKey;
        brand.LogoStorageKey = null;
        brand.RowVersion++;
        brand.UpdatedAtUtc = UtcNow();
        await db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(previous))
            await mediaStorage.DeleteStorageKeyAsync(previous, CancellationToken.None);
        return brand.ToView();
    }

    private DateTimeOffset UtcNow() => timeProvider.GetUtcNow().ToUniversalTime();
}

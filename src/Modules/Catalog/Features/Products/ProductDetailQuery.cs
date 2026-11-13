using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>Product detail projection shared by the public product page and the admin editor.</summary>
internal static class ProductDetailQuery
{
    public static async Task<ProductDetailView> LoadAsync(
        CatalogDbContext db,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await db.Products.AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => new ProductDetailView(
                product.Id,
                product.Name,
                product.Slug,
                product.Sku,
                product.Barcode,
                product.CategoryId,
                product.Category.Name,
                product.Category.Slug,
                product.BrandId,
                product.Brand != null ? product.Brand.Name : null,
                product.ShortDescription,
                product.Description,
                product.BasePrice,
                product.Status,
                product.Condition,
                product.RowVersion,
                product.Specifications
                    .OrderBy(specification => specification.SortOrder)
                    .ThenBy(specification => specification.Name)
                    .Select(specification => new ProductSpecificationView(
                        specification.Id, specification.GroupName, specification.Name,
                        specification.Value, specification.SortOrder))
                    .ToList(),
                product.Images.OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.SortOrder)
                    .Select(image => new ProductImageView(
                        image.Id, image.StorageKey, image.ThumbnailStorageKey,
                        image.AltText, image.SortOrder, image.IsPrimary,
                        image.Width, image.Height, image.ThumbnailWidth, image.ThumbnailHeight))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CatalogNotFoundException("Product was not found.");
    }
}

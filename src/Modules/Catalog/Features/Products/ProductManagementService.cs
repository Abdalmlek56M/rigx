using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>
/// Admin product editing: list, load, create/update with specifications, and status changes
/// (publishing requires an effectively active category and an active brand).
/// </summary>
public sealed class ProductManagementService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    TimeProvider timeProvider)
{
    /// <summary>Products of any status whose name contains the text (LIKE wildcards in it are literal), by name, at most <paramref name="limit"/>.</summary>
    public async Task<IReadOnlyList<ProductNameMatch>> FindNamesAsync(string text, int limit, CancellationToken cancellationToken = default)
    {
        var pattern = LikePattern.Contains(text);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Products.AsNoTracking().Where(product => EF.Functions.ILike(product.Name, pattern, LikePattern.Escape))
            .OrderBy(product => product.Name).Take(limit)
            .Select(product => new ProductNameMatch(product.Id, product.Name, product.CategoryId)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSummaryView>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Products.AsNoTracking()
            .OrderByDescending(product => product.UpdatedAtUtc).ThenBy(product => product.Name)
            .Select(product => new ProductSummaryView(
                product.Id, product.Name, product.Slug, product.Sku,
                product.BasePrice,
                product.Status,
                product.Condition,
                product.Category.Name, product.Category.Slug,
                product.Brand != null ? product.Brand.Name : null,
                product.Specifications
                    .OrderBy(specification => specification.SortOrder)
                    .ThenBy(specification => specification.Name)
                    .Take(3)
                    .Select(specification => new ProductSpecificationSummaryView(
                        specification.GroupName,
                        specification.Name,
                        specification.Value))
                    .ToList(),
                product.Images.Where(image => image.IsPrimary).Select(image => image.ThumbnailStorageKey).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetailView> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await ProductDetailQuery.LoadAsync(db, id, cancellationToken);
    }

    public async Task<ProductDetailView> SaveProductAsync(
        Guid? id,
        ProductInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateProductInput(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var category = await db.Categories.SingleOrDefaultAsync(
            item => item.Id == input.CategoryId, cancellationToken)
            ?? throw new CatalogValidationException("categoryId", "Category was not found.");
        var brand = input.BrandId is null
            ? null
            : await db.Brands.SingleOrDefaultAsync(item => item.Id == input.BrandId, cancellationToken)
              ?? throw new CatalogValidationException("brandId", "Brand was not found.");
        if (input.Status == CatalogProductStatus.Published &&
            (!await CategoryHierarchy.IsEffectivelyActiveAsync(db, category.Id, cancellationToken) ||
             (brand is not null && !brand.IsActive)))
            throw new CatalogValidationException(
                "status", "A product cannot be published under an inactive category or brand.");

        var product = id is null
            ? new CatalogProduct
            {
                Id = Guid.CreateVersion7(),
                Name = string.Empty,
                Slug = string.Empty,
                Sku = string.Empty,
                CreatedAtUtc = UtcNow()
            }
            : await db.Products
                .Include(item => item.Specifications)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
              ?? throw new CatalogNotFoundException("Product was not found.");
        CatalogValidation.EnsureVersion(product.RowVersion, input.RowVersion, id);

        var sku = CatalogValidation.Required(input.Sku, "sku", 128);
        var barcode = CatalogValidation.Optional(input.Barcode, "barcode", 128);
        if (await db.Products.AnyAsync(
                item => (item.Sku == sku || (barcode != null && item.Barcode == barcode)) &&
                        item.Id != product.Id,
                cancellationToken))
            throw new CatalogConflictException("Product SKU or barcode is already in use.");

        product.Name = CatalogValidation.Required(input.Name, "name", 255);
        product.Slug = CatalogValidation.Slug(input.Slug);
        product.Sku = sku;
        product.Barcode = barcode;
        product.CategoryId = category.Id;
        product.BrandId = brand?.Id;
        product.ShortDescription = CatalogValidation.Optional(input.ShortDescription, "shortDescription", 600);
        product.Description = CatalogValidation.Optional(input.Description, "description", 8000);
        product.BasePrice = CatalogValidation.NonNegativeMoney(input.BasePrice, "basePrice");
        product.Status = input.Status;
        product.Condition = input.Condition;
        product.UpdatedAtUtc = UtcNow();
        if (id is null)
            db.Products.Add(product);
        else
            product.RowVersion++;

        SynchronizeSpecifications(db, product, input.Specifications ?? []);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new CatalogConflictException("Product or specification identifiers conflict with existing data.");
        }

        return await ProductDetailQuery.LoadAsync(db, product.Id, cancellationToken);
    }

    public async Task<ProductDetailView> SetProductStatusAsync(
        Guid id,
        CatalogProductStatus status,
        int rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var product = await db.Products.Include(item => item.Category).Include(item => item.Brand)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new CatalogNotFoundException("Product was not found.");
        CatalogValidation.EnsureVersion(product.RowVersion, rowVersion, id);
        if (status == CatalogProductStatus.Published &&
            (!await CategoryHierarchy.IsEffectivelyActiveAsync(db, product.Category.Id, cancellationToken) ||
             (product.Brand is not null && !product.Brand.IsActive)))
            throw new CatalogValidationException(
                "status", "A product cannot be published under an inactive category or brand.");
        product.Status = status;
        product.RowVersion++;
        product.UpdatedAtUtc = UtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return await ProductDetailQuery.LoadAsync(db, product.Id, cancellationToken);
    }

    private DateTimeOffset UtcNow() => timeProvider.GetUtcNow().ToUniversalTime();

    private static void ValidateProductInput(ProductInput input)
    {
        CatalogValidation.Required(input.Name, "name", 255);
        CatalogValidation.Slug(input.Slug);
        CatalogValidation.Required(input.Sku, "sku", 128);
        CatalogValidation.Optional(input.Barcode, "barcode", 128);
        CatalogValidation.Optional(input.ShortDescription, "shortDescription", 600);
        CatalogValidation.Optional(input.Description, "description", 8000);
        CatalogValidation.NonNegativeMoney(input.BasePrice, "basePrice");
        if (input.CategoryId == Guid.Empty)
            throw new CatalogValidationException("categoryId", "Category is required.");

        var specifications = input.Specifications ?? [];
        foreach (var specification in specifications)
        {
            CatalogValidation.Required(specification.GroupName, "specifications.groupName", 100);
            CatalogValidation.Required(specification.Name, "specifications.name", 128);
            CatalogValidation.Required(specification.Value, "specifications.value", 500);
            if (specification.SortOrder < 0)
                throw new CatalogValidationException("specifications.sortOrder", "Sort order must not be negative.");
        }
        EnsureUnique(
            specifications.Select(item => $"{item.GroupName!.Trim()}\u001f{item.Name!.Trim().ToLowerInvariant()}"),
            "specifications",
            "Specification names must be unique within each product group.");
    }

    private static void EnsureUnique(IEnumerable<string> values, string field, string message)
    {
        if (values.GroupBy(value => value, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new CatalogValidationException(field, message);
    }

    private static void SynchronizeSpecifications(
        CatalogDbContext db,
        CatalogProduct product,
        IReadOnlyList<ProductSpecificationInput> inputs)
    {
        var retainedIds = inputs.Where(input => input.Id is not null).Select(input => input.Id!.Value).ToHashSet();
        foreach (var removed in product.Specifications.Where(item => !retainedIds.Contains(item.Id)).ToList())
            db.ProductSpecifications.Remove(removed);

        foreach (var input in inputs)
        {
            var specification = input.Id is null
                ? null
                : product.Specifications.SingleOrDefault(item => item.Id == input.Id)
                  ?? throw new CatalogValidationException(
                      "specifications.id", "A specification does not belong to this product.");
            if (specification is null)
            {
                specification = new CatalogProductSpecification
                {
                    Id = Guid.CreateVersion7(),
                    GroupName = string.Empty,
                    Name = string.Empty,
                    Value = string.Empty,
                    Product = product
                };
                product.Specifications.Add(specification);
            }
            specification.GroupName = input.GroupName!.Trim();
            specification.Name = input.Name!.Trim();
            specification.Value = input.Value!.Trim();
            specification.SortOrder = input.SortOrder;
        }
    }
}

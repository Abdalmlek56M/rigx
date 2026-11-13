using HardwareStore.Modules.Catalog;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Testcontainers.PostgreSql;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class CatalogPostgreSqlTests(CatalogPostgreSqlFixture fixture)
    : IClassFixture<CatalogPostgreSqlFixture>
{
    [Fact]
    public async Task Migration_creates_catalog_tables_and_storefront_hides_draft_products()
    {
        await using var db = fixture.CreateContext();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261105162018_M4_Catalog", applied);

        var category = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Graphics Cards",
            Slug = "graphics-cards",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        db.Categories.Add(category);
        db.Products.AddRange(
            NewProduct(category.Id, "Published GPU", "published-gpu", "GPU-001", CatalogProductStatus.Published),
            NewProduct(category.Id, "Draft GPU", "draft-gpu", "GPU-002", CatalogProductStatus.Draft));
        await db.SaveChangesAsync();

        var storefront = new ProductSearchService(new TestCatalogContextFactory(fixture.Options));
        var page = await storefront.GetProductsAsync(
            "graphics-cards", null, null, null, null, null, [], CatalogProductSort.Newest, 1, 20);
        Assert.Single(page.Items);
        Assert.Equal("published-gpu", page.Items[0].Slug);
    }

    [Fact]
    public async Task Search_suggestions_cover_name_sku_brand_and_category_without_private_fields()
    {
        await using var db = fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var category = new CatalogCategory
        {
            Id = Guid.CreateVersion7(), Name = $"Suggest Cards {suffix}", Slug = $"suggest-cards-{suffix}",
            IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var brand = new CatalogBrand
        {
            Id = Guid.CreateVersion7(), Name = $"SuggestBrand{suffix}", Slug = $"suggest-brand-{suffix}",
            IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        db.Categories.Add(category);
        db.Brands.Add(brand);
        var product = NewProduct(category.Id, $"Suggest GPU {suffix}", $"suggest-gpu-{suffix}",
            $"SUG-{suffix}", CatalogProductStatus.Published);
        product.BrandId = brand.Id;
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var storefront = new ProductSearchService(new TestCatalogContextFactory(fixture.Options));
        Assert.Single(await storefront.SuggestAsync($"SuggestBrand{suffix}", 8));
        Assert.Single(await storefront.SuggestAsync($"SUG-{suffix}", 8));
        Assert.Single(await storefront.SuggestAsync($"Suggest Cards {suffix}", 8));
        var byName = await storefront.SuggestAsync($"Suggest GPU {suffix}", 8);
        Assert.Single(byName);
        Assert.Equal($"suggest-gpu-{suffix}", byName[0].Slug);
        Assert.Null(typeof(ProductSuggestionView).GetProperty("AcquisitionCost"));
    }

    [Fact]
    public async Task Storefront_applies_effective_hierarchy_price_filters_sorting_and_pagination()
    {
        await using var db = fixture.CreateContext();
        var inactiveParent = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Retired",
            Slug = "retired",
            IsActive = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var hiddenChild = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            ParentId = inactiveParent.Id,
            Name = "Hidden child",
            Slug = "hidden-child",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var activeCategory = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Active products",
            Slug = "active-products",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        db.Categories.AddRange(inactiveParent, hiddenChild, activeCategory);
        var cheap = NewProduct(
            activeCategory.Id, "Cheap product", "cheap-product", "P-100", CatalogProductStatus.Published);
        cheap.CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2);
        cheap.Variants.Add(new CatalogProductVariant
        {
            Id = Guid.CreateVersion7(),
            Name = "Small",
            Sku = "P-100-S",
            Price = 50m
        });
        cheap.Specifications.Add(new CatalogProductSpecification
        {
            Id = Guid.CreateVersion7(),
            GroupName = "Memory",
            Name = "Capacity",
            Value = "8 GB"
        });
        cheap.Specifications.Add(new CatalogProductSpecification
        {
            Id = Guid.CreateVersion7(),
            GroupName = "Variant",
            Name = "Edition",
            Value = "Small",
            Variant = cheap.Variants.Single()
        });
        var expensive = NewProduct(
            activeCategory.Id, "Expensive product", "expensive-product", "P-300", CatalogProductStatus.Published);
        expensive.BasePrice = 300m;
        expensive.CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
        expensive.Specifications.Add(new CatalogProductSpecification
        {
            Id = Guid.CreateVersion7(),
            GroupName = "Platform",
            Name = "Socket",
            Value = "AM5"
        });
        expensive.Specifications.Add(new CatalogProductSpecification
        {
            Id = Guid.CreateVersion7(),
            GroupName = "Memory",
            Name = "Capacity",
            Value = "16 GB"
        });
        db.Products.AddRange(
            cheap,
            expensive,
            NewProduct(hiddenChild.Id, "Hidden product", "hidden-product", "P-HIDDEN", CatalogProductStatus.Published));
        await db.SaveChangesAsync();

        var storefront = new ProductSearchService(new TestCatalogContextFactory(fixture.Options));
        var filtered = await storefront.GetProductsAsync(
            "active-products", null, null, null, null, 100m, [], CatalogProductSort.PriceAscending, 1, 1);
        Assert.Equal(1, filtered.TotalCount);
        Assert.Equal("cheap-product", filtered.Items.Single().Slug);
        Assert.Contains(filtered.Items.Single().Specifications,
            specification => specification.Name == "Capacity" && specification.Value == "8 GB");

        var allPublished = await storefront.GetProductsAsync(
            "active-products", null, null, null, null, null, [], CatalogProductSort.PriceAscending, 1, 10);
        Assert.Equal(["cheap-product", "expensive-product"], allPublished.Items.Select(item => item.Slug));
        Assert.DoesNotContain(allPublished.Items, item => item.Slug == "hidden-product");

        var newest = await storefront.GetProductsAsync(
            "active-products", null, null, null, null, null, [], CatalogProductSort.Newest, 1, 10);
        Assert.Equal(["expensive-product", "cheap-product"], newest.Items.Select(item => item.Slug));

        var oldest = await storefront.GetProductsAsync(
            "active-products", null, null, null, null, null, [], CatalogProductSort.Oldest, 1, 10);
        Assert.Equal(["cheap-product", "expensive-product"], oldest.Items.Select(item => item.Slug));

        var nameAscending = await storefront.GetProductsAsync(
            "active-products", null, null, null, null, null, [], CatalogProductSort.NameAscending, 1, 10);
        Assert.Equal(["cheap-product", "expensive-product"], nameAscending.Items.Select(item => item.Slug));

        var nameDescending = await storefront.GetProductsAsync(
            "active-products", null, null, null, null, null, [], CatalogProductSort.NameDescending, 1, 10);
        Assert.Equal(["expensive-product", "cheap-product"], nameDescending.Items.Select(item => item.Slug));

        var specificationFiltered = await storefront.GetProductsAsync(
            "active-products", null, null, null, null, null, ["Memory|Capacity|8 GB"],
            CatalogProductSort.NameAscending, 1, 10);
        Assert.Equal("cheap-product", specificationFiltered.Items.Single().Slug);

        var specificationFilters = await storefront.GetSpecificationFiltersAsync();
        var capacityFilter = Assert.Single(specificationFilters, filter =>
            filter.GroupName == "Memory" && filter.Name == "Capacity");
        Assert.Contains("8 GB", capacityFilter.Values);

        var details = await storefront.GetProductBySlugAsync("cheap-product");
        Assert.Equal(50m, details.Variants.Single().Price);
        var variantSpecification = Assert.Single(details.Specifications,
            specification => specification.VariantId is not null);
        Assert.Equal(details.Variants.Single().Id, variantSpecification.VariantId);
    }

    [Fact]
    public async Task Primary_image_is_automatic_and_promoted_after_deleting_current_primary()
    {
        var root = Path.Combine(Path.GetTempPath(), "hardware-store-catalog-tests", Guid.NewGuid().ToString("N"));
        try
        {
            await using var db = fixture.CreateContext();
            var category = new CatalogCategory
            {
                Id = Guid.CreateVersion7(),
                Name = "Media products",
                Slug = $"media-products-{Guid.NewGuid():N}",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Categories.Add(category);
            var product = NewProduct(
                category.Id, "Media product", $"media-product-{Guid.NewGuid():N}",
                $"MEDIA-{Guid.NewGuid():N}", CatalogProductStatus.Published);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var options = new CatalogMediaOptions { RootPath = root };
            var storage = new LocalCatalogMediaStorage(
                options, new SkiaCatalogImageProcessor(options));
            var management = new ProductImageService(
                new TestCatalogContextFactory(fixture.Options),
                storage,
                TimeProvider.System,
                []);
            var first = await management.AddImageAsync(
                product.Id, "First", false, CreatePngUpload());
            var second = await management.AddImageAsync(
                product.Id, "Second", true, CreatePngUpload());

            Assert.True(first.IsPrimary);
            Assert.True(second.IsPrimary);
            await management.DeleteImageAsync(product.Id, second.Id);
            await using var proof = fixture.CreateContext();
            var remaining = await proof.ProductImages.SingleAsync(image => image.ProductId == product.Id);
            Assert.Equal(first.Id, remaining.Id);
            Assert.True(remaining.IsPrimary);
            Assert.False(string.IsNullOrWhiteSpace(remaining.OriginalStorageKey));
            Assert.EndsWith(".original", remaining.OriginalStorageKey);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Taxonomy_rules_enforce_hierarchy_slugs_versions_and_publish_guard()
    {
        var taxonomy = new TaxonomyService(new TestCatalogContextFactory(fixture.Options), TimeProvider.System);
        var products = new ProductManagementService(new TestCatalogContextFactory(fixture.Options), TimeProvider.System);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var parent = await taxonomy.SaveCategoryAsync(null,
            new CategoryInput("Parent", $"parent-{suffix}", null, null, null, 0, true, null));
        var child = await taxonomy.SaveCategoryAsync(null,
            new CategoryInput("Child", $"child-{suffix}", parent.Id, null, null, 1, true, null));
        Assert.True(child.IsEffectivelyActive);

        var cycle = await Assert.ThrowsAsync<CatalogValidationException>(() => taxonomy.SaveCategoryAsync(parent.Id,
            new CategoryInput("Parent", parent.Slug, child.Id, null, null, 0, true, parent.RowVersion)));
        Assert.Contains("parentId", cycle.Errors.Keys);
        await Assert.ThrowsAsync<CatalogConflictException>(() => taxonomy.SaveCategoryAsync(null,
            new CategoryInput("Duplicate", parent.Slug, null, null, null, 0, true, null)));
        await Assert.ThrowsAsync<CatalogConflictException>(() => taxonomy.SaveCategoryAsync(parent.Id,
            new CategoryInput("Parent", parent.Slug, null, null, null, 0, true, parent.RowVersion + 5)));

        var inactiveParent = await taxonomy.SaveCategoryAsync(parent.Id,
            new CategoryInput("Parent", parent.Slug, null, null, null, 0, false, parent.RowVersion));
        Assert.False(inactiveParent.IsEffectivelyActive);
        Assert.False((await taxonomy.GetCategoriesAsync()).Single(item => item.Id == child.Id).IsEffectivelyActive);

        var badUrl = await Assert.ThrowsAsync<CatalogValidationException>(() => taxonomy.SaveBrandAsync(null,
            new BrandInput("Brand", $"brand-{suffix}", null, null, "ftp://example.test", true, null)));
        Assert.Contains("supportUrl", badUrl.Errors.Keys);
        var brand = await taxonomy.SaveBrandAsync(null,
            new BrandInput("Brand", $"brand-{suffix}", null, null, "https://example.test", true, null));
        Assert.Contains(await taxonomy.GetBrandsAsync(), item => item.Id == brand.Id);

        var draft = await products.SaveProductAsync(null, new ProductInput("Guarded", $"guarded-{suffix}",
            $"GUARD-{suffix}", null, child.Id, brand.Id, null, null, 10m, CatalogProductStatus.Draft,
            CatalogProductCondition.New, [], [], null));
        var blocked = await Assert.ThrowsAsync<CatalogValidationException>(() =>
            products.SetProductStatusAsync(draft.Id, CatalogProductStatus.Published, draft.RowVersion));
        Assert.Contains("status", blocked.Errors.Keys);
    }

    [Fact]
    public async Task Product_management_persists_variant_price_and_variant_specific_specifications()
    {
        await using var db = fixture.CreateContext();
        var category = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Variant products",
            Slug = $"variant-products-{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var management = new ProductManagementService(
            new TestCatalogContextFactory(fixture.Options),
            TimeProvider.System);

        var created = await management.SaveProductAsync(null, new ProductInput(
            "Variant product",
            $"variant-product-{Guid.NewGuid():N}",
            $"VARIANT-{Guid.NewGuid():N}",
            null,
            category.Id,
            null,
            "Variant test",
            "Variant CRUD test",
            100m,
            CatalogProductStatus.Draft,
            CatalogProductCondition.New,
            [new ProductVariantInput(null, "Large", "VARIANT-LARGE", null, 125m, true)],
            [new ProductSpecificationInput(null, null, "VARIANT-LARGE", "Capacity", "Capacity", "16 GB", 0)],
            null));

        Assert.Equal(125m, created.Variants.Single().Price);
        Assert.Equal(CatalogProductCondition.New, created.Condition);
        Assert.Equal(created.Variants.Single().Id, created.Specifications.Single().VariantId);

        var updated = await management.SaveProductAsync(created.Id, new ProductInput(
            created.Name,
            created.Slug,
            created.Sku,
            null,
            created.CategoryId,
            null,
            created.ShortDescription,
            created.Description,
            created.BasePrice,
            created.Status,
            CatalogProductCondition.Used,
            [new ProductVariantInput(created.Variants.Single().Id, "Large", "VARIANT-LARGE", null, 150m, true)],
            [new ProductSpecificationInput(null, null, "VARIANT-LARGE", "Capacity", "Capacity", "24 GB", 0)],
            created.RowVersion));

        Assert.Equal(150m, updated.Variants.Single().Price);
        Assert.Equal(CatalogProductCondition.Used, updated.Condition);
        Assert.Equal("24 GB", updated.Specifications.Single().Value);
    }

    private static CatalogProduct NewProduct(
        Guid categoryId,
        string name,
        string slug,
        string sku,
        CatalogProductStatus status) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CategoryId = categoryId,
            Name = name,
            Slug = slug,
            Sku = sku,
            BasePrice = 100m,
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

    private static CatalogImageUpload CreatePngUpload()
    {
        using var bitmap = new SKBitmap(40, 30);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.SteelBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100)!;
        var bytes = data.ToArray();
        return new CatalogImageUpload(
            new MemoryStream(bytes), bytes.Length, "preview.png", "image/png");
    }

    private sealed class TestCatalogContextFactory(DbContextOptions<CatalogDbContext> options)
        : IDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext() => new(options);
        public Task<CatalogDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}

public sealed class CatalogPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("catalog_m4_test")
        .WithUsername("catalog_test")
        .WithPassword("test-only-password")
        .Build();

    public DbContextOptions<CatalogDbContext> Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public CatalogDbContext CreateContext() => new(Options);
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}


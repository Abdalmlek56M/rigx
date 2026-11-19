using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class CatalogModelTests
{
    [Fact]
    public void Catalog_model_contains_only_the_six_approved_tables_and_restrictive_relationships()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=none;Password=none")
            .Options;
        using var context = new CatalogDbContext(options);
        var designModel = context.GetService<IDesignTimeModel>().Model;

        var tables = designModel.GetEntityTypes()
            .Select(entity => entity.GetTableName()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "brands", "categories", "content_media_asset_usages", "content_media_assets",
                "product_images", "product_reviews", "product_specifications", "products", "store_reviews"
            },
            tables);
        var allowedCascades = new HashSet<string>(["ContentMediaAssetUsage", "ProductReview"], StringComparer.Ordinal);
        Assert.All(
            designModel.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()),
            foreignKey => Assert.Equal(allowedCascades.Contains(foreignKey.DeclaringEntityType.ClrType.Name)
                ? DeleteBehavior.Cascade
                : DeleteBehavior.Restrict, foreignKey.DeleteBehavior));

        var imageType = designModel.FindEntityType(typeof(CatalogProductImage))!;
        var primaryIndex = Assert.Single(imageType.GetIndexes(), index =>
            index.GetDatabaseName() == "ux_product_images_one_primary");
        Assert.True(primaryIndex.IsUnique);
        Assert.Equal("is_primary", primaryIndex.GetFilter());

        Assert.Contains(
            designModel.FindEntityType(typeof(CatalogCategory))!.GetCheckConstraints(),
            constraint => constraint.Name == "ck_categories_slug_format");
        Assert.Contains(
            designModel.FindEntityType(typeof(CatalogProduct))!.GetCheckConstraints(),
            constraint => constraint.Name == "ck_products_status");
    }

    [Fact]
    public void Catalog_management_permissions_are_allowlisted()
    {
        Assert.Contains("Catalog.View", PermissionCatalog.All);
        Assert.Contains("Catalog.ManageTaxonomy", PermissionCatalog.All);
        Assert.Contains("Catalog.ManageProducts", PermissionCatalog.All);
        Assert.Contains("Catalog.ManageMedia", PermissionCatalog.All);
    }

    [Fact]
    public async Task Catalog_routes_register_public_and_permission_protected_groups()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var endpoints = factory.Services
            .GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .ToArray();

        var products = endpoints.Where(endpoint =>
            endpoint.RoutePattern.RawText == "/api/v1/catalog/products").ToArray();
        Assert.NotEmpty(products);
        Assert.All(products, endpoint =>
            Assert.NotNull(endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>()));

        var adminProducts = endpoints.Where(endpoint =>
            endpoint.RoutePattern.RawText == "/api/v1/admin/catalog/products").ToArray();
        Assert.NotEmpty(adminProducts);
        Assert.All(adminProducts, endpoint =>
            Assert.NotEmpty(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()));
    }
}

public sealed class CatalogValidationTests
{
    [Fact]
    public void Inactive_parent_and_cycles_make_descendants_non_public()
    {
        var inactiveRoot = Guid.NewGuid();
        var activeChild = Guid.NewGuid();
        var activeRoot = Guid.NewGuid();
        var activeLeaf = Guid.NewGuid();
        var cycleA = Guid.NewGuid();
        var cycleB = Guid.NewGuid();
        var effective = CatalogCategoryPolicy.GetEffectivelyActiveIds(
        [
            new(inactiveRoot, null, false),
            new(activeChild, inactiveRoot, true),
            new(activeRoot, null, true),
            new(activeLeaf, activeRoot, true),
            new(cycleA, cycleB, true),
            new(cycleB, cycleA, true)
        ]);

        Assert.DoesNotContain(inactiveRoot, effective);
        Assert.DoesNotContain(activeChild, effective);
        Assert.Contains(activeRoot, effective);
        Assert.Contains(activeLeaf, effective);
        Assert.DoesNotContain(cycleA, effective);
        Assert.DoesNotContain(cycleB, effective);
    }

    [Theory]
    [InlineData("graphics-cards", "graphics-cards")]
    [InlineData("GRAPHICS-CARDS", "graphics-cards")]
    public void Slug_is_normalized(string input, string expected)
    {
        Assert.Equal(expected, CatalogValidation.Slug(input));
    }

    [Theory]
    [InlineData("graphics_cards")]
    [InlineData("-graphics")]
    [InlineData("graphics--cards")]
    [InlineData("gpu 2026")]
    [InlineData("كرت-شاشة")]
    public void Invalid_slug_is_rejected(string input)
    {
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Slug(input));
    }

    [Fact]
    public void Money_rejects_negative_and_more_than_four_decimal_places()
    {
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.NonNegativeMoney(-0.01m, "basePrice"));
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.NonNegativeMoney(1.00001m, "basePrice"));
    }
}

public sealed class CatalogMediaStorageTests
{
    [Fact]
    public async Task Webp_files_are_stored_under_generated_keys_and_path_traversal_is_rejected()
    {
        var root = Path.Combine(Path.GetTempPath(), "hardware-store-catalog-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new CatalogMediaOptions
            {
                RootPath = root,
                MaxUploadBytes = 5 * 1024 * 1024
            };
            var storage = new LocalCatalogMediaStorage(
                options, new SkiaCatalogImageProcessor(options));
            var content = CreatePng();
            var productId = Guid.Parse("11111111-1111-4111-8111-111111111111");
            var result = await storage.SaveImageAsync(
                productId,
                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                new CatalogImageUpload(new MemoryStream(content), content.Length, "image.png", "image/png"));

            Assert.StartsWith($"products/{productId:N}/", result.StorageKey);
            Assert.EndsWith("-thumb.webp", result.ThumbnailStorageKey);
            Assert.EndsWith(".original", result.OriginalStorageKey);
            Assert.Equal(32, result.Width);
            Assert.Equal(24, result.Height);
            Assert.Equal(32, result.ThumbnailWidth);
            Assert.Equal(24, result.ThumbnailHeight);
            await using (var stream = storage.OpenRead(result.OriginalStorageKey))
            {
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                Assert.Equal(content, copy.ToArray());
            }
            await using (var stream = storage.OpenRead(result.StorageKey))
            {
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                Assert.NotEmpty(copy.ToArray());
            }
            Assert.Throws<CatalogValidationException>(() => storage.OpenRead("../outside.webp"));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Excessive_source_dimensions_are_rejected_before_storage()
    {
        var root = Path.Combine(Path.GetTempPath(), "hardware-store-catalog-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new CatalogMediaOptions
            {
                RootPath = root,
                MaxSourceDimension = 1024,
                MaxUploadBytes = 5 * 1024 * 1024
            };
            var processor = new SkiaCatalogImageProcessor(options);
            using var bitmap = new SKBitmap(1100, 10);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100)!;
            var bytes = data.ToArray();

            await Assert.ThrowsAsync<CatalogValidationException>(() => processor.ProcessAsync(
                new CatalogImageUpload(new MemoryStream(bytes), bytes.Length, "wide.png", "image/png")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Media_audit_preserves_referenced_files_and_ignores_recent_orphans()
    {
        var now = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        var audit = CatalogMediaMaintenanceService.Evaluate(
            ["products/p/used.webp", "products/p/missing.webp"],
            [
                new StoredMediaFile("products/p/used.webp", now.AddDays(-1), 100),
                new StoredMediaFile("products/p/orphan-old.webp", now.AddHours(-2), 100),
                new StoredMediaFile("products/p/orphan-new.webp", now.AddMinutes(-5), 100)
            ],
            now,
            TimeSpan.FromMinutes(30));

        Assert.Equal(["products/p/missing.webp"], audit.MissingStorageKeys);
        Assert.Equal(["products/p/orphan-old.webp"], audit.OrphanStorageKeys);
    }

    [Fact]
    public async Task Renamed_non_webp_content_is_rejected()
    {
        var root = Path.Combine(Path.GetTempPath(), "hardware-store-catalog-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new CatalogMediaOptions
            {
                RootPath = root,
                MaxUploadBytes = 1024
            };
            var storage = new LocalCatalogMediaStorage(
                options, new SkiaCatalogImageProcessor(options));
            await Assert.ThrowsAsync<CatalogValidationException>(() => storage.SaveImageAsync(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                new CatalogImageUpload(new MemoryStream("%PDF-1.7"u8.ToArray()), 8, "fake.png", "image/png")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task A_transparent_png_keeps_its_transparency_and_gets_the_watermark()
    {
        var root = Path.Combine(Path.GetTempPath(), "hardware-store-catalog-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new CatalogMediaOptions { RootPath = root };
            var storage = new LocalCatalogMediaStorage(options, new SkiaCatalogImageProcessor(options));
            var photo = Png(400, 300, SKColors.Transparent, canvas => canvas.DrawRect(150, 100, 100, 100, new SKPaint { Color = SKColors.Black }));
            var logo = Png(40, 40, SKColors.Red);
            var stored = await storage.SaveImageAsync(Guid.CreateVersion7(), Guid.CreateVersion7(),
                new CatalogImageUpload(new MemoryStream(photo), photo.Length, "gpu.png", "image/png"),
                new ImageWatermark(logo, "BottomRight", 100, 20));

            await using var stream = storage.OpenRead(stored.StorageKey);
            using var image = SKBitmap.Decode(stream);
            Assert.Equal(0, image.GetPixel(5, 5).Alpha);
            Assert.Equal(255, image.GetPixel(200, 150).Alpha);
            Assert.Contains(Enumerable.Range(300, 100).SelectMany(x => Enumerable.Range(200, 100).Select(y => image.GetPixel(x, y))),
                pixel => pixel.Alpha > 200 && pixel.Red > 200 && pixel.Green < 60);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] Png(int width, int height, SKColor background, Action<SKCanvas>? draw = null)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(background);
        draw?.Invoke(canvas);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100) ?? throw new InvalidOperationException("Could not create image fixture.");
        return data.ToArray();
    }

    private static byte[] CreatePng()
    {
        using var bitmap = new SKBitmap(32, 24);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Could not create image fixture.");
        return data.ToArray();
    }
}

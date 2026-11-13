using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Purchasing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class SupplierProductCatalogPostgreSqlTests(SupplierDirectoryPostgreSqlFixture fixture)
    : IClassFixture<SupplierDirectoryPostgreSqlFixture>
{
    [Fact]
    public async Task Links_variant_with_supplier_reference_and_audit()
    {
        var database = await fixture.CreateDatabaseAsync();
        var actorId = await CreateActorAsync(database);
        var supplier = await database.CreateService().CreateAsync(
            new SupplierCreateRequest("Catalog Supplier", null, null, null, null, null, null, 0, null, true),
            actorId);
        var variant = await CreateVariantAsync(database, "GeForce RTX 5080", "RTX-5080-OC", "RTX-5080-BASE");
        var service = database.CreateProductService();

        var options = await service.SearchVariantsAsync(supplier.Id, "RTX", 10);
        Assert.Contains(options, option => option.Id == variant.VariantId);

        var created = await service.CreateAsync(
            supplier.Id,
            new SupplierProductCreateRequest(variant.VariantId, "SUP-RTX-5080", "Preferred GPU", true),
            actorId);

        Assert.Equal("SUP-RTX-5080", created.SupplierSku);
        Assert.Equal("GeForce RTX 5080", created.ProductName);
        Assert.Equal("OC Edition", created.VariantName);
        Assert.Equal("RTX-5080-OC", created.VariantSku);

        var page = await service.GetAsync(supplier.Id, "SUP-RTX", true, 1, 10);
        Assert.Single(page.Items);
        var optionsAfterLink = await service.SearchVariantsAsync(supplier.Id, "RTX", 10);
        Assert.DoesNotContain(optionsAfterLink, option => option.Id == variant.VariantId);

        await using var audit = database.CreateAuditContext();
        Assert.Contains(
            "Purchasing.SupplierProductLinked",
            await audit.SecurityAuditEvents.Select(entry => entry.EventType).ToListAsync());

        var updated = await service.UpdateAsync(
            supplier.Id,
            created.Id,
            new SupplierProductUpdateRequest("SUP-RTX-5080-B", "Updated", false, created.RowVersion),
            actorId);
        Assert.Equal("SUP-RTX-5080-B", updated.SupplierSku);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Rejects_duplicate_relation_supplier_sku_and_inactive_variant()
    {
        var database = await fixture.CreateDatabaseAsync();
        var actorId = await CreateActorAsync(database);
        var supplier = await database.CreateService().CreateAsync(
            new SupplierCreateRequest("Catalog Supplier", null, null, null, null, null, null, 0, null, true),
            actorId);
        var variant = await CreateVariantAsync(database, "GeForce RTX 5080", "RTX-5080-OC", "RTX-5080-BASE");
        var otherVariant = await CreateVariantAsync(database, "GeForce RTX 5070", "RTX-5070-OC", "RTX-5070-BASE");
        var inactiveVariant = await CreateVariantAsync(database, "Disabled GPU", "RTX-OFF", "RTX-OFF-BASE", isActive: false);
        var service = database.CreateProductService();
        await service.CreateAsync(
            supplier.Id,
            new SupplierProductCreateRequest(variant.VariantId, "SUP-1", null, true),
            actorId);

        var duplicateRelation = await Assert.ThrowsAsync<PurchasingValidationException>(() =>
            service.CreateAsync(
                supplier.Id,
                new SupplierProductCreateRequest(variant.VariantId, "SUP-2", null, true),
                actorId));
        Assert.Equal(409, duplicateRelation.StatusCode);

        var duplicateSku = await Assert.ThrowsAsync<PurchasingValidationException>(() =>
            service.CreateAsync(
                supplier.Id,
                new SupplierProductCreateRequest(otherVariant.VariantId, "SUP-1", null, true),
                actorId));
        Assert.Equal(409, duplicateSku.StatusCode);

        var inactive = await Assert.ThrowsAsync<PurchasingValidationException>(() =>
            service.CreateAsync(
                supplier.Id,
                new SupplierProductCreateRequest(inactiveVariant.VariantId, "SUP-OFF", null, true),
                actorId));
        Assert.Equal(409, inactive.StatusCode);
    }

    [Fact]
    public async Task Concurrent_link_updates_use_row_version()
    {
        var database = await fixture.CreateDatabaseAsync();
        var actorId = await CreateActorAsync(database);
        var supplier = await database.CreateService().CreateAsync(
            new SupplierCreateRequest("Concurrent Supplier", null, null, null, null, null, null, 0, null, true),
            actorId);
        var variant = await CreateVariantAsync(database, "Concurrent GPU", "RTX-CONCURRENT", "RTX-CONCURRENT-BASE");
        var service = database.CreateProductService();
        var created = await service.CreateAsync(
            supplier.Id,
            new SupplierProductCreateRequest(variant.VariantId, "SUP-CONCURRENT", null, true),
            actorId);
        var request = new SupplierProductUpdateRequest("SUP-CONCURRENT-B", "one", true, created.RowVersion);

        var attempts = await Task.WhenAll(
            CaptureAsync(service, supplier.Id, created.Id, request, actorId),
            CaptureAsync(service, supplier.Id, created.Id, request, actorId));

        Assert.Equal(1, attempts.Count(attempt => attempt is null));
        Assert.Equal(1, attempts.Count(attempt => attempt?.StatusCode == 409));
    }

    private static async Task<PurchasingValidationException?> CaptureAsync(
        SupplierProductCatalogService service,
        Guid supplierId,
        Guid id,
        SupplierProductUpdateRequest request,
        Guid actorId)
    {
        try
        {
            await service.UpdateAsync(supplierId, id, request, actorId);
            return null;
        }
        catch (PurchasingValidationException exception)
        {
            return exception;
        }
    }

    private static async Task<(Guid VariantId, Guid ProductId)> CreateVariantAsync(
        SupplierDirectoryDatabase database,
        string productName,
        string variantSku,
        string productSku,
        bool isActive = true)
    {
        await using var db = database.CreateCatalogContext();
        var category = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Test Category",
            Slug = $"cat-{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var product = new CatalogProduct
        {
            Id = Guid.CreateVersion7(),
            CategoryId = category.Id,
            Name = productName,
            Slug = $"product-{Guid.NewGuid():N}",
            Sku = productSku,
            BasePrice = 1000,
            Status = CatalogProductStatus.Published,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var variant = new CatalogProductVariant
        {
            Id = Guid.CreateVersion7(),
            ProductId = product.Id,
            Name = "OC Edition",
            Sku = variantSku,
            IsActive = isActive
        };
        db.Categories.Add(category);
        db.Products.Add(product);
        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync();
        return (variant.Id, product.Id);
    }

    private static async Task<Guid> CreateActorAsync(SupplierDirectoryDatabase database)
    {
        await using var db = database.CreateIdentityContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            FullName = "Catalog Actor",
            Email = "catalog-actor@example.test",
            IsEmployee = true,
            IsActive = true
        };
        await new PersonCodeGenerator(db, TimeProvider.System, TimeZoneInfo.Utc)
            .CreatePersonInCurrentTransactionAsync(person);
        var account = new UserAccount
        {
            Id = Guid.CreateVersion7(),
            PersonId = person.Id,
            Person = person,
            Username = "catalog-actor",
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier("catalog-actor"),
            Email = "catalog-actor@example.test",
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier("catalog-actor@example.test"),
            IsEmailVerified = true,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return account.Id;
    }
}

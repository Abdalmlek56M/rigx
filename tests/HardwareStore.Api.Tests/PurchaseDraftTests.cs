using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Purchasing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class PurchaseDraftPostgreSqlTests(SupplierDirectoryPostgreSqlFixture fixture)
    : IClassFixture<SupplierDirectoryPostgreSqlFixture>
{
    [Fact]
    public async Task Marketplace_draft_is_created_without_a_registered_supplier()
    {
        var database = await fixture.CreateDatabaseAsync();
        var drafts = database.CreatePurchaseDraftService();
        var actor = await CreateActorAsync(database);
        var (productId, variantId) = await SeedProductAsync(database);

        var created = await drafts.CreateAsync(
            new PurchaseDraftCreateRequest(
                PurchaseSourceType.Marketplace,
                null,
                "Dubizzle",
                "External Seller",
                "+20 100 000 0000",
                "https://example.test/listing/1",
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Meet in person",
                [
                    new PurchaseDraftItemInput(productId, variantId, 2, 1500m)
                ]),
            actor);

        Assert.Equal(PurchaseSourceType.Marketplace, created.SourceType);
        Assert.Null(created.SupplierId);
        Assert.Equal("Dubizzle", created.MarketplaceName);
        Assert.Equal(PurchaseDraftStatus.Draft, created.Status);
        Assert.Equal(3000m, created.TotalAmount);
        Assert.Single(created.Items);
        Assert.Equal(3000m, created.Items[0].LineTotal);

        var list = await drafts.GetAsync(null, null, "Dubizzle", null, null, 1, 10);
        Assert.Single(list.Items);
        Assert.Equal(created.Id, list.Items[0].Id);

        await using var audit = database.CreateAuditContext();
        var auditEvent = await audit.SecurityAuditEvents.SingleAsync(entry =>
            entry.EventType == "Purchasing.DraftCreated");
        Assert.Contains(created.Id.ToString(), auditEvent.SafeMetadata, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Registered_source_requires_supplier_and_other_sources_reject_supplier_id()
    {
        var database = await fixture.CreateDatabaseAsync();
        var drafts = database.CreatePurchaseDraftService();
        var actor = await CreateActorAsync(database);
        var suppliers = database.CreateService();
        var (productId, variantId) = await SeedProductAsync(database);

        var missingSupplier = await Assert.ThrowsAsync<PurchaseDraftException>(() =>
            drafts.CreateAsync(
                Draft(PurchaseSourceType.RegisteredSupplier, null, null, productId, variantId),
                actor));
        Assert.Equal(400, missingSupplier.StatusCode);

        var supplier = await suppliers.CreateAsync(
            new SupplierCreateRequest("Draft Supplier", null, null, null, null, null, null, 0, null, true),
            actor);
        var registered = await drafts.CreateAsync(
            Draft(PurchaseSourceType.RegisteredSupplier, supplier.Id, null, productId, variantId),
            actor);
        Assert.Equal(supplier.Id, registered.SupplierId);
        Assert.Equal("Draft Supplier", registered.SupplierCompanyName);

        var wrongSupplier = await Assert.ThrowsAsync<PurchaseDraftException>(() =>
            drafts.CreateAsync(
                Draft(PurchaseSourceType.IndividualSeller, supplier.Id, "Seller", productId, variantId),
                actor));
        Assert.Equal(400, wrongSupplier.StatusCode);
    }

    [Fact]
    public async Task Draft_update_requires_row_version_and_cancel_is_terminal()
    {
        var database = await fixture.CreateDatabaseAsync();
        var drafts = database.CreatePurchaseDraftService();
        var actor = await CreateActorAsync(database);
        var (productId, variantId) = await SeedProductAsync(database);
        var created = await drafts.CreateAsync(
            Draft(PurchaseSourceType.IndividualSeller, null, "Individual", productId, variantId),
            actor);

        var stale = await Assert.ThrowsAsync<PurchaseDraftException>(() =>
            drafts.UpdateAsync(
                created.Id,
                new PurchaseDraftUpdateRequest(
                    PurchaseSourceType.IndividualSeller, null, null, "Individual", null, null,
                    null, null, [new PurchaseDraftItemInput(productId, variantId, 3, 100m)],
                    created.RowVersion - 1),
                actor));
        Assert.Equal(409, stale.StatusCode);

        var updated = await drafts.UpdateAsync(
            created.Id,
            new PurchaseDraftUpdateRequest(
                PurchaseSourceType.IndividualSeller, null, null, "Individual", null, null,
                null, "Updated", [new PurchaseDraftItemInput(productId, variantId, 3, 100m)],
                created.RowVersion),
            actor);
        Assert.Equal(300m, updated.TotalAmount);
        Assert.Equal(updated.RowVersion, created.RowVersion + 1);

        var cancelled = await drafts.CancelAsync(updated.Id, updated.RowVersion, actor);
        Assert.Equal(PurchaseDraftStatus.Cancelled, cancelled.Status);

        var editCancelled = await Assert.ThrowsAsync<PurchaseDraftException>(() =>
            drafts.UpdateAsync(
                cancelled.Id,
                new PurchaseDraftUpdateRequest(
                    PurchaseSourceType.IndividualSeller, null, null, "Individual", null, null,
                    null, null, [new PurchaseDraftItemInput(productId, variantId, 1, 50m)],
                    cancelled.RowVersion),
                actor));
        Assert.Equal(409, editCancelled.StatusCode);

        var list = await drafts.GetAsync(null, PurchaseDraftStatus.Draft, null, null, null, 1, 10);
        Assert.Empty(list.Items);
    }

    private static PurchaseDraftCreateRequest Draft(
        string source,
        Guid? supplierId,
        string? seller,
        Guid productId,
        Guid? variantId) =>
        new(
            source,
            supplierId,
            source == PurchaseSourceType.Marketplace ? "Facebook Marketplace" : null,
            seller,
            null,
            null,
            null,
            null,
            [new PurchaseDraftItemInput(productId, variantId, 1, 100m)]);

    private static async Task<(Guid ProductId, Guid VariantId)> SeedProductAsync(
        SupplierDirectoryDatabase database)
    {
        await using var catalog = database.CreateCatalogContext();
        var category = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Draft Category",
            Slug = $"draft-{Guid.NewGuid():N}",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var product = new CatalogProduct
        {
            Id = Guid.CreateVersion7(),
            CategoryId = category.Id,
            Name = "Draft Test Product",
            Slug = $"draft-product-{Guid.NewGuid():N}",
            Sku = $"DRAFT-{Guid.NewGuid():N}",
            BasePrice = 2000m,
            Status = CatalogProductStatus.Published,
            Condition = CatalogProductCondition.New,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var variant = new CatalogProductVariant
        {
            Id = Guid.CreateVersion7(),
            ProductId = product.Id,
            Name = "16 GB",
            Sku = $"DRAFT-VAR-{Guid.NewGuid():N}",
            Price = 2000m,
            IsActive = true
        };
        catalog.Categories.Add(category);
        catalog.Products.Add(product);
        catalog.ProductVariants.Add(variant);
        await catalog.SaveChangesAsync();
        return (product.Id, variant.Id);
    }

    private static async Task<Guid> CreateActorAsync(SupplierDirectoryDatabase database)
    {
        await using var db = database.CreateIdentityContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            FullName = "Purchase Actor",
            Email = $"purchase-actor-{Guid.NewGuid():N}@example.test",
            IsEmployee = true,
            IsActive = true
        };
        await new PersonCodeGenerator(db, TimeProvider.System, TimeZoneInfo.Utc)
            .CreatePersonInCurrentTransactionAsync(person);
        var username = $"purchase-{Guid.NewGuid():N}";
        var account = new UserAccount
        {
            Id = Guid.CreateVersion7(),
            PersonId = person.Id,
            Person = person,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = person.Email,
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier(person.Email),
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

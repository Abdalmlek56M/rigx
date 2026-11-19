using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static HardwareStore.Api.Tests.RetailReceipts;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class CategorySpecPostgreSqlTests(CatalogPostgreSqlFixture fixture) : IClassFixture<CatalogPostgreSqlFixture>
{
    private readonly TestFactory _factory = new(fixture.Options);

    private TaxonomyService Taxonomy => new(_factory, TimeProvider.System);
    private CategorySpecService Specs => new(_factory, TimeProvider.System);

    private async Task<CategoryView> NewCategoryAsync(string name, Guid? parentId = null, string? code = null)
    {
        var slug = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}"[..24];
        return await Taxonomy.SaveCategoryAsync(null, new CategoryInput(name, slug, parentId, null, null, 0, true, null, code));
    }

    [Fact]
    public async Task A_child_category_inherits_its_parents_specs()
    {
        var parent = await NewCategoryAsync("Parent");
        var child = await NewCategoryAsync("Child", parent.Id);
        await Specs.SetSpecsAsync(parent.Id, [new SpecDefinition("socket", "Socket", SpecType.Choice, ["AM4", "AM5"], IsFilter: true)], parent.RowVersion);
        await Specs.SetSpecsAsync(child.Id, [new SpecDefinition("tdp", "TDP", SpecType.Number, Unit: "W")], child.RowVersion);

        var effective = await Specs.GetEffectiveSpecsAsync(child.Id);

        Assert.Equal(["socket", "tdp"], effective.Select(definition => definition.Key));
        Assert.True(effective[0].IsFilter);
        Assert.Equal(["tdp"], (await Taxonomy.GetCategoriesAsync()).Single(item => item.Id == child.Id).Specs.Select(item => item.Key));
        await Assert.ThrowsAsync<CatalogNotFoundException>(() => Specs.GetEffectiveSpecsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Setting_specs_checks_the_row_version()
    {
        var category = await NewCategoryAsync("Versioned");
        var saved = await Specs.SetSpecsAsync(category.Id, [new SpecDefinition("tdp", "TDP", SpecType.Number)], category.RowVersion);

        Assert.Equal(category.RowVersion + 1, saved.RowVersion);
        await Assert.ThrowsAsync<CatalogConflictException>(() => Specs.SetSpecsAsync(category.Id, [], category.RowVersion));
    }

    [Fact]
    public async Task A_duplicate_category_code_is_a_conflict()
    {
        var first = await NewCategoryAsync("First", code: "cpu" + Guid.NewGuid().ToString("N")[..6]);
        Assert.Equal(first.Code, first.Code!.ToUpperInvariant());

        await Assert.ThrowsAsync<CatalogConflictException>(() => NewCategoryAsync("Second", code: first.Code!.ToLowerInvariant()));
    }

    [Fact]
    public async Task Item_values_are_checked_against_the_effective_schema()
    {
        var category = await NewCategoryAsync("Checked");
        await Specs.SetSpecsAsync(category.Id, [new SpecDefinition("socket", "Socket", SpecType.Choice, ["AM4", "AM5"])], category.RowVersion);

        var clean = await Specs.CheckValuesAsync(category.Id, new Dictionary<string, string> { ["socket"] = "am5" });
        Assert.Equal("AM5", clean["socket"]);
        var invalid = await Assert.ThrowsAsync<CatalogValidationException>(() =>
            Specs.CheckValuesAsync(category.Id, new Dictionary<string, string> { ["socket"] = "LGA1700" }));
        Assert.Equal(400, invalid.StatusCode);
        var free = new Dictionary<string, string> { ["anything"] = "goes" };
        Assert.Same(free, await Specs.CheckValuesAsync(null, free));
    }

    private sealed class TestFactory(DbContextOptions<CatalogDbContext> options) : IDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext() => new(options);
        public Task<CatalogDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}

[Trait("Category", "RequiresDocker")]
public sealed class StoreSpecFilterPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();

    [Fact]
    public async Task Store_list_filters_by_exact_spec_values()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var lines = new[] { "CPU A", "CPU B", "CPU C", "CPU D" }
            .Select(model => new ReceiptLine(model, "Used", 1, 600m, 1000m, SerialNumber: "SN-" + model)).ToArray();
        var items = (await retail.Units.ReceiveAsync(Receipt(Guid.NewGuid(), null, lines), Actor)).Items;
        var socketsAndCores = new (string Socket, string Cores)[] { ("AM4", "6"), ("AM5", "6"), ("AM4", "8"), ("AM4", "6") };
        for (var index = 0; index < items.Count; index++)
        {
            for (var photo = 0; photo < 3; photo++)
                await retail.StockItems.AddPhotoAsync(items[index].Id, new StockItemPhoto($"i/{index}/{photo}.webp", $"i/{index}/{photo}-t.webp"));
            var current = await retail.StockItems.GetAsync(items[index].Id, false, false);
            var (socket, cores) = socketsAndCores[index];
            await retail.StockItems.UpdateAsync(items[index].Id, new StockItemUpdate(current.ModelName, null, null, null, current.Price, null, null,
                null, null, false, false, null, new Dictionary<string, string> { ["socket"] = socket, ["cores"] = cores }, current.RowVersion));
        }
        await retail.StockItems.SetVisibilityAsync(new StockVisibilityRequest([items[3].Id], true));

        var one = await retail.Store.ListAsync(new StoreItemQuery(Specs: new Dictionary<string, string> { ["socket"] = "AM4" }));
        var both = await retail.Store.ListAsync(new StoreItemQuery(Specs: new Dictionary<string, string> { ["socket"] = "AM4", ["cores"] = "6" }));
        var none = await retail.Store.ListAsync(new StoreItemQuery(Specs: new Dictionary<string, string> { ["socket"] = "AM9" }));

        Assert.Equal(["CPU A", "CPU C"], one.Items.Select(card => card.ModelName).Order());
        Assert.Equal(["CPU A"], both.Items.Select(card => card.ModelName));
        Assert.Empty(none.Items);
    }
}

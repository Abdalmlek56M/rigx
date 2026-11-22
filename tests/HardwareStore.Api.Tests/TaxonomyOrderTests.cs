using HardwareStore.Modules.Catalog;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Hand-chosen order of brands and categories, and the categories a brand is suggested for (roadmap 4.11 F2).</summary>
[Trait("Category", "RequiresDocker")]
public sealed class TaxonomyOrderTests(CatalogPostgreSqlFixture fixture) : IClassFixture<CatalogPostgreSqlFixture>
{
    private sealed class Factory(DbContextOptions<CatalogDbContext> options) : IDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext() => new(options);
        public Task<CatalogDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }

    private (TaxonomyService Taxonomy, TaxonomyOrderService Order) Services()
    {
        var factory = new Factory(fixture.Options);
        return (new TaxonomyService(factory, TimeProvider.System), new TaxonomyOrderService(factory, TimeProvider.System));
    }

    private static string Suffix() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<CategoryView> CategoryAsync(TaxonomyService taxonomy, string name, Guid? parent = null, int sort = 0)
    {
        var slug = $"{name.ToLowerInvariant()}-{Suffix()}";
        return await taxonomy.SaveCategoryAsync(null, new CategoryInput(name, slug, parent, null, null, sort, true, null));
    }

    private static Task<BrandView> BrandAsync(TaxonomyService taxonomy, string name, IReadOnlyList<Guid>? categories = null) =>
        taxonomy.SaveBrandAsync(null, new BrandInput(name, $"{name.ToLowerInvariant()}-{Suffix()}", null, null, null, true, null, categories));

    [Fact]
    public async Task Brands_are_listed_in_the_order_they_were_put_in_and_a_new_brand_goes_last()
    {
        var (taxonomy, order) = Services();
        var existing = (await taxonomy.GetBrandsAsync()).Select(brand => brand.Id).ToList();
        var a = await BrandAsync(taxonomy, "Aaa");
        var b = await BrandAsync(taxonomy, "Bbb");
        var c = await BrandAsync(taxonomy, "Ccc");
        var all = existing.Concat([c.Id, a.Id, b.Id]).ToList();

        await order.ReorderBrandsAsync(all);

        Assert.Equal(all, (await taxonomy.GetBrandsAsync()).Select(brand => brand.Id));
        Assert.Equal(all, (await taxonomy.GetActiveBrandsAsync()).Select(brand => brand.Id));
        var last = await BrandAsync(taxonomy, "Ddd");
        Assert.Equal(last.Id, (await taxonomy.GetBrandsAsync()).Last().Id);
        Assert.Equal(all.Count, last.SortOrder);
    }

    [Fact]
    public async Task Moving_a_brand_bumps_the_version_of_the_brands_that_moved_so_an_open_form_is_refused()
    {
        var (taxonomy, order) = Services();
        var existing = (await taxonomy.GetBrandsAsync()).Select(brand => brand.Id).ToList();
        var a = await BrandAsync(taxonomy, "Eee");
        var b = await BrandAsync(taxonomy, "Fff");

        await order.ReorderBrandsAsync([.. existing, b.Id, a.Id]);

        var now = (await taxonomy.GetBrandsAsync()).ToDictionary(brand => brand.Id);
        Assert.Equal(a.RowVersion + 1, now[a.Id].RowVersion);
        Assert.Equal(b.RowVersion + 1, now[b.Id].RowVersion);
        var stale = await Assert.ThrowsAsync<CatalogConflictException>(() =>
            taxonomy.SaveBrandAsync(a.Id, new BrandInput("Eee", a.Slug, null, null, null, true, a.RowVersion)));
        Assert.Equal(409, stale.StatusCode);
    }

    [Fact]
    public async Task A_list_that_is_not_every_brand_once_is_refused_and_changes_nothing()
    {
        var (taxonomy, order) = Services();
        var brand = await BrandAsync(taxonomy, "Ggg");
        await BrandAsync(taxonomy, "Ggh");
        var all = (await taxonomy.GetBrandsAsync()).Select(item => item.Id).ToList();

        Assert.Equal(400, (await Assert.ThrowsAsync<CatalogValidationException>(() => order.ReorderBrandsAsync(null))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<CatalogValidationException>(() => order.ReorderBrandsAsync([brand.Id, brand.Id]))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<CatalogConflictException>(() => order.ReorderBrandsAsync([brand.Id]))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<CatalogConflictException>(() => order.ReorderBrandsAsync([.. all.Skip(1), Guid.NewGuid()]))).StatusCode);
        Assert.Equal(all, (await taxonomy.GetBrandsAsync()).Select(item => item.Id));
    }

    [Fact]
    public async Task Categories_are_ordered_among_their_siblings_only()
    {
        var (taxonomy, order) = Services();
        var parent = await CategoryAsync(taxonomy, "Parent");
        var x = await CategoryAsync(taxonomy, "Xcat", parent.Id, 0);
        var y = await CategoryAsync(taxonomy, "Ycat", parent.Id, 1);
        var z = await CategoryAsync(taxonomy, "Zcat", parent.Id, 2);
        var other = await CategoryAsync(taxonomy, "Other", parent.Id, 3);
        var topBefore = (await taxonomy.GetCategoriesAsync()).Where(category => category.ParentId is null).ToDictionary(category => category.Id, category => category.SortOrder);

        await order.ReorderCategoriesAsync(parent.Id, [other.Id, z.Id, y.Id, x.Id]);

        var children = (await taxonomy.GetCategoriesAsync()).Where(category => category.ParentId == parent.Id).Select(category => category.Id);
        Assert.Equal([other.Id, z.Id, y.Id, x.Id], children);
        var topAfter = (await taxonomy.GetCategoriesAsync()).Where(category => category.ParentId is null).ToDictionary(category => category.Id, category => category.SortOrder);
        Assert.Equal(topBefore, topAfter);
        Assert.Equal(409, (await Assert.ThrowsAsync<CatalogConflictException>(() => order.ReorderCategoriesAsync(parent.Id, [x.Id, y.Id]))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<CatalogConflictException>(() => order.ReorderCategoriesAsync(null, [x.Id, y.Id, z.Id, other.Id]))).StatusCode);
    }

    [Fact]
    public async Task A_brand_keeps_the_categories_it_is_suggested_for_and_they_can_be_changed_or_cleared()
    {
        var (taxonomy, _) = Services();
        var gpu = await CategoryAsync(taxonomy, "Gpu");
        var ram = await CategoryAsync(taxonomy, "Ram");
        var brand = await BrandAsync(taxonomy, "Hhh", [ram.Id, gpu.Id, ram.Id]);

        Assert.Equal(new[] { gpu.Id, ram.Id }.Order(), brand.CategoryIds);

        var kept = await taxonomy.SaveBrandAsync(brand.Id, new BrandInput("Hhh", brand.Slug, null, null, null, true, brand.RowVersion));
        Assert.Equal(brand.CategoryIds, kept.CategoryIds);

        var moved = await taxonomy.SaveBrandAsync(brand.Id, new BrandInput("Hhh", brand.Slug, null, null, null, true, kept.RowVersion, [ram.Id]));
        Assert.Equal([ram.Id], moved.CategoryIds);
        Assert.Equal([ram.Id], (await taxonomy.GetBrandsAsync()).Single(item => item.Id == brand.Id).CategoryIds);

        var cleared = await taxonomy.SaveBrandAsync(brand.Id, new BrandInput("Hhh", brand.Slug, null, null, null, true, moved.RowVersion, []));
        Assert.Empty(cleared.CategoryIds);
    }

    [Fact]
    public async Task A_suggestion_for_a_category_that_does_not_exist_is_a_400()
    {
        var (taxonomy, _) = Services();

        var error = await Assert.ThrowsAsync<CatalogValidationException>(() => BrandAsync(taxonomy, "Iii", [Guid.NewGuid()]));

        Assert.Contains("categoryIds", error.Errors.Keys);
    }
}

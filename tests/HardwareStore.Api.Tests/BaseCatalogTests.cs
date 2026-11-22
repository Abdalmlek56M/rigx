using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The store's starting data: base categories and brands (added on every migrate run) and the default social links.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class BaseCatalogPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string BeforeSocialLinks = "20261114114658_M46_BackupSettings";
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task The_seed_adds_the_whole_tree_and_brand_list_once()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateCatalogContext();

        var first = await BaseCatalogSeeder.SeedAsync(db, Now);
        var second = await BaseCatalogSeeder.SeedAsync(db, Now);

        Assert.Equal((BaseCatalog.Categories.Count, BaseCatalog.Brands.Count), (first.CategoriesAdded, first.BrandsAdded));
        Assert.Equal((0, 0), (second.CategoriesAdded, second.BrandsAdded));
        var categories = await db.Categories.AsNoTracking().ToListAsync();
        var processors = categories.Single(category => category.Slug == "processors");
        Assert.Equal("معالجات", processors.Name);
        Assert.Equal(["amd", "intel"], categories.Where(category => category.ParentId == processors.Id).Select(category => category.Slug).Order());
        Assert.All(categories, category => Assert.True(category.IsActive));
        Assert.Equal("be-quiet", (await db.Brands.AsNoTracking().SingleAsync(brand => brand.Name == "be quiet!")).Slug);
    }

    [Fact]
    public async Task Base_categories_get_starter_specs_only_while_they_have_none()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownSpec = new SpecDefinition("chip", "الشريحة", SpecType.Text);
        await using (var owner = database.CreateCatalogContext())
        {
            var monitors = Category("monitors", "شاشات");
            monitors.SpecDefinitions = [ownSpec];
            owner.Categories.Add(monitors);
            await owner.SaveChangesAsync();
        }
        await using var db = database.CreateCatalogContext();

        await BaseCatalogSeeder.SeedAsync(db, Now);
        await BaseCatalogSeeder.SeedAsync(db, Now);

        var categories = await db.Categories.AsNoTracking().ToListAsync();
        var gpu = categories.Single(category => category.Slug == "graphics-cards").SpecDefinitions;
        var vram = Assert.Single(gpu, spec => spec.Key == "vram");
        Assert.Equal((SpecType.Number, "GB"), (vram.Type, vram.Unit));
        Assert.Equal([ownSpec], categories.Single(category => category.Slug == "monitors").SpecDefinitions);
        Assert.Empty(categories.Single(category => category.Slug == "accessories").SpecDefinitions);
        Assert.Equal(1, categories.Single(category => category.Slug == "graphics-cards").RowVersion);
    }

    [Fact]
    public async Task A_store_holding_the_first_starter_specs_untouched_gets_the_richer_list_once()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using (var owner = database.CreateCatalogContext())
        {
            var power = Category("power-supplies", "باورات");
            power.SpecDefinitions =
            [
                new SpecDefinition("power_w", "القدرة", SpecType.Number, Unit: "W"),
                new SpecDefinition("efficiency", "الشهادة", SpecType.Choice, ["80+", "80+ Gold"], IsFilter: true)
            ];
            var cases = Category("cases", "كيس");
            cases.SpecDefinitions = [new SpecDefinition("form_factor", "مقاس البوردة", SpecType.Choice, ["ATX"], IsFilter: true), new SpecDefinition("color", "اللون", SpecType.Text)];
            var processors = Category("processors", "معالجات");
            processors.Code = "CPU"; // the owner has set the builder up, so the builder slots are left alone and row versions show only the specs change
            owner.Categories.AddRange(power, cases, processors);
            await owner.SaveChangesAsync();
        }
        await using var db = database.CreateCatalogContext();

        await BaseCatalogSeeder.SeedAsync(db, Now);
        await BaseCatalogSeeder.SeedAsync(db, Now);

        var categories = await db.Categories.AsNoTracking().ToListAsync();
        var supplies = categories.Single(category => category.Slug == "power-supplies");
        Assert.Equal(["power_w", "efficiency", "form_factor", "modular", "fan_size", "pcie_connectors"], supplies.SpecDefinitions.Select(spec => spec.Key));
        Assert.Equal(["80+", "80+ Gold"], supplies.SpecDefinitions.Single(spec => spec.Key == "efficiency").Options);
        Assert.Equal(2, supplies.RowVersion);
        var ownCase = categories.Single(category => category.Slug == "cases");
        Assert.Equal(["form_factor", "color"], ownCase.SpecDefinitions.Select(spec => spec.Key));
        Assert.Equal(1, ownCase.RowVersion);
    }

    [Fact]
    public async Task The_seed_keeps_the_owner_categories_and_brands_as_they_are()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using (var owner = database.CreateCatalogContext())
        {
            owner.Categories.AddRange(
                Category("processors", "بروسيسورات", isActive: false),
                Category("ram", "رامات"));
            owner.Brands.Add(new CatalogBrand { Id = Guid.CreateVersion7(), Name = "Asus", Slug = "asus-eg", CreatedAtUtc = Now, UpdatedAtUtc = Now });
            await owner.SaveChangesAsync();
        }
        await using var db = database.CreateCatalogContext();

        var result = await BaseCatalogSeeder.SeedAsync(db, Now);

        Assert.Equal(BaseCatalog.Categories.Count - 2, result.CategoriesAdded);
        Assert.Equal(BaseCatalog.Brands.Count - 1, result.BrandsAdded);
        var categories = await db.Categories.AsNoTracking().ToListAsync();
        var processors = categories.Single(category => category.Slug == "processors");
        Assert.Equal(("بروسيسورات", false), (processors.Name, processors.IsActive));
        Assert.Equal(processors.Id, categories.Single(category => category.Slug == "intel").ParentId);
        Assert.Single(categories, category => category.Name == "رامات");
        Assert.DoesNotContain(categories, category => category.Slug == "memory");
        Assert.Single(await db.Brands.AsNoTracking().Where(brand => brand.Name.ToLower() == "asus").ToListAsync());
    }

    [Fact]
    public async Task A_new_store_gets_the_builder_slots_and_a_store_the_owner_set_up_is_left_alone()
    {
        var fresh = await fixture.CreateDatabaseAsync();
        await using (var db = fresh.CreateCatalogContext())
        {
            await BaseCatalogSeeder.SeedAsync(db, Now);
            await BaseCatalogSeeder.SeedAsync(db, Now);
        }
        var configured = await fixture.CreateDatabaseAsync();
        await using (var owner = configured.CreateCatalogContext())
        {
            var processors = Category("processors", "معالجات");
            processors.Code = "CPU";
            processors.ShowInBuilder = true;
            owner.Categories.Add(processors);
            await owner.SaveChangesAsync();
        }
        await using (var db = configured.CreateCatalogContext()) await BaseCatalogSeeder.SeedAsync(db, Now);

        await using var freshRead = fresh.CreateCatalogContext();
        var slots = (await freshRead.Categories.AsNoTracking().Where(category => category.ShowInBuilder).ToListAsync())
            .ToDictionary(category => category.Slug, category => category.Code!);
        Assert.Equal(BaseCatalog.BuilderSlots.ToDictionary(), slots);
        Assert.Equal(1, (await freshRead.Categories.AsNoTracking().SingleAsync(category => category.Slug == "storage")).RowVersion);
        await using var configuredRead = configured.CreateCatalogContext();
        var onlyOwnerSlot = await configuredRead.Categories.AsNoTracking().Where(category => category.ShowInBuilder).ToListAsync();
        Assert.Equal(["processors"], onlyOwnerSlot.Select(category => category.Slug));
    }

    [Fact]
    public async Task A_renamed_starting_category_is_renamed_only_where_the_owner_kept_the_old_name()
    {
        var kept = await fixture.CreateDatabaseAsync();
        await using (var owner = kept.CreateCatalogContext())
        {
            owner.Categories.Add(Category("cases", "كيسات"));
            await owner.SaveChangesAsync();
        }
        var changed = await fixture.CreateDatabaseAsync();
        await using (var owner = changed.CreateCatalogContext())
        {
            owner.Categories.Add(Category("cases", "كيسات جيمنج"));
            await owner.SaveChangesAsync();
        }
        var otherSlug = await fixture.CreateDatabaseAsync();
        await using (var owner = otherSlug.CreateCatalogContext())
        {
            owner.Categories.Add(Category("pc-cases", "كيسات"));
            await owner.SaveChangesAsync();
        }

        await using var keptDb = kept.CreateCatalogContext();
        var first = await BaseCatalogSeeder.SeedAsync(keptDb, Now);
        var second = await BaseCatalogSeeder.SeedAsync(keptDb, Now);
        await using var changedDb = changed.CreateCatalogContext();
        var changedResult = await BaseCatalogSeeder.SeedAsync(changedDb, Now);
        await using var otherDb = otherSlug.CreateCatalogContext();
        await BaseCatalogSeeder.SeedAsync(otherDb, Now);

        Assert.Equal((1, 0), (first.CategoriesRenamed, second.CategoriesRenamed));
        Assert.Equal("كيس", (await keptDb.Categories.AsNoTracking().SingleAsync(category => category.Slug == "cases")).Name);
        Assert.Equal(0, changedResult.CategoriesRenamed);
        Assert.Equal("كيسات جيمنج", (await changedDb.Categories.AsNoTracking().SingleAsync(category => category.Slug == "cases")).Name);
        var others = await otherDb.Categories.AsNoTracking().Where(category => category.ParentId == null).ToListAsync();
        Assert.Single(others, category => category.Name is "كيس" or "كيسات");
    }

    [Fact]
    public async Task A_saved_contact_list_gets_the_missing_social_links_once_and_keeps_the_owner_links()
    {
        var name = $"social_links_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{name}\"";
            await create.ExecuteNonQueryAsync();
        }
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = name }.ConnectionString;
        await using var storefront = new StorefrontDbContext(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
        await storefront.GetService<IMigrator>().MigrateAsync(BeforeSocialLinks);
        await storefront.Database.ExecuteSqlRawAsync("""
            UPDATE store_settings SET row_version = 3, contact_links = '{{"links":[{{"platform":"WhatsApp","value":"201000000000","showOnSite":true,"showOnInvoice":true,"showInFloatingButton":true}},
                                  {{"platform":"Facebook","value":"https://www.facebook.com/owner.page","showOnSite":true,"showOnInvoice":false,"showInFloatingButton":false}}]}}'::jsonb
            """);

        await storefront.Database.MigrateAsync();

        var settings = await storefront.StoreSettings.AsNoTracking().SingleAsync();
        Assert.Equal([ContactPlatform.WhatsApp, ContactPlatform.Facebook, ContactPlatform.Instagram, ContactPlatform.TikTok, ContactPlatform.YouTube,
            ContactPlatform.Telegram], settings.ContactLinks.Links.Select(link => link.Platform));
        Assert.Equal("https://www.facebook.com/owner.page", settings.ContactLinks.Links[1].Value);
        Assert.Equal("https://t.me/rigxhardware", settings.ContactLinks.Links[5].Value);
        Assert.All(settings.ContactLinks.Links.Skip(2), link => Assert.True(link.ShowOnSite && !link.ShowOnInvoice && !link.ShowInFloatingButton));
        Assert.True(settings.RowVersion > 3);
        Assert.NotNull(settings.ContactLinks.Normalize());
    }

    [Fact]
    public void A_store_that_never_saved_its_links_shows_the_number_and_every_social_account()
    {
        var links = new ContactLinkSettings().Normalize().Links;

        Assert.Equal(ContactPlatform.All.Order(), links.Select(link => link.Platform).Order());
        Assert.All(links.Where(link => !ContactPlatform.IsNumber(link.Platform)), link => Assert.Contains("rigxhardware", link.Value));
    }

    private static CatalogCategory Category(string slug, string name, bool isActive = true) => new()
    {
        Id = Guid.CreateVersion7(), Name = name, Slug = slug, IsActive = isActive, CreatedAtUtc = Now, UpdatedAtUtc = Now
    };
}

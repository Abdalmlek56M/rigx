using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>Per-category sale price switches (alone / with a board / inside a build) on the storefront cards and the online checkout.</summary>
public sealed class PriceOptionRuleTests
{
    [Fact]
    public void A_set_lists_what_it_offers_in_order_and_defaults_to_the_first()
    {
        Assert.Equal(["Alone", "WithBoard", "InBuild"], PriceOptionSet.All.Names);
        Assert.Equal("Alone", PriceOptionSet.All.Default);

        var noAlone = new PriceOptionSet(Alone: false);
        Assert.Equal(["WithBoard", "InBuild"], noAlone.Names);
        Assert.Equal("WithBoard", noAlone.Default);
        Assert.False(noAlone.Allows("Alone"));

        var onlyInBuild = new PriceOptionSet(false, false, true);
        Assert.Equal("InBuild", onlyInBuild.Default);
    }

    [Fact]
    public void A_category_that_is_not_listed_offers_every_price()
    {
        var restricted = Guid.NewGuid();
        var rule = new PriceOptionRule(new Dictionary<Guid, PriceOptionSet> { [restricted] = new(Alone: false) });

        Assert.Equal(["WithBoard", "InBuild"], rule.ForCategory(restricted).Names);
        Assert.Equal(PriceOptionSet.All, rule.ForCategory(Guid.NewGuid()));
        Assert.Equal(PriceOptionSet.All, rule.ForCategory(null));
        Assert.Equal(PriceOptionSet.All, PriceOptionRule.Open.ForCategory(restricted));
    }

    [Fact]
    public void An_item_with_its_own_choice_replaces_its_category_and_one_without_follows_it()
    {
        var category = Guid.NewGuid();
        var rule = new PriceOptionRule(new Dictionary<Guid, PriceOptionSet> { [category] = new(Alone: false) });

        Assert.Null(StockItemRules.OwnPriceOptions(null, null, null));
        Assert.Equal(["WithBoard", "InBuild"], rule.ForItem(category, null).Names);

        var aloneOrInBuild = StockItemRules.OwnPriceOptions(true, false, true);
        Assert.Equal(["Alone", "InBuild"], rule.ForItem(category, aloneOrInBuild).Names);
        Assert.Equal(["Alone"], rule.ForItem(null, StockItemRules.OwnPriceOptions(true, false, false)).Names);
    }

    [Fact]
    public void An_item_choice_must_keep_one_way_to_sell_it()
    {
        StockItemRules.ValidatePriceOptions(null);
        StockItemRules.ValidatePriceOptions(new ItemPriceOptions(false, false, true));

        var error = Assert.Throws<RetailException>(() => StockItemRules.ValidatePriceOptions(new ItemPriceOptions(false, false, false)));
        Assert.Equal(400, error.StatusCode);
        Assert.True(error.Errors?.ContainsKey("priceOptions"));
    }
}

[Trait("Category", "RequiresDocker")]
public sealed class PriceOptionPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private sealed class RetailFactory(string connectionString) : IDbContextFactory<RetailDbContext>
    {
        public RetailDbContext CreateDbContext() => new(new DbContextOptionsBuilder<RetailDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class FixedSource(PriceOptionRule rule) : IPriceOptionSource
    {
        public Task<PriceOptionRule> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(rule);
    }

    [Fact]
    public async Task The_store_cards_list_the_offered_prices_and_online_checkout_refuses_a_price_that_is_off_while_the_pos_stays_free()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var category = Guid.NewGuid();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("RX 6600", "New", 3, 500m, 1000m, category, WithBoardExtra: 100m, AloneExtra: 200m));
        await retail.PublishAsync(item);
        var rule = new PriceOptionRule(new Dictionary<Guid, PriceOptionSet> { [category] = new(Alone: false) });
        var store = new StoreItemService(new RetailFactory(database.ConnectionString), new FixedSource(rule));

        var card = Assert.Single((await store.ListAsync(new StoreItemQuery())).Items);
        Assert.Equal(["WithBoard", "InBuild"], card.PriceOptions);
        Assert.Equal(["WithBoard", "InBuild"], (await store.GetAsync(card.Slug)).PriceOptions);
        Assert.Equal(["Alone", "WithBoard", "InBuild"], Assert.Single((await retail.Store.ListAsync(new StoreItemQuery())).Items).PriceOptions);

        var policy = Policy() with { PriceOptions = rule };
        var defaulted = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]), policy);
        Assert.Equal(1100m, Assert.Single(defaulted.Items).UnitPrice);

        var chosen = await retail.CheckoutAsync(Checkout([StockLine(item.Id, priceOption: "WithBoard")]), policy);
        Assert.Equal(1100m, Assert.Single(chosen.Items).UnitPrice);
        Assert.Equal(400, await StatusCodeAsync(() => retail.CheckoutAsync(Checkout([StockLine(item.Id, priceOption: "Alone")]), policy)));

        var pos = await retail.PosAsync(Sale([StockLine(item.Id, priceOption: "Alone")]));
        Assert.Equal(1200m, Assert.Single(pos.Items).UnitPrice);
    }

    [Fact]
    public async Task An_item_set_to_sell_alone_only_shows_and_sells_only_alone_online_and_can_go_back_to_its_category()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var category = Guid.NewGuid();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("RTX 4060", "New", 3, 500m, 1000m, category, WithBoardExtra: 100m, AloneExtra: 200m));
        await retail.PublishAsync(item);
        var store = new StoreItemService(new RetailFactory(database.ConnectionString), new FixedSource(PriceOptionRule.Open));

        async Task<StockItemAdminView> SetAsync(ItemPriceOptions? options)
        {
            var current = await retail.StockItems.GetAsync(item.Id, includeCosts: false, includeSerials: false);
            return await retail.StockItems.UpdateAsync(item.Id, new StockItemUpdate(current.ModelName, current.CategoryId, current.BrandId, current.ProductId,
                current.Price, current.WithBoardExtra, current.AloneExtra, current.WarrantyLabel, current.WarrantyMonths, current.HasBox, current.IsHidden,
                current.InternalNote, current.Specs, current.RowVersion, options));
        }

        Assert.Null((await retail.StockItems.GetAsync(item.Id, false, false)).PriceOptions);
        var saved = await SetAsync(new ItemPriceOptions(Alone: true, WithBoard: false, InBuild: false));
        Assert.Equal(new ItemPriceOptions(true, false, false), saved.PriceOptions);

        var card = Assert.Single((await store.ListAsync(new StoreItemQuery())).Items);
        Assert.Equal(["Alone"], card.PriceOptions);
        Assert.Equal(["Alone"], (await store.GetAsync(card.Slug)).PriceOptions);

        var policy = Policy() with { PriceOptions = PriceOptionRule.Open };
        Assert.Equal(1200m, Assert.Single((await retail.CheckoutAsync(Checkout([StockLine(item.Id)]), policy)).Items).UnitPrice);
        Assert.Equal(400, await StatusCodeAsync(() => retail.CheckoutAsync(Checkout([StockLine(item.Id, priceOption: "WithBoard")]), policy)));
        Assert.Equal(400, await StatusCodeAsync(() => SetAsync(new ItemPriceOptions(false, false, false))));

        Assert.Null((await SetAsync(null)).PriceOptions);
        Assert.Equal(["Alone", "WithBoard", "InBuild"], Assert.Single((await store.ListAsync(new StoreItemQuery())).Items).PriceOptions);
    }
}

using HardwareStore.Modules.Catalog;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>A child category follows its parent's switched-off sale prices; the child can switch off more but never back on.</summary>
public sealed class CategoryPriceRulesTests
{
    private static CategoryView Category(Guid id, Guid? parentId, CategoryPriceOptions? options = null) =>
        new(id, parentId, "name", $"slug-{id}", null, null, 0, true, true, 1, null, true, false, [], [], null, options);

    [Fact]
    public void A_child_offers_what_its_parent_and_it_offer()
    {
        var parent = Guid.NewGuid();
        var child = Guid.NewGuid();
        var grandchild = Guid.NewGuid();
        var effective = CategoryPriceRules.Effective([
            Category(parent, null, new CategoryPriceOptions(Alone: false)),
            Category(child, parent),
            Category(grandchild, child, new CategoryPriceOptions(WithBoard: false))
        ]);

        Assert.Equal(new CategoryPriceOptions(false, true, true), effective[parent]);
        Assert.Equal(new CategoryPriceOptions(false, true, true), effective[child]);
        Assert.Equal(new CategoryPriceOptions(false, false, true), effective[grandchild]);
    }

    [Fact]
    public void A_child_cannot_switch_back_on_a_price_its_parent_turned_off()
    {
        var parent = Guid.NewGuid();
        var child = Guid.NewGuid();
        var effective = CategoryPriceRules.Effective([
            Category(parent, null, new CategoryPriceOptions(Alone: false)),
            Category(child, parent, new CategoryPriceOptions(Alone: true))
        ]);

        Assert.False(effective[child].Alone);
    }

    [Fact]
    public void When_the_parent_and_the_child_leave_nothing_the_childs_own_switches_apply()
    {
        var parent = Guid.NewGuid();
        var child = Guid.NewGuid();
        var effective = CategoryPriceRules.Effective([
            Category(parent, null, new CategoryPriceOptions(Alone: false)),
            Category(child, parent, new CategoryPriceOptions(false, true, false))
        ]);

        Assert.Equal(new CategoryPriceOptions(false, true, false), effective[child]);
    }

    [Fact]
    public void A_category_without_switches_or_with_an_unknown_parent_offers_everything()
    {
        var orphan = Guid.NewGuid();
        var plain = Guid.NewGuid();
        var effective = CategoryPriceRules.Effective([Category(orphan, Guid.NewGuid()), Category(plain, null)]);

        Assert.Equal(new CategoryPriceOptions(), effective[orphan]);
        Assert.Equal(new CategoryPriceOptions(), effective[plain]);
    }
}

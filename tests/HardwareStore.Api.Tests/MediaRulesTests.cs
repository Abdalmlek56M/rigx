using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Storefront;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Watermark placement and request photo rules: no database, no imaging library.</summary>
public sealed class MediaRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("TopLeft", 24, 24)]
    [InlineData("TopRight", 776, 24)]
    [InlineData("BottomLeft", 24, 676)]
    [InlineData("BottomRight", 776, 676)]
    [InlineData("Center", 400, 350)]
    public void The_logo_is_placed_by_position_with_a_margin_of_three_percent_of_the_shorter_side(string position, int x, int y)
    {
        var rect = WatermarkPlacement.Compute(1000, 800, 200, 100, position, 20);

        Assert.Equal(new WatermarkRect(x, y, 200, 100), rect);
    }

    [Fact]
    public void The_logo_width_follows_the_size_percent_and_keeps_its_aspect()
    {
        var rect = WatermarkPlacement.Compute(480, 360, 300, 100, "BottomRight", 25);

        Assert.Equal(120, rect.Width);
        Assert.Equal(40, rect.Height);
        Assert.Equal(480 - 120 - 11, rect.X);
        Assert.Equal(360 - 40 - 11, rect.Y);
    }

    [Fact]
    public void A_tall_logo_is_shrunk_to_fit_the_image_height()
    {
        var rect = WatermarkPlacement.Compute(1000, 100, 100, 100, "Center", 50);

        Assert.Equal(new WatermarkRect(450, 0, 100, 100), rect);
    }

    [Fact]
    public void An_unknown_position_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WatermarkPlacement.Compute(100, 100, 10, 10, "Middle", 20));
    }

    [Theory]
    [InlineData(CustomerRequestType.Sell, true)]
    [InlineData(CustomerRequestType.TradeIn, true)]
    [InlineData(CustomerRequestType.Upgrade, true)]
    [InlineData(CustomerRequestType.Support, true)]
    [InlineData(CustomerRequestType.NotifyMe, false)]
    [InlineData(CustomerRequestType.Contact, false)]
    [InlineData(CustomerRequestType.Service, false)]
    public void Only_sell_trade_in_upgrade_and_support_requests_take_photos(string type, bool accepts)
    {
        Assert.Equal(accepts, RequestPhotoRules.AcceptsPhotos(type));
    }

    [Fact]
    public void An_issued_token_is_random_hashed_and_valid_for_thirty_minutes()
    {
        var first = RequestPhotoRules.IssueToken(Now);
        var second = RequestPhotoRules.IssueToken(Now);

        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(43, first.Token.Length);
        Assert.Equal(RequestPhotoRules.Hash(first.Token), first.Hash);
        Assert.NotEqual(first.Token, first.Hash);
        Assert.Equal(Now.AddMinutes(30), first.ExpiresAtUtc);
    }

    [Fact]
    public void A_missing_or_expired_token_is_not_found_and_a_full_request_is_a_conflict()
    {
        var open = Request(Now.AddMinutes(1), photos: 4);

        Assert.Same(open, RequestPhotoRules.EnsureCanAdd(open, Now));
        Assert.Equal(404, Assert.Throws<StorefrontException>(() => RequestPhotoRules.EnsureCanAdd(null, Now)).StatusCode);
        Assert.Equal(404, Assert.Throws<StorefrontException>(() => RequestPhotoRules.EnsureCanAdd(Request(Now, photos: 0), Now)).StatusCode);
        Assert.Equal(404, Assert.Throws<StorefrontException>(() => RequestPhotoRules.EnsureCanAdd(Request(null, photos: 0), Now)).StatusCode);
        Assert.Equal(409, Assert.Throws<StorefrontException>(() =>
            RequestPhotoRules.EnsureCanAdd(Request(Now.AddMinutes(1), RequestPhotoRules.MaxPhotos), Now)).StatusCode);
    }

    [Fact]
    public void Orphan_cleanup_never_lists_private_or_logo_files()
    {
        var old = Now.AddDays(-2);
        StoredMediaFile File(string key) => new(key, old, 10);

        var audit = CatalogMediaMaintenanceService.Evaluate([],
            [File("private/requests/a/b.webp"), File("settings/store/c.webp"), File("products/p/d.webp")], Now, TimeSpan.FromMinutes(30));

        Assert.Equal(["products/p/d.webp"], audit.OrphanStorageKeys);
    }

    private static CustomerRequest Request(DateTimeOffset? expires, int photos) => new()
    {
        Code = "REQ-0001", Type = CustomerRequestType.Sell, Status = CustomerRequestStatus.New, Name = "Ahmed", Phone = "01012345678",
        PhotoTokenExpiresAtUtc = expires, Photos = Enumerable.Range(0, photos).Select(index => $"private/requests/x/{index}.webp").ToList()
    };
}

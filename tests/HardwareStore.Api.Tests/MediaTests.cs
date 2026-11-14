using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkiaSharp;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Media (roadmap 2.9): watermark on upload, store logos, re-apply, private photos for requests and expense receipts.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class MediaPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Settings = "/api/v1/admin/storefront/settings";
    private const string RetailApi = "/api/v1/admin/retail";
    private static readonly Guid Actor = Guid.NewGuid();

    private sealed class Env : IAsyncDisposable
    {
        public required OwnerBootstrapDatabase Database { get; init; }
        public required IdentityLoginApiFactory Factory { get; init; }
        public required MutableTimeProvider Clock { get; init; }
        public required string Root { get; init; }
        public required HttpClient Admin { get; init; }
        public required HttpClient Anonymous { get; init; }
        public required RetailTestServices Retail { get; init; }

        public async ValueTask DisposeAsync()
        {
            Admin.Dispose();
            Anonymous.Dispose();
            await Factory.DisposeAsync();
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }

        public string PathOf(string storageKey) => Path.Combine(Root, storageKey.Replace('/', Path.DirectorySeparatorChar));
    }

    private async Task<Env> StartAsync()
    {
        var database = await fixture.CreateDatabaseAsync();
        var root = Path.Combine(Path.GetTempPath(), "rigx-media-" + Guid.NewGuid().ToString("N"));
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var factory = new IdentityLoginApiFactory(database.ConnectionString, timeProvider: clock, configureTestServices: services =>
        {
            services.RemoveAll<IDbContextFactory<StorefrontDbContext>>();
            services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(database.ConnectionString));
            services.RemoveAll<IDbContextFactory<RetailDbContext>>();
            services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(database.ConnectionString));
            services.RemoveAll<IDbContextFactory<CatalogDbContext>>();
            services.AddDbContextFactory<CatalogDbContext>(options => options.UseNpgsql(database.ConnectionString));
            services.RemoveAll<CatalogMediaOptions>();
            services.AddSingleton(new CatalogMediaOptions { RootPath = root });
        });
        await EmailCodeSignInTests.CreateAccountAsync(database, "media-admin@example.test", verified: true, active: true, "media-role",
            "System.StoreSettings", "Inventory.StockAdjust", "Inventory.View", "Requests.View", "Finance.ManageExpenses", "Finance.ViewReports");
        var admin = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await TestSignIn.SignInAsync(admin, factory.Services, "media-admin@example.test");
        return new Env
        {
            Database = database, Factory = factory, Clock = clock, Root = root, Admin = admin,
            Anonymous = factory.CreatePreflightedClient(new() { HandleCookies = true }),
            Retail = database.CreateRetailServices(clock)
        };
    }

    private static byte[] Image(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    private static readonly byte[] Photo = Image(400, 300, SKColors.Blue);
    private static readonly byte[] RedLogo = Image(100, 50, SKColors.Red);

    private static SKColor PixelOf(byte[] webp, int x, int y)
    {
        using var bitmap = SKBitmap.Decode(webp);
        return bitmap.GetPixel(x, y);
    }

    private static async Task<JsonElement> JsonOfAsync(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> SettingsAsync(HttpClient admin) => await JsonOfAsync(await admin.GetAsync(Settings));

    private static async Task SetWatermarkAsync(HttpClient admin, bool enabled)
    {
        var current = await SettingsAsync(admin);
        using var response = await TestSignIn.SendJsonAsync(admin, HttpMethod.Put, $"{Settings}/watermark", new
        {
            value = new { enabled, position = "TopLeft", opacityPercent = 100, sizePercent = 30, applyToArticles = false },
            rowVersion = current.GetProperty("rowVersion").GetInt64()
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> UploadLogoAsync(HttpClient admin, string kind)
    {
        using var response = await TestSignIn.SendImageAsync(admin, HttpMethod.Put, $"{Settings}/logos/{kind}", RedLogo);
        var view = await JsonOfAsync(response);
        return view.GetProperty("logos").GetProperty(kind == "store" ? "storeLogoKey" : "watermarkLogoKey").GetString()!;
    }

    private static async Task<StockItemAdminView> StockItemAsync(Env env) =>
        Assert.Single((await env.Retail.Units.ReceiveAsync(
            RetailReceipts.Receipt(Guid.NewGuid(), null, [new ReceiptLine("Media GPU", "New", 1, 300m, 500m)]), Actor)).Items);

    private static async Task<StockItemPhoto> AddItemPhotoAsync(Env env, Guid itemId)
    {
        using var response = await TestSignIn.SendImageAsync(env.Admin, HttpMethod.Post, $"{RetailApi}/stock-items/{itemId}/photos", Photo);
        var photos = await JsonOfAsync(response);
        var last = photos.EnumerateArray().Last();
        return new StockItemPhoto(last.GetProperty("storageKey").GetString()!, last.GetProperty("thumbnailStorageKey").GetString()!);
    }

    [Fact]
    public async Task The_watermark_is_drawn_on_new_photos_and_never_on_the_kept_original()
    {
        await using var env = await StartAsync();
        var item = await StockItemAsync(env);
        await UploadLogoAsync(env.Admin, "watermark");

        var clean = await AddItemPhotoAsync(env, item.Id);
        await SetWatermarkAsync(env.Admin, enabled: true);
        var marked = await AddItemPhotoAsync(env, item.Id);

        var cleanBytes = await File.ReadAllBytesAsync(env.PathOf(clean.StorageKey));
        var markedBytes = await File.ReadAllBytesAsync(env.PathOf(marked.StorageKey));
        Assert.NotEqual(cleanBytes, markedBytes);
        Assert.True(PixelOf(cleanBytes, 69, 39).Blue > 200);
        var logoPixel = PixelOf(markedBytes, 69, 39);
        Assert.True(logoPixel.Red > 150 && logoPixel.Blue < 100, $"expected the red logo, got {logoPixel}");
        var thumbPixel = PixelOf(await File.ReadAllBytesAsync(env.PathOf(marked.ThumbnailStorageKey)), 69 * 480 / 400, 39 * 480 / 400);
        Assert.True(thumbPixel.Red > 150 && thumbPixel.Blue < 100, $"expected the logo on the thumbnail too, got {thumbPixel}");
        Assert.Equal(Photo, await File.ReadAllBytesAsync(env.PathOf(CatalogMediaKeys.OriginalKeyFor(marked.StorageKey))));
        Assert.Equal(Photo, await File.ReadAllBytesAsync(env.PathOf(CatalogMediaKeys.OriginalKeyFor(clean.StorageKey))));
    }

    [Fact]
    public async Task Logos_are_replaced_under_new_keys_removed_audited_and_untouched_by_section_updates()
    {
        await using var env = await StartAsync();
        var first = await UploadLogoAsync(env.Admin, "store");
        var second = await UploadLogoAsync(env.Admin, "store");
        var watermarkLogo = await UploadLogoAsync(env.Admin, "watermark");
        await SetWatermarkAsync(env.Admin, enabled: true);

        Assert.NotEqual(first, second);
        Assert.StartsWith("settings/store/", second);
        Assert.False(File.Exists(env.PathOf(first)));
        Assert.True(File.Exists(env.PathOf(second)));
        var admin = await SettingsAsync(env.Admin);
        Assert.Equal(second, admin.GetProperty("logos").GetProperty("storeLogoKey").GetString());
        Assert.Equal(watermarkLogo, admin.GetProperty("logos").GetProperty("watermarkLogoKey").GetString());
        using var publicResponse = await env.Anonymous.GetAsync("/api/v1/storefront/settings");
        var publicText = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains(second, publicText);
        Assert.DoesNotContain(watermarkLogo, publicText);
        using var served = await env.Anonymous.GetAsync($"/api/v1/catalog/media/{second}");
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);

        using var unknown = await TestSignIn.SendImageAsync(env.Admin, HttpMethod.Put, $"{Settings}/logos/banner", RedLogo);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        using var removed = await TestSignIn.SendJsonAsync(env.Admin, HttpMethod.Delete, $"{Settings}/logos/store", null);
        var afterRemove = await JsonOfAsync(removed);
        Assert.Equal(JsonValueKind.Null, afterRemove.GetProperty("logos").GetProperty("storeLogoKey").ValueKind);
        Assert.False(File.Exists(env.PathOf(second)));

        await using var audit = env.Database.CreateAuditContext();
        var events = await audit.SecurityAuditEvents.Select(entry => entry.EventType).ToListAsync();
        Assert.Equal(3, events.Count(type => type == "Storefront.LogoUpdated"));
        Assert.Single(events, type => type == "Storefront.LogoRemoved");
    }

    [Fact]
    public async Task Reapply_renders_every_photo_with_an_original_from_the_current_settings_into_new_keys()
    {
        await using var env = await StartAsync();
        var item = await StockItemAsync(env);
        await UploadLogoAsync(env.Admin, "watermark");
        await SetWatermarkAsync(env.Admin, enabled: true);
        var marked = await AddItemPhotoAsync(env, item.Id);
        await env.Retail.StockItems.AddPhotoAsync(item.Id, new StockItemPhoto("legacy/none.webp", "legacy/none-thumb.webp"));
        await SetWatermarkAsync(env.Admin, enabled: false);

        using var scope = env.Factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<WatermarkReapplyService>().RunAsync(CancellationToken.None);

        Assert.Equal(new WatermarkReapplyResult(1, 1, 0), result);
        var photos = (await env.Retail.StockItems.GetAsync(item.Id, false, false)).Photos;
        Assert.Equal(2, photos.Count);
        Assert.DoesNotContain(photos, photo => photo.StorageKey == marked.StorageKey);
        Assert.Contains(photos, photo => photo.StorageKey == "legacy/none.webp");
        var renewed = photos.Single(photo => photo.StorageKey != "legacy/none.webp");
        Assert.True(PixelOf(await File.ReadAllBytesAsync(env.PathOf(renewed.StorageKey)), 69, 39).Blue > 200);
        Assert.Equal(Photo, await File.ReadAllBytesAsync(env.PathOf(CatalogMediaKeys.OriginalKeyFor(renewed.StorageKey))));
        Assert.True(File.Exists(env.PathOf(marked.StorageKey)));
    }

    [Fact]
    public async Task Reapply_is_accepted_for_the_settings_permission_only()
    {
        await using var env = await StartAsync();

        using var accepted = await TestSignIn.SendJsonAsync(env.Admin, HttpMethod.Post, $"{Settings}/watermark/reapply", null);
        using var anonymous = await TestSignIn.SendJsonAsync(env.Anonymous, HttpMethod.Post, $"{Settings}/watermark/reapply", null);

        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Private_images_are_never_served_by_the_public_media_route()
    {
        await using var env = await StartAsync();
        var storage = env.Factory.Services.GetRequiredService<ICatalogMediaStorage>();
        CatalogImageUpload Upload() => new(new MemoryStream(Photo), Photo.Length, null, null);

        var key = await storage.SavePrivateImageAsync("requests", Guid.NewGuid(), Upload());

        Assert.StartsWith("private/requests/", key);
        Assert.True(File.Exists(env.PathOf(key)));
        using var response = await env.Anonymous.GetAsync($"/api/v1/catalog/media/{key}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await Assert.ThrowsAsync<CatalogValidationException>(() => storage.SavePrivateImageAsync("avatars", Guid.NewGuid(), Upload()));
    }

    [Fact]
    public async Task A_customer_attaches_up_to_five_private_photos_with_the_token_from_the_receipt()
    {
        await using var env = await StartAsync();
        var sell = new { type = "Sell", name = "Ahmed", phone = "01012345678", modelName = "RTX 3060", partType = "GPU" };

        var first = await SubmitAsync(env, sell);
        var again = await SubmitAsync(env, sell);
        using var contact = await TestSignIn.PostJsonAsync(env.Anonymous, "/api/v1/requests",
            new { type = "Contact", name = "Ahmed", phone = "01099999999", message = "I would like some help with my PC." });

        Assert.Equal(first.Code, again.Code);
        Assert.NotNull(first.PhotoUploadToken);
        Assert.NotEqual(first.PhotoUploadToken, again.PhotoUploadToken);
        Assert.Null((await contact.Content.ReadFromJsonAsync<CustomerRequestReceipt>())!.PhotoUploadToken);
        using (var replaced = await UploadPhotoAsync(env, first.PhotoUploadToken!))
            Assert.Equal(HttpStatusCode.NotFound, replaced.StatusCode);
        using (var unknown = await UploadPhotoAsync(env, "not-a-token"))
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        for (var count = 1; count <= RequestPhotoRules.MaxPhotos; count++)
        {
            using var uploaded = await UploadPhotoAsync(env, again.PhotoUploadToken!);
            Assert.Equal(count, (await JsonOfAsync(uploaded)).GetProperty("photoCount").GetInt32());
        }
        using (var sixth = await UploadPhotoAsync(env, again.PhotoUploadToken!))
            Assert.Equal(HttpStatusCode.Conflict, sixth.StatusCode);

        var request = await FindRequestAsync(env, first.Code!);
        Assert.Equal(5, request.PhotoCount);
        using var stream = await env.Admin.GetAsync($"/api/v1/admin/requests/{request.Id}/photos/0");
        Assert.Equal(HttpStatusCode.OK, stream.StatusCode);
        Assert.Equal("image/webp", stream.Content.Headers.ContentType!.MediaType);
        Assert.Contains("no-store", stream.Headers.CacheControl!.ToString());
        using var missing = await env.Admin.GetAsync($"/api/v1/admin/requests/{request.Id}/photos/9");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var anonymousRead = await env.Anonymous.GetAsync($"/api/v1/admin/requests/{request.Id}/photos/0");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousRead.StatusCode);
    }

    [Fact]
    public async Task The_photo_token_stops_working_after_thirty_minutes()
    {
        await using var env = await StartAsync();
        var receipt = await SubmitAsync(env, new { type = "Support", name = "Sara", phone = "01011112222", message = "My PC will not boot at all." });

        using (var early = await UploadPhotoAsync(env, receipt.PhotoUploadToken!))
            Assert.Equal(HttpStatusCode.OK, early.StatusCode);
        env.Clock.Advance(RequestPhotoRules.TokenLifetime + TimeSpan.FromMinutes(1));

        using var late = await UploadPhotoAsync(env, receipt.PhotoUploadToken!);
        Assert.Equal(HttpStatusCode.NotFound, late.StatusCode);
    }

    [Fact]
    public async Task An_expense_receipt_is_private_replaceable_and_streamed_without_caching()
    {
        await using var env = await StartAsync();
        await env.Retail.FundAsync(1000m);
        var expense = await env.Retail.Expenses.CreateExpenseAsync(
            new ExpenseCreateRequest(new DateOnly(2026, 10, 15), 50m, "إيجار", ExpenseClassification.GeneralOverhead, null, null, null), Actor);
        var url = $"{RetailApi}/expenses/{expense.Id}/receipt";

        Assert.False(expense.HasReceipt);
        using (var none = await env.Admin.GetAsync(url))
            Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
        using var first = await TestSignIn.SendImageAsync(env.Admin, HttpMethod.Put, url, Photo);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        var directory = Path.Combine(env.Root, "private", "receipts", expense.Id.ToString("N"));
        var firstFile = Assert.Single(Directory.GetFiles(directory));
        using var second = await TestSignIn.SendImageAsync(env.Admin, HttpMethod.Put, url, Photo);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var secondFile = Assert.Single(Directory.GetFiles(directory));
        Assert.NotEqual(firstFile, secondFile);
        using var streamed = await env.Admin.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, streamed.StatusCode);
        Assert.Equal("image/webp", streamed.Content.Headers.ContentType!.MediaType);
        Assert.Contains("no-store", streamed.Headers.CacheControl!.ToString());
        var listed = await JsonOfAsync(await env.Admin.GetAsync($"{RetailApi}/expenses"));
        Assert.True(listed.EnumerateArray().Single().GetProperty("hasReceipt").GetBoolean());
        using var unknown = await TestSignIn.SendImageAsync(env.Admin, HttpMethod.Put, $"{RetailApi}/expenses/{Guid.NewGuid()}/receipt", Photo);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    private static async Task<CustomerRequestReceipt> SubmitAsync(Env env, object body)
    {
        using var response = await TestSignIn.PostJsonAsync(env.Anonymous, "/api/v1/requests", body);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CustomerRequestReceipt>())!;
    }

    private static Task<HttpResponseMessage> UploadPhotoAsync(Env env, string token) =>
        TestSignIn.SendImageAsync(env.Anonymous, HttpMethod.Post, "/api/v1/requests/photos", Photo, ("token", token));

    private static async Task<CustomerRequestView> FindRequestAsync(Env env, string code)
    {
        var page = await JsonOfAsync(await env.Admin.GetAsync($"/api/v1/admin/requests?q={code}"));
        var id = page.GetProperty("items").EnumerateArray().Single().GetProperty("id").GetGuid();
        return (await env.Admin.GetFromJsonAsync<CustomerRequestView>($"/api/v1/admin/requests/{id}"))!;
    }
}

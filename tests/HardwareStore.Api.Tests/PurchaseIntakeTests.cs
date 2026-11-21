using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class PurchaseIntakePostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 11, 10);

    private sealed class Harness(OwnerBootstrapDatabase database)
    {
        public SellerService Sellers { get; } = new(Factory<PurchasingDbContext>(database), TimeProvider.System);
        public PurchaseService Purchases { get; } = new(Factory<PurchasingDbContext>(database), TimeProvider.System, RetailSales.BusinessZone);
        public RetailTestServices Retail { get; } = database.CreateRetailServices();
        private PurchaseReceiptTesting? receipts;
        private PurchaseReceiptTesting Receipting => receipts ??= new(database, Purchases, Retail);
        public LocalCatalogMediaStorage Media => Receipting.Media;
        public PurchaseReceiptService Receipts => Receipting.Create();
        public PurchasePaymentService Payments => new(Purchases, Retail.Money, TimeProvider.System, RetailSales.BusinessZone);

        public async Task<SellerView> NewSellerAsync(string phone = "01012345678") =>
            await Sellers.CreateAsync(new SellerRequest("Person", "Ahmed", phone, null, null, null, null, null));

        private static TestFactory<T> Factory<T>(OwnerBootstrapDatabase db) where T : DbContext => new(db.ConnectionString);
    }

    private sealed class TestFactory<T>(string connectionString) : IDbContextFactory<T> where T : DbContext
    {
        public T CreateDbContext() =>
            (T)Activator.CreateInstance(typeof(T), new DbContextOptionsBuilder<T>().UseNpgsql(connectionString).Options)!;
    }

    private static PurchaseRequest Parts(Guid sellerId, decimal paid, params PurchaseLineRequest[] lines) =>
        new(sellerId, PurchaseKind.Parts, false, null, Day, null, SellerPaymentMethod.Cash, paid, 0, null, lines);

    private static PurchaseRequest FullPc(Guid sellerId, params PurchaseLineRequest[] lines) =>
        new(sellerId, PurchaseKind.FullPc, true, "Gaming PC", Day, null, SellerPaymentMethod.Cash, 0, 0, null, lines);

    private static PurchaseLineRequest Used(string model, int quantity = 1, string? warranty = null) =>
        new(model, "Used", quantity, 2000m, 3000m, SerialNumber: quantity == 1 ? "SN-" + model : null, WarrantyLabel: warranty);

    private static PurchaseLineRequest New(string model, int quantity) => new(model, "New", quantity, 100m, 150m);

    [Fact]
    public async Task Second_seller_with_the_same_phone_is_a_conflict_naming_the_first()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var first = await harness.NewSellerAsync();

        var error = await Assert.ThrowsAsync<PurchasingException>(() => harness.NewSellerAsync("+20 101 234 5678"));

        Assert.Equal(409, error.StatusCode);
        Assert.Contains(first.Id.ToString(), error.Message);
        var found = Assert.Single(await harness.Sellers.ListAsync("010-1234-5678", null, false));
        Assert.Equal(first.Id, found.Id);
    }

    [Fact]
    public async Task National_id_is_hidden_unless_the_caller_may_see_it()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.Sellers.CreateAsync(new SellerRequest("Trader", "Mona", "01111111111", "29801011234567", "C", null, null, null));

        Assert.Equal("29801011234567", (await harness.Sellers.GetAsync(seller.Id, true)).NationalId);
        Assert.Null((await harness.Sellers.GetAsync(seller.Id, false)).NationalId);
    }

    [Fact]
    public async Task Purchase_gets_the_next_PO_code_and_the_remaining_amount()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();

        var first = await harness.Purchases.CreateAsync(Parts(seller.Id, 1000m, Used("RTX 3060", 2)), Actor);
        var second = await harness.Purchases.CreateAsync(Parts(seller.Id, 0m, New("Fan", 3)), Actor);

        Assert.Equal("PO-0001", first.Code);
        Assert.Equal("PO-0002", second.Code);
        Assert.Equal(4000m, first.Total);
        Assert.Equal(3000m, first.Remaining);
        Assert.Equal(PurchaseStatus.Draft, first.Status);
        var byCode = await harness.Purchases.ListAsync(new PurchaseQuery(Search: "po 2"));
        Assert.Equal(second.Id, Assert.Single(byCode.Items).Id);
        Assert.Equal(2, (await harness.Purchases.ListAsync(new PurchaseQuery(Search: "01012345678"))).Total);
    }

    [Fact]
    public async Task Receiving_a_parts_purchase_creates_the_stock_items_and_closes_the_purchase()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(Parts(seller.Id, 0m, Used("RTX 3060", 2, "شهر"), New("Fan", 3)), Actor);

        var received = await harness.Receipts.ReceiveAsync(purchase.Id, Actor, default);

        Assert.Equal(3, received.Items.Items.Count);
        Assert.Equal(PurchaseStatus.Received, received.Purchase.Status);
        Assert.NotNull(received.Purchase.ReceivedAtUtc);
        Assert.Null(received.Purchase.AssemblyId);
        var used = received.Items.Items.Where(item => item.Condition == "Used").ToList();
        Assert.All(used, item => Assert.Equal(1, item.WarrantyMonths));
        Assert.Equal(3, Assert.Single(received.Items.Items, item => item.Condition == "New").Units.Count);
    }

    [Fact]
    public async Task A_line_copied_from_an_existing_item_gets_its_brand_and_each_new_piece_its_own_copy_of_the_photos()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();
        var source = (await harness.Receipts.ReceiveAsync(
            (await harness.Purchases.CreateAsync(Parts(seller.Id, 0m, Used("RTX 4060")), Actor)).Id, Actor, default)).Items.Items.Single();
        var png = PhotoPng();
        var stored = await harness.Media.SaveImageAsync(source.Id, Guid.CreateVersion7(), new CatalogImageUpload(new MemoryStream(png), png.Length, "a.png", "image/png"));
        await harness.Retail.StockItems.AddPhotoAsync(source.Id, new StockItemPhoto(stored.StorageKey, stored.ThumbnailStorageKey));
        var brandId = Guid.NewGuid();
        var copy = Used("RTX 4060", 2) with { BrandId = brandId, CopyPhotosFromItemId = source.Id };

        var purchase = await harness.Purchases.CreateAsync(Parts(seller.Id, 0m, copy, New("Fan", 1)), Actor);
        Assert.Equal(source.Id, purchase.Lines[0].CopyPhotosFromItemId);
        var received = await harness.Receipts.ReceiveAsync(purchase.Id, Actor, default);

        var pieces = received.Items.Items.Where(item => item.Condition == "Used").ToList();
        Assert.Equal(2, pieces.Count);
        foreach (var piece in pieces)
        {
            Assert.Equal(brandId, piece.BrandId);
            var photo = Assert.Single(await harness.Retail.StockItems.FindPhotosAsync(piece.Id));
            Assert.NotEqual(stored.StorageKey, photo.StorageKey);
            Assert.True(harness.Media.Exists(photo.StorageKey));
        }
        Assert.Empty(await harness.Retail.StockItems.FindPhotosAsync(received.Items.Items.Single(item => item.Condition == "New").Id));
    }

    private static byte[] PhotoPng()
    {
        using var bitmap = new SKBitmap(64, 48);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100)!;
        return data.ToArray();
    }

    [Fact]
    public async Task Receiving_twice_is_a_conflict_and_creates_no_second_set_of_units()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(Parts(seller.Id, 0m, Used("RX 6600")), Actor);
        await harness.Receipts.ReceiveAsync(purchase.Id, Actor, default);

        var error = await Assert.ThrowsAsync<PurchasingException>(() => harness.Receipts.ReceiveAsync(purchase.Id, Actor, default));

        Assert.Equal(409, error.StatusCode);
        Assert.Single((await harness.Retail.StockItems.ListAsync(new StockItemQuery(), true, true)).Items);
    }

    [Fact]
    public async Task Full_pc_kept_as_a_build_becomes_one_assembly_with_all_units()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(FullPc(seller.Id, Used("Ryzen 5 5600"), Used("RTX 3060"), New("DDR4 8GB", 2)), Actor);

        var received = await harness.Receipts.ReceiveAsync(purchase.Id, Actor, default);

        var assembly = Assert.Single(await harness.Retail.Assemblies.ListAssembliesAsync(true));
        Assert.Equal(received.Purchase.AssemblyId, assembly.Id);
        Assert.Equal("BLD-01", assembly.Code);
        Assert.Equal("Gaming PC", assembly.Name);
        Assert.Equal(4, assembly.Units.Count);
        Assert.Equal(received.Items.Items.SelectMany(item => item.Units).Select(unit => unit.Id).Order(), assembly.Units.Select(unit => unit.UnitId).Order());
    }

    [Fact]
    public async Task Unknown_warranty_label_is_a_bad_request_and_the_purchase_stays_a_draft()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(Parts(seller.Id, 0m, Used("RX 580", 1, "99 years")), Actor);

        var error = await Assert.ThrowsAsync<RetailException>(() => harness.Receipts.ReceiveAsync(purchase.Id, Actor, default));

        Assert.Equal(400, error.StatusCode);
        Assert.Equal(PurchaseStatus.Draft, (await harness.Purchases.GetAsync(purchase.Id)).Status);
        Assert.Empty((await harness.Retail.StockItems.ListAsync(new StockItemQuery(), true, true)).Items);
    }

    [Fact]
    public async Task A_received_purchase_cannot_be_edited_or_cancelled_and_a_stale_draft_edit_is_a_conflict()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        await harness.Retail.FundAsync(1000m);
        var seller = await harness.NewSellerAsync();
        var stale = await harness.Purchases.CreateAsync(Parts(seller.Id, 0m, Used("GTX 1060")), Actor);
        await harness.Purchases.UpdateAsync(stale.Id, Parts(seller.Id, 500m, Used("GTX 1060")) with { RowVersion = stale.RowVersion });
        var staleEdit = await Assert.ThrowsAsync<PurchasingException>(() =>
            harness.Purchases.UpdateAsync(stale.Id, Parts(seller.Id, 0m, Used("GTX 1060")) with { RowVersion = stale.RowVersion }));
        Assert.Equal(409, staleEdit.StatusCode);

        var received = (await harness.Receipts.ReceiveAsync(stale.Id, Actor, default)).Purchase;
        var edit = await Assert.ThrowsAsync<PurchasingException>(() =>
            harness.Purchases.UpdateAsync(received.Id, Parts(seller.Id, 0m, Used("GTX 1060")) with { RowVersion = received.RowVersion }));
        var cancel = await Assert.ThrowsAsync<PurchasingException>(() => harness.Purchases.CancelAsync(received.Id, received.RowVersion));

        Assert.Equal(409, edit.StatusCode);
        Assert.Equal(409, cancel.StatusCode);
    }

    [Fact]
    public async Task Cancelling_a_draft_closes_it_for_receiving()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(Parts(seller.Id, 0m, Used("RX 570")), Actor);

        var cancelled = await harness.Purchases.CancelAsync(purchase.Id, purchase.RowVersion);

        Assert.Equal(PurchaseStatus.Cancelled, cancelled.Status);
        var error = await Assert.ThrowsAsync<PurchasingException>(() => harness.Receipts.ReceiveAsync(purchase.Id, Actor, default));
        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task Receiving_a_purchase_pays_the_seller_from_the_account_of_the_payment_method()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        await harness.Retail.FundAsync(1000m);
        await harness.Retail.FundAsync(300m, MoneyAccountKind.InstaPay);
        var seller = await harness.NewSellerAsync();
        var cash = await harness.Purchases.CreateAsync(Parts(seller.Id, 500m, Used("RTX 3060")), Actor);
        var insta = await harness.Purchases.CreateAsync(Parts(seller.Id, 300m, Used("RX 6600")) with { PaymentMethod = SellerPaymentMethod.InstaPay }, Actor);

        await harness.Receipts.ReceiveAsync(cash.Id, Actor, default);
        await harness.Receipts.ReceiveAsync(insta.Id, Actor, default);

        Assert.Equal((500m, 0m), (await harness.Retail.BalanceAsync(), await harness.Retail.BalanceAsync(MoneyAccountKind.InstaPay)));
        var movement = Assert.Single(await harness.Retail.Money.ListMovementsAsync((await harness.Retail.AccountAsync()).Id, null, null),
            item => item.Source == MoneySource.Purchase);
        Assert.Equal((-500m, cash.Code), (movement.Amount, movement.Reference));
    }

    [Fact]
    public async Task A_purchase_the_account_cannot_pay_is_refused_before_anything_is_received()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        await harness.Retail.FundAsync(100m);
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(Parts(seller.Id, 500m, Used("RTX 3060")), Actor);

        var error = await Assert.ThrowsAsync<RetailException>(() => harness.Receipts.ReceiveAsync(purchase.Id, Actor, default));

        Assert.Equal(409, error.StatusCode);
        Assert.Empty(await harness.Retail.Units.ListUnitsAsync(null, null));
        Assert.Equal(PurchaseStatus.Draft, (await harness.Purchases.GetDraftAsync(purchase.Id, default)).Status);
        Assert.Equal(100m, await harness.Retail.BalanceAsync());
        await harness.Retail.FundAsync(400m);
        await harness.Receipts.ReceiveAsync(purchase.Id, Actor, default);
        Assert.Equal(0m, await harness.Retail.BalanceAsync());
    }

    [Fact]
    public async Task Old_stock_paid_outside_the_cash_accounts_is_received_without_touching_them()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        await harness.Retail.FundAsync(100m);
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(Parts(seller.Id, 2000m, Used("RTX 3060")) with { PayFromCash = false }, Actor);

        var received = await harness.Receipts.ReceiveAsync(purchase.Id, Actor, default);

        Assert.False(received.Purchase.PayFromCash);
        Assert.Equal((2000m, 0m), (received.Purchase.PaidAmount, received.Purchase.Remaining));
        Assert.Equal(100m, await harness.Retail.BalanceAsync());
        Assert.DoesNotContain(await harness.Retail.Money.ListMovementsAsync((await harness.Retail.AccountAsync()).Id, null, null),
            item => item.Source == MoneySource.Purchase);
    }

    [Fact]
    public async Task What_is_still_owed_to_the_seller_can_be_paid_later_from_cash_but_never_beyond_it()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        await harness.Retail.FundAsync(5000m);
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(Parts(seller.Id, 500m, Used("RTX 3060")), Actor);

        var early = await Assert.ThrowsAsync<PurchasingException>(() => harness.Payments.PayAsync(purchase.Id, new PurchasePaymentRequest(100m, null, purchase.RowVersion), Actor, default));
        Assert.Equal(409, early.StatusCode);

        var received = (await harness.Receipts.ReceiveAsync(purchase.Id, Actor, default)).Purchase;
        Assert.Equal((500m, 1500m), (received.PaidAmount, received.Remaining));

        var paid = await harness.Payments.PayAsync(purchase.Id, new PurchasePaymentRequest(1000m, "Cash", received.RowVersion), Actor, default);
        Assert.Equal((1500m, 500m), (paid.PaidAmount, paid.Remaining));
        Assert.Equal(3500m, await harness.Retail.BalanceAsync());

        var tooMuch = await Assert.ThrowsAsync<PurchasingException>(() => harness.Payments.PayAsync(purchase.Id, new PurchasePaymentRequest(600m, null, paid.RowVersion), Actor, default));
        Assert.Equal(400, tooMuch.StatusCode);
        Assert.Equal(3500m, await harness.Retail.BalanceAsync());
    }

    [Fact]
    public async Task A_build_purchase_starts_its_sale_price_at_the_parts_total()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();
        var purchase = await harness.Purchases.CreateAsync(FullPc(seller.Id, Used("RTX 3060"), Used("i5 12400F")), Actor);

        var received = await harness.Receipts.ReceiveAsync(purchase.Id, Actor, default);

        var build = await harness.Retail.Assemblies.GetAsync(received.Purchase.AssemblyId!.Value, includeCosts: true);
        Assert.Equal(6000m, build.SalePrice);
    }

    [Fact]
    public async Task A_pure_trade_in_moves_no_money_and_the_cash_part_of_a_mixed_one_comes_from_cash()
    {
        var harness = new Harness(await fixture.CreateDatabaseAsync());
        var seller = await harness.NewSellerAsync();
        var tradeIn = await harness.Purchases.CreateAsync(new PurchaseRequest(seller.Id, PurchaseKind.Parts, false, null, Day, null,
            SellerPaymentMethod.TradeIn, 0, 2000m, "old card", [Used("RTX 3060")]), Actor);
        await harness.Receipts.ReceiveAsync(tradeIn.Id, Actor, default);

        Assert.All(await harness.Retail.Money.ListAsync(), account => Assert.Equal(0m, account.Balance));
        Assert.Empty(await harness.Retail.Money.ListMovementsAsync((await harness.Retail.AccountAsync()).Id, null, null));

        var mixed = await harness.Purchases.CreateAsync(new PurchaseRequest(seller.Id, PurchaseKind.Parts, false, null, Day, null,
            SellerPaymentMethod.CashAndTradeIn, 500m, 1500m, "old card", [Used("RX 6600")]), Actor);
        Assert.Equal(409, (await Assert.ThrowsAsync<RetailException>(() => harness.Receipts.ReceiveAsync(mixed.Id, Actor, default))).StatusCode);
        await harness.Retail.FundAsync(500m);
        await harness.Receipts.ReceiveAsync(mixed.Id, Actor, default);
        Assert.Equal(0m, await harness.Retail.BalanceAsync());
    }
}

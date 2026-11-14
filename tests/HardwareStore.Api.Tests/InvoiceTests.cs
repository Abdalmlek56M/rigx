using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using static HardwareStore.Api.Tests.RetailReceipts;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>Invoices issued on delivery, the public invoice link, admin and customer lists, warranty search by invoice and quotes (roadmap 2.6).</summary>
[Trait("Category", "RequiresDocker")]
public sealed class InvoicePostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string AdminRoot = "/api/v1/admin/retail";
    private const string BuyerPhone = "01289186840";
    private static readonly string[] FullStaff = ["Sales.ViewOrders", "Sales.ApproveOrder", "Sales.ViewCustomerContact", "Inventory.ViewSerials"];
    private static readonly int Year = DateTimeOffset.UtcNow.Year;

    private static string Number(int sequence, string prefix = "INV") => InvoiceRules.FormatNumber(prefix, Year, sequence);

    private sealed class Env(OwnerBootstrapDatabase database, IdentityLoginApiFactory factory) : IAsyncDisposable
    {
        private readonly List<HttpClient> _clients = [];

        public OwnerBootstrapDatabase Database { get; } = database;
        public IdentityLoginApiFactory Factory { get; } = factory;
        public RetailTestServices Retail { get; } = database.CreateRetailServices();
        public HttpClient Staff { get; private set; } = null!;
        public Guid StaffId { get; private set; }
        public HttpClient Anonymous => NewClient();

        public static async Task<Env> StartAsync(OwnerBootstrapPostgreSqlFixture fixture)
        {
            var database = await fixture.CreateDatabaseAsync();
            var env = new Env(database, new IdentityLoginApiFactory(database.ConnectionString, configureTestServices: services =>
            {
                services.RemoveAll<IDbContextFactory<RetailDbContext>>();
                services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<CatalogDbContext>>();
                services.AddDbContextFactory<CatalogDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<StorefrontDbContext>>();
                services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(database.ConnectionString));
            }));
            (env.Staff, env.StaffId) = await env.SignedInAsync("staff", FullStaff);
            return env;
        }

        public HttpClient NewClient()
        {
            var client = Factory.CreatePreflightedClient(new() { HandleCookies = true });
            _clients.Add(client);
            return client;
        }

        public async Task<(HttpClient Client, Guid AccountId)> SignedInAsync(string name, params string[] permissions)
        {
            var id = await EmailCodeSignInTests.CreateAccountAsync(Database, $"{name}@example.test", verified: true, active: true,
                roleCode: $"{name}-role", permissions);
            var client = NewClient();
            await TestSignIn.SignInAsync(client, Factory.Services, $"{name}@example.test");
            return (client, id);
        }

        /// <summary>Confirms, prepares (and ships a delivery order) through the service, then sets Delivered through the API.</summary>
        public async Task<OrderView> DeliverAsync(OrderView order)
        {
            var steps = order.FulfillmentMethod == OrderFulfillment.Delivery
                ? new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped }
                : [OrderStatus.Confirmed, OrderStatus.Preparing];
            var ready = await Retail.StatusesAsync(order, steps);
            return await PostStatusAsync(ready.Id, ready.RowVersion, OrderStatus.Delivered);
        }

        public async Task<long> RowVersionAsync(Guid orderId) => (await Retail.Orders.GetAdminOrderDetailAsync(orderId, false)).Order.RowVersion;

        public async Task<OrderView> PostStatusAsync(Guid id, long rowVersion, string status)
        {
            using var response = await TestSignIn.PostJsonAsync(Staff, $"{AdminRoot}/orders/{id}/status", new { status, rowVersion });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<OrderView>())!;
        }

        public async Task<InvoiceDocument> InvoiceOfAsync(OrderView order, HttpClient? client = null) =>
            await GetAsync<InvoiceDocument>(client ?? Staff, $"{AdminRoot}/orders/{order.Id}/invoice");

        public static async Task<T> GetAsync<T>(HttpClient client, string url)
        {
            using var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        public async Task<StockItemAdminView> SellableAsync(string model, string? serial = null, string label = "ضمان المحل", int? months = 3,
            decimal price = 500m, int quantity = 1)
        {
            var item = await Retail.ReceiveItemAsync(new ReceiptLine(model, "New", quantity, 100m, price, SerialNumber: serial,
                WarrantyLabel: label, WarrantyMonths: months));
            await Retail.PublishAsync(item);
            return item;
        }

        /// <summary>Sells one fresh item as a delivered order.</summary>
        public async Task<(StockItemAdminView Item, OrderView Order)> DeliveredAsync(string model, string phone = BuyerPhone, Guid? userId = null,
            string? email = null, string? serial = null, string fulfillment = OrderFulfillment.Pickup)
        {
            var item = await SellableAsync(model, serial);
            var order = await Retail.CheckoutAsync(Checkout([StockLine(item.Id)], fulfillment, phone: phone, email: email), Policy(userId));
            return (item, await DeliverAsync(order));
        }

        public async Task<AssemblyView> PublishedBuildAsync(string name, decimal price, IReadOnlyList<Guid> unitIds, string sellMode = AssemblySellMode.Full)
        {
            var build = await Retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest(name, unitIds));
            var card = await Retail.Assemblies.UpdateAssemblyAsync(build.Id, new AssemblyUpdateRequest(name, "عنوان", "Title", price, build.RowVersion,
                Usage: "Gaming", SellMode: sellMode, WarrantyLabel: "Build", WarrantyMonths: 12));
            return await Retail.Assemblies.SetAssemblyPublishedAsync(card.Id, true, card.RowVersion);
        }

        public async Task<List<Guid>> ReceiveUnitsAsync(params ReceiptLine[] lines) =>
            (await Retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null, lines), Guid.NewGuid())).Select(item => item.Units[0].Id).ToList();

        public async ValueTask DisposeAsync()
        {
            foreach (var client in _clients) client.Dispose();
            await Factory.DisposeAsync();
        }
    }

    private static ReceiptLine Part(string model, decimal price) =>
        new(model, "New", 1, 100m, price, WarrantyLabel: "Item", WarrantyMonths: 1);

    // ---------------------------------------------------------------- issue on delivery

    [Fact]
    public async Task Delivery_issues_numbered_invoices_per_year_at_the_warranty_start_and_repeating_delivered_issues_no_second_one()
    {
        await using var env = await Env.StartAsync(fixture);
        var (firstItem, first) = await env.DeliveredAsync("Invoice GPU 1");
        var (_, second) = await env.DeliveredAsync("Invoice GPU 2");

        var firstDoc = await env.InvoiceOfAsync(first);
        var secondDoc = await env.InvoiceOfAsync(second);

        Assert.Equal((Number(1), Number(2)), (firstDoc.Invoice.Number, secondDoc.Invoice.Number));
        Assert.Equal((InvoiceStatus.Issued, first.Reference), (firstDoc.Invoice.Status, firstDoc.Invoice.OrderReference));
        var warranty = Assert.Single((await env.Retail.Warranties.ListAdminAsync(firstItem.Code, null, 1, true, true)).Items);
        Assert.Equal(warranty.StartsAtUtc, firstDoc.Invoice.IssuedAtUtc);

        await env.PostStatusAsync(first.Id, await env.RowVersionAsync(first.Id), OrderStatus.Delivered);

        await using var db = env.Database.CreateRetailContext();
        Assert.Equal(2, await db.Invoices.CountAsync());
        Assert.Equal(Number(1), (await env.InvoiceOfAsync(first)).Invoice.Number);
    }

    [Fact]
    public async Task The_invoice_number_uses_the_prefix_from_the_store_settings()
    {
        await using var env = await Env.StartAsync(fixture);
        await using (var scope = env.Factory.Services.CreateAsyncScope())
        {
            var settings = scope.ServiceProvider.GetRequiredService<StoreSettingsService>();
            var current = await settings.GetAdminAsync();
            await settings.UpdateSectionAsync(new InvoiceSettings { NumberPrefix = "rgx" }, current.RowVersion, env.StaffId);
        }

        var (_, order) = await env.DeliveredAsync("Prefix GPU");

        Assert.Equal(Number(1, "RGX"), (await env.InvoiceOfAsync(order)).Invoice.Number);
    }

    [Fact]
    public async Task An_order_has_no_invoice_before_delivery()
    {
        await using var env = await Env.StartAsync(fixture);
        var item = await env.SellableAsync("Early GPU");
        var order = await env.Retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup));
        var prepared = await env.Retail.StatusesAsync(order, OrderStatus.Confirmed, OrderStatus.Preparing);

        using var early = await env.Staff.GetAsync($"{AdminRoot}/orders/{prepared.Id}/invoice");
        using var unknown = await env.Staff.GetAsync($"{AdminRoot}/orders/{Guid.NewGuid()}/invoice");

        Assert.Equal(HttpStatusCode.NotFound, early.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    // ---------------------------------------------------------------- public document

    [Fact]
    public async Task The_public_link_shows_lines_parts_warranty_totals_branding_and_links()
    {
        await using var env = await Env.StartAsync(fixture);
        var (item, order) = await env.DeliveredAsync("Public GPU", serial: "SN-PUBLIC-1");
        var token = (await env.InvoiceOfAsync(order)).Invoice.AccessToken;
        var warranty = Assert.Single((await env.Retail.Warranties.ListAdminAsync(item.Code, null, 1, true, true)).Items);

        var document = await Env.GetAsync<InvoiceDocument>(env.Anonymous, $"/api/v1/invoices/{token}");

        var invoice = document.Invoice;
        Assert.Equal((Number(1), InvoiceKind.Parts, InvoiceStatus.Issued, BuyerPhone), (invoice.Number, invoice.Kind, invoice.Status, invoice.PhoneNumber));
        var line = Assert.Single(invoice.Lines);
        Assert.Equal((item.Code, "Public GPU", (string?)null, 1), (line.Code, line.Name, line.BuildCode, line.Quantity));
        var part = Assert.Single(line.Parts);
        Assert.Equal((item.Code, "New", "SN-PUBLIC-1", "ضمان المحل"), (part.Code, part.Condition, part.SerialNumber, part.WarrantyLabel));
        Assert.Equal(warranty.EndsAtUtc, part.WarrantyEndsAtUtc);
        Assert.Equal(WarrantyStatus.Active, part.WarrantyStatus);
        Assert.Equal((order.Total, order.PaidAmount, order.Remaining), (invoice.Total, invoice.Paid, invoice.Remaining));
        Assert.Equal(invoice.LinesTotal + invoice.DeliveryFee - invoice.Discount, invoice.Total);
        Assert.True(document.Branding.ShowBackgroundLogo);
        Assert.True(document.Branding.ShowWarrantyQr);
        Assert.False(string.IsNullOrWhiteSpace(document.Branding.StoreName));
        Assert.EndsWith($"/i/{token}", document.InvoiceUrl);
        Assert.Contains(Uri.EscapeDataString(invoice.Number), document.WarrantyLookupUrl);
    }

    [Fact]
    public async Task Invoice_parts_show_their_part_type_from_the_catalog_category()
    {
        await using var env = await Env.StartAsync(fixture);
        CategoryView category;
        await using (var scope = env.Factory.Services.CreateAsyncScope())
            category = await scope.ServiceProvider.GetRequiredService<TaxonomyService>().SaveCategoryAsync(null,
                new CategoryInput("كروت شاشة", $"gpu-{Guid.NewGuid():N}"[..12], null, null, null, 0, true, null));
        var item = await env.Retail.ReceiveItemAsync(new ReceiptLine("Typed GPU", "New", 2, 100m, 500m, category.Id));
        await env.Retail.PublishAsync(item);
        var plain = await env.SellableAsync("Plain GPU");
        var order = await env.DeliverAsync(await env.Retail.CheckoutAsync(
            Checkout([StockLine(item.Id), StockLine(plain.Id)], OrderFulfillment.Pickup), Policy()));
        var token = (await env.InvoiceOfAsync(order)).Invoice.AccessToken;

        var document = await Env.GetAsync<InvoiceDocument>(env.Anonymous, $"/api/v1/invoices/{token}");

        var parts = document.Invoice.Lines.SelectMany(line => line.Parts).ToList();
        var typed = Assert.Single(parts, part => part.Code == item.Code);
        Assert.Equal((category.Id, "كروت شاشة"), (typed.CategoryId, typed.Category));
        var untyped = Assert.Single(parts, part => part.Code == plain.Code);
        Assert.Equal(((Guid?)null, (string?)null), (untyped.CategoryId, untyped.Category));
    }

    [Fact]
    public async Task The_warranty_qr_link_disappears_when_the_setting_is_off_and_unknown_tokens_are_not_found()
    {
        await using var env = await Env.StartAsync(fixture);
        await using (var scope = env.Factory.Services.CreateAsyncScope())
        {
            var settings = scope.ServiceProvider.GetRequiredService<StoreSettingsService>();
            await settings.UpdateSectionAsync(new InvoiceSettings { ShowWarrantyQr = false, ShowBackgroundLogo = false, TermsLine = "Keep this invoice" },
                (await settings.GetAdminAsync()).RowVersion, env.StaffId);
        }
        var (_, order) = await env.DeliveredAsync("Branding GPU");
        var anonymous = env.Anonymous;

        var document = await Env.GetAsync<InvoiceDocument>(anonymous, $"/api/v1/invoices/{(await env.InvoiceOfAsync(order)).Invoice.AccessToken}");

        Assert.Null(document.WarrantyLookupUrl);
        Assert.Equal((false, false, "Keep this invoice"), (document.Branding.ShowWarrantyQr, document.Branding.ShowBackgroundLogo, document.Branding.TermsLine));
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/v1/invoices/not-a-real-token")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/invoices/mine")).StatusCode);
    }

    // ---------------------------------------------------------------- builds

    [Fact]
    public async Task A_build_order_is_a_build_invoice_with_the_parts_under_the_build_line()
    {
        await using var env = await Env.StartAsync(fixture);
        var units = await env.ReceiveUnitsAsync(Part("Build CPU", 3000m), Part("Build GPU", 2000m));
        var build = await env.PublishedBuildAsync("Invoice build", 5000m, units);
        var order = await env.DeliverAsync(await env.Retail.PosAsync(Sale([BuildLine(build.Id)])));

        var invoice = (await env.InvoiceOfAsync(order)).Invoice;

        Assert.Equal(InvoiceKind.Build, invoice.Kind);
        var line = Assert.Single(invoice.Lines);
        Assert.Equal((build.Code, build.Code, 5000m), (line.Code, line.BuildCode, line.LineTotal));
        Assert.Equal(2, line.Parts.Count);
        Assert.All(line.Parts, part => Assert.StartsWith("RX-", part.Code));
        Assert.All(line.Parts, part => Assert.Equal(("Build", WarrantyStatus.Active), (part.WarrantyLabel, part.WarrantyStatus)));
    }

    [Fact]
    public async Task A_split_build_sale_lists_each_part_with_the_build_code_and_the_warranty_records_it_too()
    {
        await using var env = await Env.StartAsync(fixture);
        var units = await env.ReceiveUnitsAsync(Part("Split CPU", 3000m), Part("Split GPU", 2000m), Part("Split RAM", 500m));
        var build = await env.PublishedBuildAsync("Split invoice build", 5000m, units, AssemblySellMode.FullOrSplit);
        var split = Sale([BuildLine(build.Id, parts: [new SplitPartRequest(units[0]), new SplitPartRequest(units[1])])]);
        var order = await env.DeliverAsync(await env.Retail.PosAsync(split));

        var invoice = (await env.InvoiceOfAsync(order)).Invoice;

        Assert.Equal(2, invoice.Lines.Count);
        Assert.All(invoice.Lines, line =>
        {
            Assert.Equal(build.Code, line.BuildCode);
            Assert.StartsWith("RX-", line.Code);
            Assert.Single(line.Parts);
        });
        var warranties = (await env.Retail.Warranties.ListAdminAsync(null, null, 1, true, true)).Items;
        Assert.Equal(2, warranties.Count);
        Assert.All(warranties, warranty => Assert.Equal(build.Code, warranty.BuildCode));
    }

    // ---------------------------------------------------------------- cancel and warranty search

    [Fact]
    public async Task Cancelling_a_delivered_order_voids_its_invoice_but_keeps_the_number()
    {
        await using var env = await Env.StartAsync(fixture);
        var (_, order) = await env.DeliveredAsync("Cancelled invoice GPU");

        await env.PostStatusAsync(order.Id, await env.RowVersionAsync(order.Id), OrderStatus.Cancelled);

        var invoice = (await env.InvoiceOfAsync(order)).Invoice;
        Assert.Equal((Number(1), InvoiceStatus.Void), (invoice.Number, invoice.Status));
        var listed = Assert.Single((await Env.GetAsync<PagedResult<InvoiceSummaryView>>(env.Staff, $"{AdminRoot}/invoices")).Items);
        Assert.Equal(InvoiceStatus.Void, listed.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Warranty_lookup_by_invoice_number_returns_the_order_group_with_the_number(bool lowercase)
    {
        await using var env = await Env.StartAsync(fixture);
        var (item, order) = await env.DeliveredAsync("Lookup invoice GPU");
        await env.DeliveredAsync("Other invoice GPU");
        var number = (await env.InvoiceOfAsync(order)).Invoice.Number;

        var result = await Env.GetAsync<WarrantyLookupResult>(env.Anonymous,
            $"/api/v1/warranty/lookup?q={Uri.EscapeDataString(lowercase ? number.ToLowerInvariant() : number)}");

        var group = Assert.Single(result.Groups);
        Assert.Equal((order.Reference, number), (group.OrderReference, group.InvoiceNumber));
        Assert.Equal(item.Code, Assert.Single(group.Items).Code);
    }

    // ---------------------------------------------------------------- lists

    [Fact]
    public async Task A_customer_lists_only_the_invoices_of_their_own_orders()
    {
        await using var env = await Env.StartAsync(fixture);
        var (customer, customerId) = await env.SignedInAsync("buyer");
        var (_, mine) = await env.DeliveredAsync("Mine invoice GPU", userId: customerId);
        await env.DeliveredAsync("Guest invoice GPU");
        await env.DeliveredAsync("Other invoice GPU", userId: Guid.NewGuid());

        var listed = await Env.GetAsync<List<InvoiceSummaryView>>(customer, "/api/v1/invoices/mine");

        var own = Assert.Single(listed);
        Assert.Equal((mine.Reference, Number(1), InvoiceStatus.Issued, mine.Total), (own.OrderReference, own.Number, own.Status, own.Total));
        Assert.Equal((await env.InvoiceOfAsync(mine)).Invoice.AccessToken, own.AccessToken);
    }

    [Fact]
    public async Task Admin_list_searches_by_number_order_reference_and_phone_and_pages()
    {
        await using var env = await Env.StartAsync(fixture);
        var (_, first) = await env.DeliveredAsync("Search GPU 1", phone: "01012345678");
        var (_, second) = await env.DeliveredAsync("Search GPU 2", phone: "01155555555");

        var all = await Env.GetAsync<PagedResult<InvoiceSummaryView>>(env.Staff, $"{AdminRoot}/invoices");
        var byNumber = await Env.GetAsync<PagedResult<InvoiceSummaryView>>(env.Staff, $"{AdminRoot}/invoices?q={Number(1).ToLowerInvariant()}");
        var byReference = await Env.GetAsync<PagedResult<InvoiceSummaryView>>(env.Staff, $"{AdminRoot}/invoices?q={second.Reference}");
        var byPhone = await Env.GetAsync<PagedResult<InvoiceSummaryView>>(env.Staff, $"{AdminRoot}/invoices?q=0101%20234%205678");
        var none = await Env.GetAsync<PagedResult<InvoiceSummaryView>>(env.Staff, $"{AdminRoot}/invoices?q=01999999999");
        var beyond = await Env.GetAsync<PagedResult<InvoiceSummaryView>>(env.Staff, $"{AdminRoot}/invoices?page=9");

        Assert.Equal([Number(2), Number(1)], all.Items.Select(item => item.Number));
        Assert.Equal((1, 50, 2), (all.Page, all.PageSize, all.Total));
        Assert.Equal(first.Reference, Assert.Single(byNumber.Items).OrderReference);
        Assert.Equal(Number(2), Assert.Single(byReference.Items).Number);
        Assert.Equal(Number(1), Assert.Single(byPhone.Items).Number);
        Assert.Equal(0, none.Total);
        Assert.Equal((9, 2, 0), (beyond.Page, beyond.Total, beyond.Items.Count));
    }

    // ---------------------------------------------------------------- staff masking

    [Fact]
    public async Task Staff_without_contact_and_serial_permissions_get_a_masked_invoice()
    {
        await using var env = await Env.StartAsync(fixture);
        var (_, order) = await env.DeliveredAsync("Masked GPU", serial: "SN-MASK-1", fulfillment: OrderFulfillment.Delivery);
        var full = (await env.InvoiceOfAsync(order)).Invoice;
        var (viewer, _) = await env.SignedInAsync("plain-viewer", "Sales.ViewOrders");
        var masked = (await env.InvoiceOfAsync(order, viewer)).Invoice;

        Assert.Equal(BuyerPhone, full.PhoneNumber);
        Assert.Equal("SN-MASK-1", Assert.Single(Assert.Single(full.Lines).Parts).SerialNumber);
        Assert.NotNull(full.Address);
        Assert.Equal("********840", masked.PhoneNumber);
        Assert.Null(masked.Address);
        Assert.Null(Assert.Single(Assert.Single(masked.Lines).Parts).SerialNumber);
        Assert.Equal(full.Number, masked.Number);
    }

    // ---------------------------------------------------------------- email

    [Fact]
    public async Task Delivery_emails_the_invoice_number_and_link_once_when_the_order_has_an_email()
    {
        await using var env = await Env.StartAsync(fixture);
        var (_, order) = await env.DeliveredAsync("Email GPU", email: "buyer@example.test");
        await env.DeliveredAsync("No email GPU");
        var document = await env.InvoiceOfAsync(order);

        await TestSignIn.DeliverOutboxAsync(env.Factory.Services);

        var invoiced = env.Factory.Services.GetRequiredService<InMemoryTestEmailProvider>().OrderUpdates
            .Where(message => message.Stage == OrderEmailMessage.Invoiced).ToList();
        var email = Assert.Single(invoiced);
        Assert.Equal(("buyer@example.test", order.Reference, document.Invoice.Number, document.InvoiceUrl),
            (email.Email, email.Reference, email.InvoiceNumber, email.InvoiceUrl));
    }

    [Fact]
    public async Task Repeating_the_delivered_status_does_not_email_the_invoice_again()
    {
        await using var env = await Env.StartAsync(fixture);
        var (_, order) = await env.DeliveredAsync("Repeat email GPU", email: "repeat@example.test");

        await env.PostStatusAsync(order.Id, await env.RowVersionAsync(order.Id), OrderStatus.Delivered);
        await TestSignIn.DeliverOutboxAsync(env.Factory.Services);

        Assert.Single(env.Factory.Services.GetRequiredService<InMemoryTestEmailProvider>().OrderUpdates,
            message => message.Stage == OrderEmailMessage.Invoiced);
    }

    // ---------------------------------------------------------------- quote

    [Fact]
    public async Task A_quote_of_one_published_build_has_title_usage_part_prices_and_the_build_price_without_customer_data()
    {
        await using var env = await Env.StartAsync(fixture);
        var units = await env.ReceiveUnitsAsync(Part("Quote CPU", 3000m), Part("Quote GPU", 2000m));
        var build = await env.PublishedBuildAsync("Quote build", 4500m, units);

        using var response = await TestSignIn.PostJsonAsync(env.Anonymous, "/api/v1/store/quote", new QuoteRequest([BuildLine(build.Id)]));
        var raw = await response.Content.ReadAsStringAsync();
        var quote = JsonSerializer.Deserialize<QuoteDocument>(raw, JsonSerializerOptions.Web)!.Quote;
        var quoteJson = JsonDocument.Parse(raw).RootElement.GetProperty("quote").GetRawText();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(quote.Title));
        Assert.Equal("Gaming", quote.Usage);
        var line = Assert.Single(quote.Lines);
        Assert.Equal(quote.Title, line.Name);
        Assert.Equal((build.Code, 1, 4500m, 4500m), (line.Code, line.Quantity, line.UnitPrice, line.LineTotal));
        Assert.Null(line.Condition);
        Assert.Equal(2, line.Parts.Count);
        Assert.All(line.Parts, part => Assert.True(part.Price > 0m));
        Assert.Equal(4500m, quote.Total);
        Assert.DoesNotContain("phone", quoteJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("customer", quoteJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", quoteJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_quote_of_a_stock_item_has_its_condition_and_total_and_a_hidden_item_or_build_cannot_be_quoted()
    {
        await using var env = await Env.StartAsync(fixture);
        var item = await env.SellableAsync("Quote item", price: 700m, quantity: 3);
        var hidden = await env.Retail.ReceiveItemAsync(new ReceiptLine("Hidden quote item", "New", 1, 100m, 400m));
        var units = await env.ReceiveUnitsAsync(Part("Draft CPU", 3000m));
        var draftBuild = await env.Retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Draft build", units));
        var anonymous = env.Anonymous;

        var quote = (await (await TestSignIn.PostJsonAsync(anonymous, "/api/v1/store/quote", new QuoteRequest([StockLine(item.Id, 2)])))
            .Content.ReadFromJsonAsync<QuoteDocument>())!.Quote;
        using var hiddenItem = await TestSignIn.PostJsonAsync(anonymous, "/api/v1/store/quote", new QuoteRequest([StockLine(hidden.Id)]));
        using var unpublished = await TestSignIn.PostJsonAsync(anonymous, "/api/v1/store/quote", new QuoteRequest([BuildLine(draftBuild.Id)]));
        using var empty = await TestSignIn.PostJsonAsync(anonymous, "/api/v1/store/quote", new QuoteRequest([]));

        var line = Assert.Single(quote.Lines);
        Assert.Equal((item.Code, "New", 2), (line.Code, line.Condition, line.Quantity));
        Assert.Equal(line.UnitPrice * 2, line.LineTotal);
        Assert.Equal(line.LineTotal, quote.Total);
        Assert.Empty(line.Parts);
        Assert.Null(quote.Title);
        Assert.Null(quote.Usage);
        Assert.Equal(HttpStatusCode.Conflict, hiddenItem.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, unpublished.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }
}

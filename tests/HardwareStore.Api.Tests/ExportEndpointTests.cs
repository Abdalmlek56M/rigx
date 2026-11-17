using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>File export (roadmap 2.10 A): every dataset in CSV, per-dataset permissions, sensitive columns, step-up for customers, audit.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class ExportEndpointTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Export = "/api/v1/admin/system/export";
    private const string Exporter = "exporter@example.test";
    private const string SellerNationalId = "29001010101010";
    private static readonly string[] ViewPermissions =
        ["Sales.ViewOrders", "Inventory.View", "Finance.ViewReports", "Finance.ViewVaults", "Purchasing.View", "Customers.View"];
    private static readonly string[] Datasets = ["orders", "stock-items", "expenses", "purchases", "money-movements", "customers"];

    private static async Task<string> CsvAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("EFBBBF", Convert.ToHexString(bytes, 0, 3));
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    private static string[] Lines(string csv) => csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    private static async Task<HttpResponseMessage> ExportAsync(HttpClient client, string dataset, string query = "?format=csv",
        IDictionary<string, string>? headers = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Export}/{dataset}{query}");
        foreach (var header in headers ?? new Dictionary<string, string>()) request.Headers.Add(header.Key, header.Value);
        return await client.SendAsync(request);
    }

    /// <summary>One order, one stock item, one expense, one purchase (seller with a national ID) and one customer.</summary>
    private static async Task SeedAsync(OwnerBootstrapDatabase database, IServiceProvider services)
    {
        var retail = database.CreateRetailServices();
        await retail.FundAsync(1000m);
        var item = await retail.PublishedItemAsync("Export GPU", 2, 1000m, cost: 400m);
        await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.StockLine(item.Id)], OrderFulfillment.Pickup, phone: "01099998888"), RetailSales.Policy());
        await retail.Expenses.CreateExpenseAsync(new ExpenseCreateRequest(null, 50m, "إيجار", ExpenseClassification.GeneralOverhead, "this month", null, null), Guid.NewGuid());
        await retail.Expenses.CreateExpenseAsync(new ExpenseCreateRequest(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-45)), 70m, "إيجار", ExpenseClassification.GeneralOverhead, "long ago", null, null), Guid.NewGuid());
        using var scope = services.CreateScope();
        var seller = await scope.ServiceProvider.GetRequiredService<SellerService>()
            .CreateAsync(new SellerRequest("Person", "Hassan", "01012345678", SellerNationalId, null, null, null, null));
        await scope.ServiceProvider.GetRequiredService<PurchaseService>().CreateAsync(new PurchaseRequest(seller.Id, PurchaseKind.Parts, false, null, null,
            null, SellerPaymentMethod.Cash, 2000m, 0m, null, [new PurchaseLineRequest("RTX 3060", "Used", 1, 2000m, 3000m, SerialNumber: "SN-1")]), Guid.NewGuid());
        await CustomerAdminTests.CustomerAsync(database, "alice", "01012345678");
    }

    [Fact]
    public async Task Each_dataset_downloads_as_csv_with_rows_a_dated_file_name_and_no_caching()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Exporter, ViewPermissions);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        await SeedAsync(database, factory.Services);
        using var client = await AdminApiFactory.SignedInAsync(factory, Exporter);

        foreach (var dataset in Datasets)
        {
            using var response = await ExportAsync(client, dataset, headers: dataset == "customers" ? await AdminApiFactory.StepUpAsync(client, factory, Exporter) : null);
            var csv = Lines(await CsvAsync(response));
            Assert.True(csv.Length >= 2, $"{dataset} exported no rows");
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
            Assert.Matches($@"^rigx-{dataset}-\d{{4}}-\d{{2}}-\d{{2}}-\d{{4}}-\d{{2}}-\d{{2}}\.csv$", response.Content.Headers.ContentDisposition!.FileName);
        }
        Assert.Contains("Export GPU", await CsvAsync(await ExportAsync(client, "stock-items")));
        Assert.Contains("Hassan", await CsvAsync(await ExportAsync(client, "purchases")));
        var expenses = await CsvAsync(await ExportAsync(client, "expenses"));
        Assert.Contains("this month", expenses);
        Assert.DoesNotContain("long ago", expenses);
        var wider = $"?format=csv&from={DateTime.UtcNow.AddDays(-60):yyyy-MM-dd}";
        Assert.Contains("long ago", await CsvAsync(await ExportAsync(client, "expenses", wider)));
    }

    [Fact]
    public async Task Excel_and_xml_files_open_and_bad_requests_are_rejected()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Exporter, ViewPermissions);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        await SeedAsync(database, factory.Services);
        using var client = await AdminApiFactory.SignedInAsync(factory, Exporter);

        using var xlsx = await ExportAsync(client, "stock-items", "?format=xlsx");
        Assert.Equal(HttpStatusCode.OK, xlsx.StatusCode);
        Assert.EndsWith(".xlsx", xlsx.Content.Headers.ContentDisposition!.FileName);
        using var package = new ZipArchive(new MemoryStream(await xlsx.Content.ReadAsByteArrayAsync()));
        Assert.Contains("Export GPU", new StreamReader(package.GetEntry("xl/worksheets/sheet1.xml")!.Open()).ReadToEnd());

        using var xml = await ExportAsync(client, "stock-items", "?format=xml");
        var root = XDocument.Parse(await xml.Content.ReadAsStringAsync()).Root!;
        Assert.Equal("stock-items", root.Attribute("dataset")!.Value);
        Assert.Contains(root.Descendants("col"), col => col.Value == "Export GPU");

        Assert.Equal(HttpStatusCode.BadRequest, (await ExportAsync(client, "orders", "?format=pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ExportAsync(client, "orders", "?format=csv&from=2026-11-02&to=2026-11-01")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ExportAsync(client, "secrets")).StatusCode);
    }

    [Fact]
    public async Task A_dataset_needs_the_permission_of_its_own_list_and_cost_and_contact_columns_need_theirs()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, "orders-only@example.test", "Sales.ViewOrders");
        await AdminApiFactory.StaffAsync(database, Exporter, ViewPermissions);
        await AdminApiFactory.StaffAsync(database, "boss@example.test", [.. ViewPermissions, "Inventory.ViewCosts", "Sales.ViewCustomerContact"]);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        await SeedAsync(database, factory.Services);
        using var ordersOnly = await AdminApiFactory.SignedInAsync(factory, "orders-only@example.test");
        using var plain = await AdminApiFactory.SignedInAsync(factory, Exporter);
        using var boss = await AdminApiFactory.SignedInAsync(factory, "boss@example.test");

        foreach (var dataset in Datasets.Where(dataset => dataset != "orders"))
            Assert.Equal(HttpStatusCode.Forbidden, (await ExportAsync(ordersOnly, dataset)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ExportAsync(ordersOnly, "orders")).StatusCode);

        var plainItems = Lines(await CsvAsync(await ExportAsync(plain, "stock-items")));
        var bossItems = Lines(await CsvAsync(await ExportAsync(boss, "stock-items")));
        Assert.DoesNotContain("cost", plainItems[0]);
        Assert.Contains("stock cost", bossItems[0]);
        Assert.EndsWith(",800.00,1200.00", bossItems[1]);
        var plainOrders = await CsvAsync(await ExportAsync(plain, "orders"));
        var bossOrders = await CsvAsync(await ExportAsync(boss, "orders"));
        Assert.DoesNotContain("phone", plainOrders);
        Assert.DoesNotContain("01099998888", plainOrders);
        Assert.Contains("01099998888", bossOrders);
        Assert.DoesNotContain(SellerNationalId, await CsvAsync(await ExportAsync(boss, "purchases")));
    }

    [Fact]
    public async Task The_customers_export_needs_a_fresh_code_and_every_export_is_audited()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Exporter, ViewPermissions);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        await SeedAsync(database, factory.Services);
        using var client = await AdminApiFactory.SignedInAsync(factory, Exporter);

        Assert.Equal(HttpStatusCode.Forbidden, (await ExportAsync(client, "customers")).StatusCode);
        var customers = Lines(await CsvAsync(await ExportAsync(client, "customers", headers: await AdminApiFactory.StepUpAsync(client, factory, Exporter))));
        Assert.Contains("alice@example.test", customers[1]);
        Assert.Equal(HttpStatusCode.OK, (await ExportAsync(client, "expenses", "?format=xml")).StatusCode);

        await using var audit = database.CreateAuditContext();
        var events = await audit.SecurityAuditEvents.AsNoTracking().Where(item => item.EventType == ExportRunner.ExportedEvent).ToListAsync();
        Assert.Equal(2, events.Count);
        var metadata = events.Select(item => JsonDocument.Parse(item.SafeMetadata).RootElement).ToDictionary(item => item.GetProperty("dataset").GetString()!);
        Assert.Equal(("csv", 1), (metadata["customers"].GetProperty("format").GetString(), metadata["customers"].GetProperty("rows").GetInt32()));
        Assert.Equal("xml", metadata["expenses"].GetProperty("format").GetString());
        Assert.Equal(HardwareStore.Common.Domain.BusinessDay.Of(DateTimeOffset.UtcNow, RetailSales.BusinessZone).AddDays(-30).ToString("yyyy-MM-dd"), metadata["expenses"].GetProperty("from").GetString());
        Assert.All(events, item => Assert.Equal("User", item.ActorKind));
    }
}

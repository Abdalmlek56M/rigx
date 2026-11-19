using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Export;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Opening-stock import from Excel (roadmap 4.8): template, dry run, all-or-nothing import, the same file twice, permissions.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class StockImportTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Url = "/api/v1/admin/retail/stock-import";
    private const string Importer = "importer@example.test";

    private static readonly string[] Header = [.. StockImportRules.Columns.Select(column => column.Arabic)];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>A row of text cells in column order; blank cells are left out of the sheet.</summary>
    private static IReadOnlyList<ExportCell> Row(string category, string model, string condition, string quantity, string cost, string price,
        string withBoard = "", string alone = "", string warranty = "", string box = "", string serial = "", string note = "", string hidden = "") =>
        [.. new[] { category, model, condition, quantity, cost, price, withBoard, alone, warranty, box, serial, note, hidden }.Select(ExportCell.Text)];

    private static byte[] Workbook(params IReadOnlyList<ExportCell>[] rows) => XlsxExportWriter.Write(new ExportTable(Header, rows));

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] file, bool? dryRun, string fileName = "stock.xlsx")
    {
        using var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(file);
        part.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(part, "file", fileName);
        var query = dryRun is { } flag ? $"?dryRun={flag.ToString().ToLowerInvariant()}" : string.Empty;
        using var request = new HttpRequestMessage(HttpMethod.Post, Url + query) { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", await TestSignIn.CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private static async Task<StockImportResult> ResultAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StockImportResult>(Json))!;
    }

    private sealed record ImportEnvironment(OwnerBootstrapDatabase Database, IdentityLoginApiFactory Factory, HttpClient Client) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Factory.DisposeAsync();
        }

        public async Task<List<StockItem>> ItemsAsync()
        {
            await using var db = await Factory.Services.GetRequiredService<IDbContextFactory<RetailDbContext>>().CreateDbContextAsync();
            return await db.StockItems.AsNoTracking().Include(item => item.Units).OrderBy(item => item.Code.Length).ThenBy(item => item.Code).ToListAsync();
        }

        public async Task<int> ImportRecordsAsync()
        {
            await using var db = await Factory.Services.GetRequiredService<IDbContextFactory<RetailDbContext>>().CreateDbContextAsync();
            return await db.StockImports.CountAsync();
        }
    }

    private async Task<ImportEnvironment> StartAsync(params string[] permissions)
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Importer, permissions);
        var factory = AdminApiFactory.Create(database.ConnectionString);
        using (var scope = factory.Services.CreateScope())
        {
            var taxonomy = scope.ServiceProvider.GetRequiredService<TaxonomyService>();
            await taxonomy.SaveCategoryAsync(null, new CategoryInput("كروت الشاشة", "gpus", null, null, null, 0, true, null, "GPU"));
            await taxonomy.SaveCategoryAsync(null, new CategoryInput("Memory", "memory", null, null, null, 1, true, null, "RAM"));
        }
        return new ImportEnvironment(database, factory, await AdminApiFactory.SignedInAsync(factory, Importer));
    }

    [Fact]
    public async Task The_template_downloads_and_importing_it_unchanged_reads_back_the_example_row()
    {
        await using var env = await StartAsync("Purchasing.ReceiveGoods");

        using var template = await env.Client.GetAsync($"{Url}/template");

        Assert.Equal(HttpStatusCode.OK, template.StatusCode);
        Assert.EndsWith(".xlsx", template.Content.Headers.ContentDisposition!.FileName);
        var bytes = await template.Content.ReadAsByteArrayAsync();
        Assert.Equal(Header, XlsxImportReader.ReadFirstSheet(bytes)[0]);
        var result = await ResultAsync(await UploadAsync(env.Client, bytes, dryRun: true));
        var row = Assert.Single(result.Rows);
        Assert.Empty(row.Errors);
        Assert.Equal((2, "Used", 1), (row.Row, row.Condition, row.ItemsToCreate));
        Assert.Equal((1, 0, 1, false), (result.ValidRows, result.ErrorRows, result.ItemsToCreate, result.Imported));
    }

    [Fact]
    public async Task A_dry_run_reports_each_row_and_writes_nothing()
    {
        await using var env = await StartAsync("Purchasing.ReceiveGoods");
        var file = Workbook(Row("GPU", "RTX 3060", "مستعمل", "2", "8000", "9500"), Row("Nope", "", "جديد", "1", "1", ""), Row("RAM", "DDR4", "new", "3", "200", "300"));

        var result = await ResultAsync(await UploadAsync(env.Client, file, dryRun: null));

        Assert.False(result.Imported);
        Assert.Equal((2, 1, 3), (result.ValidRows, result.ErrorRows, result.ItemsToCreate));
        Assert.Equal([2, 3, 4], result.Rows.Select(row => row.Row));
        Assert.Equal([2, 0, 1], result.Rows.Select(row => row.ItemsToCreate));
        Assert.Contains("القسم \"Nope\" مش موجود", result.Rows[1].Errors);
        Assert.Contains("الموديل مطلوب", result.Rows[1].Errors);
        Assert.Empty(await env.ItemsAsync());
        Assert.Equal(0, await env.ImportRecordsAsync());
    }

    [Fact]
    public async Task A_valid_import_creates_the_items_with_cost_prices_warranty_and_box()
    {
        await using var env = await StartAsync("Purchasing.ReceiveGoods");
        var file = Workbook(
            Row("GPU", "RTX 3060", "مستعمل", "2", "8,000", "9500", "10500", "10000", "12", "نعم", note: "من فلان", hidden: "نعم"),
            Row("RAM", "DDR4 16GB", "جديد", "٣", "200", "300"));

        var result = await ResultAsync(await UploadAsync(env.Client, file, dryRun: false));

        Assert.True(result.Imported);
        Assert.Equal(3, result.ItemsToCreate);
        Assert.Equal(["RTX 3060", "RTX 3060", "DDR4 16GB"], result.Items!.Select(item => item.ModelName));
        var items = await env.ItemsAsync();
        Assert.Equal(result.Items!.Select(item => item.Code), items.Select(item => item.Code));
        Assert.Equal(result.Items!.Select(item => item.Slug), items.Select(item => item.Slug));
        var used = items.Take(2).ToList();
        Assert.All(used, item =>
        {
            Assert.Equal(("Used", 9500m, 1000m, 500m), (item.Condition, item.Price, item.WithBoardExtra, item.AloneExtra));
            Assert.Equal(("سنة", 12, true, true, "من فلان"), (item.WarrantyLabel, item.WarrantyMonths, item.HasBox, item.IsHidden, item.InternalNote));
            var unit = Assert.Single(item.Units);
            Assert.Equal((8000m, "Import", UnitStatus.Available), (unit.AcquisitionCost, unit.SourceType, unit.Status));
            Assert.Null(unit.ReceiptId);
        });
        var lot = items[2];
        Assert.Equal(("New", 300m, false), (lot.Condition, lot.Price, lot.HasBox));
        Assert.Equal(3, lot.Units.Count);
        Assert.All(lot.Units, unit => Assert.Equal(200m, unit.AcquisitionCost));
        Assert.Equal(1, await env.ImportRecordsAsync());
    }

    [Fact]
    public async Task One_bad_row_imports_nothing_and_a_real_import_with_errors_is_a_400_naming_the_row()
    {
        await using var env = await StartAsync("Purchasing.ReceiveGoods");
        var file = Workbook(Row("GPU", "Good GPU", "جديد", "1", "10", "20"), Row("GPU", "Bad GPU", "جديد", "1", "10", "20", withBoard: "5"));

        using var response = await UploadAsync(env.Client, file, dryRun: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("السعر مع بوردة", problem.GetProperty("errors").GetProperty("الصف 3")[0].GetString());
        Assert.Empty(await env.ItemsAsync());
        Assert.Equal(0, await env.ImportRecordsAsync());
    }

    [Fact]
    public async Task The_same_file_twice_is_a_409_and_creates_nothing_more()
    {
        await using var env = await StartAsync("Purchasing.ReceiveGoods");
        var file = Workbook(Row("GPU", "RTX 3060", "جديد", "1", "10", "20"));
        Assert.True((await ResultAsync(await UploadAsync(env.Client, file, dryRun: false))).Imported);

        using var second = await UploadAsync(env.Client, file, dryRun: false);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("الملف ده اتستورد قبل كده", (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        Assert.Single(await env.ItemsAsync());
        Assert.Equal(1, await env.ImportRecordsAsync());
    }

    [Fact]
    public async Task Importing_needs_the_receive_goods_permission()
    {
        await using var env = await StartAsync("Inventory.View");
        var file = Workbook(Row("GPU", "RTX 3060", "جديد", "1", "10", "20"));

        Assert.Equal(HttpStatusCode.Forbidden, (await env.Client.GetAsync($"{Url}/template")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadAsync(env.Client, file, dryRun: true)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadAsync(env.Client, file, dryRun: false)).StatusCode);
    }

    [Fact]
    public async Task A_file_that_is_not_a_small_xlsx_workbook_is_a_400()
    {
        await using var env = await StartAsync("Purchasing.ReceiveGoods");
        var valid = Workbook(Row("GPU", "RTX 3060", "جديد", "1", "10", "20"));

        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(env.Client, "a,b,c"u8.ToArray(), dryRun: true, "stock.csv")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(env.Client, "not a zip"u8.ToArray(), dryRun: true)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(env.Client, new byte[StockImportRules.MaxFileBytes + 1], dryRun: true)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(env.Client, valid, dryRun: true, "stock.xls")).StatusCode);
        var tooManyRows = Workbook([.. Enumerable.Range(0, StockImportRules.MaxDataRows + 1).Select(index => Row("GPU", $"Model {index}", "جديد", "1", "1", "2"))]);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(env.Client, tooManyRows, dryRun: true)).StatusCode);
        var noHeader = XlsxExportWriter.Write(new ExportTable(["a", "b"], [[ExportCell.Text("x"), ExportCell.Text("y")]]));
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(env.Client, noHeader, dryRun: true)).StatusCode);
    }
}

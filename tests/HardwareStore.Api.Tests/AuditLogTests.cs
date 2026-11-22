using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Business audit log (roadmap 2.11): capture in the saving transaction, append-only storage, query, export and restore of earlier values.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class AuditLogTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Api = "/api/v1/admin/audit";
    private const string Items = "/api/v1/admin/retail/stock-items";
    private const string Editor = "audit-editor@example.test";
    private const string Owner = "audit-owner@example.test";
    private const string Viewer = "audit-viewer@example.test";
    private const string Chrome = "Mozilla/5.0 (Windows NT 10.0) Chrome/130.0 Safari/537.36";
    private static readonly string[] EditorPermissions = ["Inventory.View", "Inventory.StockAdjust", "System.ViewAuditLogs"];

    private sealed record Change(string Field, JsonElement? Before, JsonElement? After);

    private sealed record Entry(Guid Id, DateTimeOffset OccurredAtUtc, string ActorKind, Guid? ActorUserId, string? ActorName, string? Device,
        string? CorrelationId, string Module, string Entity, string EntityId, string Action, List<Change> Changes, Guid? RestoredFromEntryId, bool CanRestore, Dictionary<Guid, string>? LinkedNames);

    private sealed record EntryPage(List<Entry> Items, int Page, int PageSize, int Total);

    private sealed record MonthRow(string Month, int Count);

    private sealed record DayRow(string Date, int Count, List<string> ActorNames, List<string> OtherActors);

    private sealed record Filters(List<string> Modules, List<string> Entities, List<string> Actions);

    private static HttpClient WithAgent(HttpClient client)
    {
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Chrome);
        return client;
    }

    private static async Task<List<AuditEntry>> EntriesAsync(OwnerBootstrapDatabase database, string? entity = null)
    {
        await using var audit = database.CreateAuditContext();
        return await audit.AuditEntries.AsNoTracking().Where(entry => entity == null || entry.Entity == entity).OrderBy(entry => entry.OccurredAtUtc).ToListAsync();
    }

    private static async Task<StockItemAdminView> SeedItemAsync(OwnerBootstrapDatabase database, string model = "Audit GPU") =>
        await database.CreateRetailServices().PublishedItemAsync(model, 2, 1000m, cost: 400m);

    private static async Task<StockItemAdminView> ItemAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<StockItemAdminView>($"{Items}/{id}"))!;

    private static Task<HttpResponseMessage> SetPriceAsync(HttpClient client, StockItemAdminView item, decimal price) =>
        TestSignIn.SendJsonAsync(client, HttpMethod.Put, $"{Items}/{item.Id}", new
        {
            modelName = item.ModelName, categoryId = item.CategoryId, productId = item.ProductId, price, withBoardExtra = item.WithBoardExtra,
            aloneExtra = item.AloneExtra, warrantyLabel = (string?)null, hasBox = item.HasBox, isHidden = item.IsHidden, internalNote = item.InternalNote,
            specs = item.Specs, rowVersion = item.RowVersion
        });

    private static async Task<StockItemAdminView> EditPriceAsync(HttpClient client, Guid id, decimal price)
    {
        using var response = await SetPriceAsync(client, await ItemAsync(client, id), price);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ItemAsync(client, id);
    }

    private static async Task<Guid> InsertEntryAsync(OwnerBootstrapDatabase database, string module, string entity, string entityId, string action,
        string changes, DateTimeOffset at, Guid? actor = null)
    {
        var id = Guid.CreateVersion7();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {AuditTable.Name} (id, occurred_at_utc, actor_kind, actor_user_id, module, entity, entity_id, action, changes)
            VALUES (@id, @at, @kind, @actor, @module, @entity, @entityId, @action, CAST(@changes AS jsonb))
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("at", at.ToUniversalTime());
        command.Parameters.AddWithValue("kind", actor is null ? "System" : "User");
        command.Parameters.AddWithValue("actor", (object?)actor ?? DBNull.Value);
        command.Parameters.AddWithValue("module", module);
        command.Parameters.AddWithValue("entity", entity);
        command.Parameters.AddWithValue("entityId", entityId);
        command.Parameters.AddWithValue("action", action);
        command.Parameters.AddWithValue("changes", changes);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    private static async Task<(OwnerBootstrapDatabase Database, IdentityLoginApiFactory Factory, HttpClient Client, Guid EditorId)>
        StartAsync(OwnerBootstrapPostgreSqlFixture fixture)
    {
        var database = await fixture.CreateDatabaseAsync();
        var editorId = await AdminApiFactory.StaffAsync(database, Editor, EditorPermissions);
        var factory = AdminApiFactory.Create(database.ConnectionString);
        return (database, factory, WithAgent(await AdminApiFactory.SignedInAsync(factory, Editor)), editorId);
    }

    [Fact]
    public async Task Editing_a_stock_item_price_logs_one_entry_with_before_after_the_actor_and_the_device()
    {
        var (database, factory, client, editorId) = await StartAsync(fixture);
        await using var _ = factory;
        using var __ = client;
        var item = await SeedItemAsync(database);
        Assert.Empty(await EntriesAsync(database, "stock_items"));

        await EditPriceAsync(client, item.Id, 1250m);

        var entry = Assert.Single(await EntriesAsync(database, "stock_items"));
        Assert.Equal(("Retail", item.Id.ToString(), "Updated", "User", editorId), (entry.Module, entry.EntityId, entry.Action, entry.ActorKind, entry.ActorUserId));
        Assert.Equal(Chrome, entry.ActorUserAgent);
        Assert.False(string.IsNullOrEmpty(entry.CorrelationId));
        var change = Assert.Single(AuditChanges.Parse(entry.Changes));
        Assert.Equal("Price", change.Field);
        Assert.Equal(1000m, change.Before!.GetValue<decimal>());
        Assert.Equal(1250m, change.After!.GetValue<decimal>());
    }

    [Fact]
    public async Task Excluded_kinds_such_as_sessions_leave_no_entry_but_signing_in_still_works()
    {
        var (database, factory, client, _) = await StartAsync(fixture);
        await using var _ = factory;
        using var __ = client;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Items}?pageSize=1")).StatusCode);

        var entities = (await EntriesAsync(database)).Select(entry => entry.Entity).ToList();
        Assert.DoesNotContain("auth_sessions", entities);
        Assert.DoesNotContain("email_codes", entities);
        Assert.DoesNotContain("identity_email_outbox", entities);
    }

    [Fact]
    public async Task The_list_filters_pages_and_defaults_to_the_last_thirty_days_newest_first()
    {
        var (database, factory, client, editorId) = await StartAsync(fixture);
        await using var _ = factory;
        using var __ = client;
        var now = DateTimeOffset.UtcNow;
        var recent = await InsertEntryAsync(database, "Retail", "stock_items", "a", "Updated", """[{"field":"Price","before":1,"after":2}]""", now.AddHours(-1), editorId);
        var older = await InsertEntryAsync(database, "Catalog", "categories", "b", "Created", "[]", now.AddDays(-3));
        await InsertEntryAsync(database, "Retail", "stock_items", "a", "Updated", "[]", now.AddDays(-45));

        var all = (await client.GetFromJsonAsync<EntryPage>($"{Api}/entries"))!;
        Assert.Equal([recent, older], all.Items.Select(entry => entry.Id));
        Assert.Equal(2, all.Total);
        var first = all.Items[0];
        Assert.Equal((editorId, "User", "Retail", "stock_items", "Updated", true), (first.ActorUserId, first.ActorKind, first.Module, first.Entity, first.Action, first.CanRestore));
        Assert.False(string.IsNullOrWhiteSpace(first.ActorName));

        Assert.Equal([older], (await client.GetFromJsonAsync<EntryPage>($"{Api}/entries?module=Catalog"))!.Items.Select(entry => entry.Id));
        Assert.Equal([recent], (await client.GetFromJsonAsync<EntryPage>($"{Api}/entries?entity=stock_items&entityId=a"))!.Items.Select(entry => entry.Id));
        Assert.Equal([recent], (await client.GetFromJsonAsync<EntryPage>($"{Api}/entries?actorUserId={editorId}"))!.Items.Select(entry => entry.Id));
        Assert.Equal([older], (await client.GetFromJsonAsync<EntryPage>($"{Api}/entries?action=Created"))!.Items.Select(entry => entry.Id));
        var secondPage = (await client.GetFromJsonAsync<EntryPage>($"{Api}/entries?pageSize=1&page=2"))!;
        Assert.Equal([older], secondPage.Items.Select(entry => entry.Id));
        Assert.Equal(2, secondPage.Total);
        var today = BusinessDay.Of(now, RetailSales.BusinessZone);
        var wide = (await client.GetFromJsonAsync<EntryPage>($"{Api}/entries?from={today.AddDays(-60):yyyy-MM-dd}&to={today:yyyy-MM-dd}"))!;
        Assert.Equal(3, wide.Total);
        Assert.Equal(older, (await client.GetFromJsonAsync<Entry>($"{Api}/entries/{older}"))!.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Api}/entries/{Guid.NewGuid()}")).StatusCode);

        var filters = (await client.GetFromJsonAsync<Filters>($"{Api}/filters"))!;
        Assert.Equal(["Catalog", "Retail"], filters.Modules);
        Assert.Equal(["categories", "stock_items"], filters.Entities);
        Assert.Equal(AuditAction.All, filters.Actions);
    }

    [Fact]
    public async Task An_entry_names_the_users_it_points_to_and_leaves_unknown_ids_out()
    {
        var (database, factory, client, editorId) = await StartAsync(fixture);
        await using var _ = factory;
        using var __ = client;
        var stranger = Guid.NewGuid();
        var id = await InsertEntryAsync(database, "Identity", "user_roles", "x", "Created",
            $$"""[{"field":"AccountId","before":null,"after":"{{editorId}}"},{"field":"Other","before":null,"after":"{{stranger}}"}]""", DateTimeOffset.UtcNow);

        var entry = (await client.GetFromJsonAsync<Entry>($"{Api}/entries/{id}"))!;

        Assert.False(string.IsNullOrWhiteSpace(entry.LinkedNames![editorId]));
        Assert.DoesNotContain(stranger, entry.LinkedNames.Keys);
        var listed = (await client.GetFromJsonAsync<EntryPage>($"{Api}/entries"))!.Items.Single(item => item.Id == id);
        Assert.Empty(listed.LinkedNames!);
    }

    [Fact]
    public async Task The_log_is_browsed_by_month_then_by_day_with_counts_and_the_people_behind_them()
    {
        var (database, factory, client, editorId) = await StartAsync(fixture);
        await using var _ = factory;
        using var __ = client;
        var now = DateTimeOffset.UtcNow;
        var today = BusinessDay.Of(now, RetailSales.BusinessZone);
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var longAgo = thisMonth.AddMonths(-3).AddDays(9);
        var longAgoNoon = new DateTimeOffset(longAgo.ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(2)).ToUniversalTime();
        await InsertEntryAsync(database, "Retail", "stock_items", "a", "Updated", "[]", now, editorId);
        await InsertEntryAsync(database, "Retail", "stock_items", "b", "Updated", "[]", now, editorId);
        await InsertEntryAsync(database, "Catalog", "categories", "c", "Created", "[]", now);
        await InsertEntryAsync(database, "Catalog", "categories", "d", "Created", "[]", longAgoNoon);

        var months = (await client.GetFromJsonAsync<List<MonthRow>>($"{Api}/months"))!;
        Assert.Equal([$"{thisMonth:yyyy-MM}", $"{thisMonth.AddMonths(-3):yyyy-MM}"], months.Select(month => month.Month));
        Assert.Equal([3, 1], months.Select(month => month.Count));
        Assert.Equal([1, 1], (await client.GetFromJsonAsync<List<MonthRow>>($"{Api}/months?module=Catalog"))!.Select(month => month.Count));
        Assert.Equal([2], (await client.GetFromJsonAsync<List<MonthRow>>($"{Api}/months?entity=stock_items"))!.Select(month => month.Count));

        var days = (await client.GetFromJsonAsync<List<DayRow>>($"{Api}/days?month={thisMonth:yyyy-MM}"))!;
        var latest = Assert.Single(days, day => day.Date == today.ToString("yyyy-MM-dd"));
        Assert.Equal(3, latest.Count);
        Assert.Single(latest.ActorNames);
        Assert.Equal(["System"], latest.OtherActors);
        Assert.Equal(1, (await client.GetFromJsonAsync<List<DayRow>>($"{Api}/days?month={thisMonth.AddMonths(-3):yyyy-MM}"))!.Single().Count);
        Assert.Empty((await client.GetFromJsonAsync<List<DayRow>>($"{Api}/days?month={thisMonth.AddMonths(-1):yyyy-MM}"))!);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Api}/days")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Api}/days?month=2026-13")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Api}/months?action=Nonsense")).StatusCode);
    }

    [Theory]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    [InlineData("action=Nonsense")]
    [InlineData("from=2026-11-10&to=2026-11-01")]
    public async Task Invalid_filters_are_rejected_with_400(string query)
    {
        var (_, factory, client, _) = await StartAsync(fixture);
        await using var _ = factory;
        using var __ = client;

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Api}/entries?{query}")).StatusCode);
    }

    [Fact]
    public async Task The_log_needs_the_audit_view_permission_and_exports_as_csv_in_the_same_range()
    {
        var database = await fixture.CreateDatabaseAsync();
        var editorId = await AdminApiFactory.StaffAsync(database, Editor, EditorPermissions);
        await AdminApiFactory.StaffAsync(database, Viewer, "Inventory.View");
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var client = await AdminApiFactory.SignedInAsync(factory, Editor);
        using var outsider = await AdminApiFactory.SignedInAsync(factory, Viewer);
        await InsertEntryAsync(database, "Retail", "stock_items", "csv-row", "Updated", """[{"field":"Price","before":1,"after":2}]""", DateTimeOffset.UtcNow, editorId);
        await InsertEntryAsync(database, "Retail", "stock_items", "too-old", "Updated", "[]", DateTimeOffset.UtcNow.AddDays(-60));

        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"{Api}/entries")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync("/api/v1/admin/system/export/audit")).StatusCode);
        using var csv = await client.GetAsync("/api/v1/admin/system/export/audit?format=csv");

        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        var text = await csv.Content.ReadAsStringAsync();
        Assert.Contains("csv-row", text);
        Assert.DoesNotContain("too-old", text);
        Assert.Contains("Price", text);
    }

    [Fact]
    public async Task Restoring_an_edit_puts_the_old_price_back_and_logs_the_restore_linked_to_its_entry()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Editor, EditorPermissions);
        var ownerId = await AdminApiFactory.StaffAsync(database, Owner, [.. EditorPermissions, "Audit.Restore"]);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var editor = WithAgent(await AdminApiFactory.SignedInAsync(factory, Editor));
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        var item = await SeedItemAsync(database);
        var versionBefore = (await ItemAsync(owner, item.Id)).RowVersion;
        await EditPriceAsync(editor, item.Id, 1250m);
        var edit = Assert.Single(await EntriesAsync(database, "stock_items"));
        var restoreUrl = $"{Api}/entries/{edit.Id}/restore";

        Assert.Equal(HttpStatusCode.Forbidden, (await TestSignIn.PostJsonAsync(editor, restoreUrl, new { }, await AdminApiFactory.StepUpAsync(editor, factory, Editor))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await TestSignIn.PostJsonAsync(owner, restoreUrl, new { })).StatusCode);
        Assert.Equal(1250m, (await ItemAsync(owner, item.Id)).Price);

        var restored = await TestSignIn.PostJsonAsync(owner, restoreUrl, new { }, await AdminApiFactory.StepUpAsync(owner, factory, Owner));

        Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
        var after = await ItemAsync(owner, item.Id);
        Assert.Equal(1000m, after.Price);
        Assert.Equal(versionBefore + 2, after.RowVersion);
        var entries = await EntriesAsync(database, "stock_items");
        var log = Assert.Single(entries, entry => entry.Action == AuditAction.ValuesRestored);
        Assert.Equal((edit.Id, ownerId), (log.RestoredFromEntryId, log.ActorUserId));
        var change = Assert.Single(AuditChanges.Parse(log.Changes));
        Assert.Equal((1250m, 1000m), (change.Before!.GetValue<decimal>(), change.After!.GetValue<decimal>()));
        var listed = (await owner.GetFromJsonAsync<EntryPage>($"{Api}/entries?action=ValuesRestored"))!.Items.Single();
        Assert.Equal((edit.Id, false), (listed.RestoredFromEntryId, listed.CanRestore));
    }

    [Fact]
    public async Task A_restore_is_refused_with_409_when_the_record_changed_since_the_entry()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, [.. EditorPermissions, "Audit.Restore"]);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        var item = await SeedItemAsync(database);
        await EditPriceAsync(owner, item.Id, 1250m);
        await EditPriceAsync(owner, item.Id, 1300m);
        var entries = await EntriesAsync(database, "stock_items");

        using var stale = await TestSignIn.PostJsonAsync(owner, $"{Api}/entries/{entries[0].Id}/restore", new { }, await AdminApiFactory.StepUpAsync(owner, factory, Owner));

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains("Price", await stale.Content.ReadAsStringAsync());
        Assert.Equal(1300m, (await ItemAsync(owner, item.Id)).Price);
        Assert.Equal(HttpStatusCode.NoContent,
            (await TestSignIn.PostJsonAsync(owner, $"{Api}/entries/{entries[1].Id}/restore", new { }, await AdminApiFactory.StepUpAsync(owner, factory, Owner))).StatusCode);
        Assert.Equal(1250m, (await ItemAsync(owner, item.Id)).Price);
    }

    [Fact]
    public async Task Only_edits_of_allowlisted_kinds_can_be_restored()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, [.. EditorPermissions, "Audit.Restore"]);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        var item = await SeedItemAsync(database);
        var now = DateTimeOffset.UtcNow;
        var order = await InsertEntryAsync(database, "Retail", "store_orders", Guid.NewGuid().ToString(), "Updated", """[{"field":"Status","before":"A","after":"B"}]""", now);
        var created = await InsertEntryAsync(database, "Retail", "stock_items", item.Id.ToString(), "Created", """[{"field":"Price","before":null,"after":5}]""", now);
        var technical = await InsertEntryAsync(database, "Retail", "stock_items", item.Id.ToString(), "Updated", """[{"field":"Code","before":"A","after":"B"}]""", now);
        var gone = await InsertEntryAsync(database, "Retail", "stock_items", Guid.NewGuid().ToString(), "Updated", """[{"field":"Price","before":1,"after":2}]""", now);

        foreach (var id in new[] { order, created, technical })
            Assert.Equal(HttpStatusCode.Conflict, (await TestSignIn.PostJsonAsync(owner, $"{Api}/entries/{id}/restore", new { }, await AdminApiFactory.StepUpAsync(owner, factory, Owner))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await TestSignIn.PostJsonAsync(owner, $"{Api}/entries/{gone}/restore", new { }, await AdminApiFactory.StepUpAsync(owner, factory, Owner))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await TestSignIn.PostJsonAsync(owner, $"{Api}/entries/{Guid.NewGuid()}/restore", new { }, await AdminApiFactory.StepUpAsync(owner, factory, Owner))).StatusCode);
        var flags = (await owner.GetFromJsonAsync<EntryPage>($"{Api}/entries"))!.Items.ToDictionary(entry => entry.Id, entry => entry.CanRestore);
        Assert.Equal((false, false, false, true), (flags[order], flags[created], flags[technical], flags[gone]));
    }

    [Fact]
    public void The_audit_actor_reads_the_request_and_is_the_system_without_one()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.9");
        context.Request.Headers.UserAgent = new string('u', 400);
        var accessor = new HttpContextAccessor { HttpContext = context };
        var actor = new HttpAuditActor(accessor);

        Assert.Equal((AuditActorKind.Anonymous, null, "203.0.113.9", "trace-1"), (actor.Kind, actor.UserId, actor.IpAddress, actor.CorrelationId));
        Assert.Equal(AuditRules.MaxUserAgentLength, actor.UserAgent!.Length);
        var userId = Guid.NewGuid();
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())], "test"));
        Assert.Equal((AuditActorKind.User, userId), (actor.Kind, actor.UserId));
        accessor.HttpContext = null;
        Assert.Equal((AuditActorKind.System, null), (actor.Kind, actor.UserId));
    }
}

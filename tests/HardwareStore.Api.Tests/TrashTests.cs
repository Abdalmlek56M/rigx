using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Trash (roadmap 2.10 A): delete goes to the trash, restore within 30 days, owner-only purge, and the purge job.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class TrashTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Trash = "/api/v1/admin/system/trash";
    private const string Content = "/api/v1/admin/content";
    private const string Editor = "editor@example.test";
    private const string Owner = "owner@example.test";
    private static readonly string[] EditorPermissions = ["System.StoreSettings", "System.Trash", "Sales.ApproveOrder", "Sales.ViewCustomerContact"];

    private sealed record TrashRow(string Kind, Guid Id, string Label, DateTimeOffset DeletedAtUtc, Guid? DeletedByUserId, DateTimeOffset PurgeAfterUtc);

    private static async Task<List<TrashRow>> TrashAsync(HttpClient client, string? kind = null) =>
        (await client.GetFromJsonAsync<List<TrashRow>>(kind is null ? Trash : $"{Trash}?kind={kind}"))!;

    private static async Task<int> ItemCountAsync(HttpClient client, string url)
    {
        using var document = JsonDocument.Parse(await client.GetStringAsync(url));
        return document.RootElement.GetProperty("items").GetArrayLength();
    }

    private static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string url, IDictionary<string, string>? headers = null) =>
        TestSignIn.SendJsonAsync(client, HttpMethod.Delete, url, null, headers);

    private static async Task<ArticleView> PublishedArticleAsync(IServiceProvider services, string slug)
    {
        using var scope = services.CreateScope();
        var articles = scope.ServiceProvider.GetRequiredService<ContentArticleService>();
        var created = await articles.CreateAsync(ContentArticles.Input(slug), Guid.NewGuid());
        return await articles.PublishAsync(created.Id, new ArticlePublishRequest(created.RowVersion), Guid.NewGuid());
    }

    private static async Task<int> AuditCountAsync(OwnerBootstrapDatabase database, string eventType, string kind, Guid id)
    {
        await using var audit = database.CreateAuditContext();
        var events = await audit.SecurityAuditEvents.AsNoTracking().Where(item => item.EventType == eventType).ToListAsync();
        return events.Count(item => JsonDocument.Parse(item.SafeMetadata).RootElement is var metadata &&
            metadata.GetProperty("kind").GetString() == kind && metadata.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task A_deleted_article_leaves_the_site_and_the_lists_sits_in_the_trash_and_comes_back_when_restored()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Editor, EditorPermissions);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var client = await AdminApiFactory.SignedInAsync(factory, Editor);
        using var anonymous = factory.CreatePreflightedClient(new() { HandleCookies = true });
        var article = await PublishedArticleAsync(factory.Services, "to-trash");
        Assert.Equal(1, await ItemCountAsync(anonymous, "/api/v1/content/articles"));

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(client, $"{Content}/articles/{article.Id}")).StatusCode);

        Assert.Equal(0, await ItemCountAsync(anonymous, "/api/v1/content/articles"));
        Assert.Equal(0, await ItemCountAsync(client, $"{Content}/articles"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Content}/articles/{article.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/v1/content/articles/to-trash")).StatusCode);
        var entry = Assert.Single(await TrashAsync(client, "article"));
        Assert.Equal(("article", article.Id, "عنوان عربي"), (entry.Kind, entry.Id, entry.Label));
        Assert.Equal(entry.DeletedAtUtc.AddDays(30), entry.PurgeAfterUtc);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(client, $"{Content}/articles/{article.Id}")).StatusCode);
        Assert.Empty(await TrashAsync(client, "faq"));

        Assert.Equal(HttpStatusCode.NoContent, (await TestSignIn.PostJsonAsync(client, $"{Trash}/article/{article.Id}/restore", new { })).StatusCode);

        Assert.Equal(1, await ItemCountAsync(anonymous, "/api/v1/content/articles"));
        Assert.Empty(await TrashAsync(client));
        Assert.Equal(1, await AuditCountAsync(database, TrashAuditor.DeletedEvent, "article", article.Id));
        Assert.Equal(1, await AuditCountAsync(database, TrashAuditor.RestoredEvent, "article", article.Id));
    }

    [Fact]
    public async Task Restoring_an_article_whose_slug_was_taken_meanwhile_is_a_409_and_an_unknown_kind_is_a_404()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Editor, EditorPermissions);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var client = await AdminApiFactory.SignedInAsync(factory, Editor);
        var first = await PublishedArticleAsync(factory.Services, "same-slug");
        await DeleteAsync(client, $"{Content}/articles/{first.Id}");
        var second = await PublishedArticleAsync(factory.Services, "same-slug");

        var conflict = await TestSignIn.PostJsonAsync(client, $"{Trash}/article/{first.Id}/restore", new { });

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Single(await TrashAsync(client, "article"));
        await DeleteAsync(client, $"{Content}/articles/{second.Id}");
        Assert.Equal(HttpStatusCode.NoContent, (await TestSignIn.PostJsonAsync(client, $"{Trash}/article/{first.Id}/restore", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await TestSignIn.PostJsonAsync(client, $"{Trash}/nothing/{first.Id}/restore", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Trash}?kind=nothing")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await TestSignIn.PostJsonAsync(client, $"{Trash}/article/{Guid.NewGuid()}/restore", new { })).StatusCode);
    }

    [Fact]
    public async Task A_deleted_faq_is_hidden_and_restorable()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Editor, EditorPermissions);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var client = await AdminApiFactory.SignedInAsync(factory, Editor);
        using var scope = factory.Services.CreateScope();
        var faqs = scope.ServiceProvider.GetRequiredService<ContentFaqService>();
        var faq = await faqs.CreateAsync(ContentFaqs.General("هل الشحن متاح؟", "Is shipping available?"));

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(client, $"{Content}/faqs/{faq.Id}")).StatusCode);

        Assert.Empty(await faqs.ListAdminAsync());
        Assert.Equal("faq", Assert.Single(await TrashAsync(client)).Kind);
        Assert.Equal(HttpStatusCode.NoContent, (await TestSignIn.PostJsonAsync(client, $"{Trash}/faq/{faq.Id}/restore", new { })).StatusCode);
        Assert.Single(await faqs.ListAdminAsync());
        Assert.Equal(1, await AuditCountAsync(database, TrashAuditor.DeletedEvent, "faq", faq.Id));
    }

    [Fact]
    public async Task Deleting_a_blacklist_entry_is_soft_stops_matching_and_restoring_brings_it_back()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Editor, EditorPermissions);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var client = await AdminApiFactory.SignedInAsync(factory, Editor);
        using var scope = factory.Services.CreateScope();
        var blacklist = scope.ServiceProvider.GetRequiredService<BlacklistService>();
        var entry = await blacklist.AddAsync(new BlacklistRequest(null, "Bad Buyer", "01011112222", null, null, "Fake orders"), Guid.NewGuid());
        Assert.True(await blacklist.MatchesContactAsync("01011112222", null));

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(client, $"/api/v1/admin/retail/blacklist/{entry.Id}")).StatusCode);

        Assert.False(await blacklist.MatchesContactAsync("01011112222", null));
        Assert.Empty(await blacklist.ListAsync());
        await using (var retail = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<RetailDbContext>>().CreateDbContextAsync())
            Assert.Equal(1, await retail.Blacklist.IgnoreQueryFilters().CountAsync(item => item.Id == entry.Id && item.DeletedAtUtc != null));
        Assert.Equal("Bad Buyer", Assert.Single(await TrashAsync(client, "blacklist")).Label);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(client, $"/api/v1/admin/retail/blacklist/{entry.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await TestSignIn.PostJsonAsync(client, $"{Trash}/blacklist/{entry.Id}/restore", new { })).StatusCode);

        Assert.True(await blacklist.MatchesContactAsync("01011112222", null));
        Assert.Equal(1, await AuditCountAsync(database, TrashAuditor.DeletedEvent, "blacklist", entry.Id));
    }

    [Fact]
    public async Task Purging_now_needs_the_purge_permission_and_a_fresh_code_and_removes_the_item_for_good()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Editor, EditorPermissions);
        await AdminApiFactory.StaffAsync(database, Owner, [.. EditorPermissions, "System.Purge"]);
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var editor = await AdminApiFactory.SignedInAsync(factory, Editor);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        var article = await PublishedArticleAsync(factory.Services, "purge-me");
        await DeleteAsync(owner, $"{Content}/articles/{article.Id}");

        var withoutPermission = await DeleteAsync(editor, $"{Trash}/article/{article.Id}", await AdminApiFactory.StepUpAsync(editor, factory, Editor));
        var withoutCode = await DeleteAsync(owner, $"{Trash}/article/{article.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, withoutPermission.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, withoutCode.StatusCode);
        Assert.Single(await TrashAsync(owner, "article"));
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(owner, $"{Trash}/article/{Guid.NewGuid()}", await AdminApiFactory.StepUpAsync(owner, factory, Owner))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(owner, $"{Trash}/nothing/{article.Id}", await AdminApiFactory.StepUpAsync(owner, factory, Owner))).StatusCode);

        var purged = await DeleteAsync(owner, $"{Trash}/article/{article.Id}", await AdminApiFactory.StepUpAsync(owner, factory, Owner));

        Assert.Equal(HttpStatusCode.NoContent, purged.StatusCode);
        Assert.Empty(await TrashAsync(owner));
        using var scope = factory.Services.CreateScope();
        await using var storefront = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<StorefrontDbContext>>().CreateDbContextAsync();
        Assert.False(await storefront.ContentArticles.IgnoreQueryFilters().AnyAsync(item => item.Id == article.Id));
        Assert.False(await storefront.ContentArticleRevisions.AnyAsync(revision => revision.ArticleId == article.Id));
        Assert.Equal(1, await AuditCountAsync(database, TrashAuditor.PurgedEvent, "article", article.Id));
    }

    [Fact]
    public async Task The_purge_job_removes_items_deleted_more_than_30_days_ago_and_nothing_newer()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        var old = await PublishedArticleAsync(factory.Services, "old-one");
        var recent = await PublishedArticleAsync(factory.Services, "recent-one");
        var oldFaq = Guid.Empty;
        using (var scope = factory.Services.CreateScope())
        {
            var articles = scope.ServiceProvider.GetRequiredService<ContentArticleService>();
            await articles.DeleteAsync(old.Id, Guid.NewGuid());
            await articles.DeleteAsync(recent.Id, Guid.NewGuid());
            var faqs = scope.ServiceProvider.GetRequiredService<ContentFaqService>();
            oldFaq = (await faqs.CreateAsync(ContentFaqs.General("سؤال قديم", "Old question"))).Id;
            await faqs.DeleteAsync(oldFaq, Guid.NewGuid());
        }
        var now = DateTimeOffset.UtcNow;
        await using (var storefront = await factory.Services.GetRequiredService<IDbContextFactory<StorefrontDbContext>>().CreateDbContextAsync())
        {
            await storefront.ContentArticles.IgnoreQueryFilters().Where(item => item.Id == old.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(item => item.DeletedAtUtc, now.AddDays(-31)));
            await storefront.ContentArticles.IgnoreQueryFilters().Where(item => item.Id == recent.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(item => item.DeletedAtUtc, now.AddDays(-5)));
            await storefront.ContentFaqs.IgnoreQueryFilters().Where(item => item.Id == oldFaq)
                .ExecuteUpdateAsync(set => set.SetProperty(item => item.DeletedAtUtc, now.AddDays(-40)));
        }

        using var runScope = factory.Services.CreateScope();
        var purged = await runScope.ServiceProvider.GetRequiredService<TrashService>().PurgeExpiredAsync(CancellationToken.None);

        Assert.Equal(2, purged);
        var left = await runScope.ServiceProvider.GetRequiredService<TrashService>().ListAsync(null, CancellationToken.None);
        Assert.Equal(recent.Id, Assert.Single(left).Id);
        Assert.Equal(1, await AuditCountAsync(database, TrashAuditor.PurgedEvent, "article", old.Id));
        Assert.Equal(1, await AuditCountAsync(database, TrashAuditor.PurgedEvent, "faq", oldFaq));
        Assert.Equal(0, await AuditCountAsync(database, TrashAuditor.PurgedEvent, "article", recent.Id));
        await using var audit = database.CreateAuditContext();
        Assert.All(await audit.SecurityAuditEvents.AsNoTracking().Where(item => item.EventType == TrashAuditor.PurgedEvent).ToListAsync(),
            item => Assert.Equal("System", item.ActorKind));
    }
}

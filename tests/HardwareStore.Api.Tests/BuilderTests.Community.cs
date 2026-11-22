using System.Net;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Modules.PcBuilder;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Build visibility, the moderated public gallery and hearts / likes on a shared build (roadmap 4.11 D).</summary>
public sealed partial class BuilderTests
{
    private const string Community = "/api/v1/builder/community";
    private const string AdminBuilds = "/api/v1/admin/customer-builds";
    private const string Manage = "Catalog.ManageProducts";

    private static async Task<SavedBuildView> SaveAsAsync(HttpClient client, object parts, BuildVisibility? visibility, string name = "Streaming PC") =>
        await ReadAsync<SavedBuildView>(await TestSignIn.PostJsonAsync(client, $"{Root}/saved", new { name, usage = "Gaming", parts, visibility }));

    private static async Task<SavedBuildView> UpdateAsync(HttpClient client, SavedBuildView build, object parts, BuildVisibility? visibility, string? name = null) =>
        await ReadAsync<SavedBuildView>(await TestSignIn.SendJsonAsync(client, HttpMethod.Put, $"{Root}/saved/{build.Token}",
            new { name = name ?? build.Name, usage = build.Usage, parts, rowVersion = build.RowVersion, visibility }));

    private static async Task<SavedBuildView> OpenAsync(HttpClient client, string token) =>
        await ReadAsync<SavedBuildView>(await client.GetAsync($"{Root}/saved/{token}"));

    private static Task<HttpStatusCode> DecideAsync(HttpClient admin, string token, string decision) =>
        StatusAsync(admin, HttpMethod.Post, $"{AdminBuilds}/{token}/decision", new { decision });

    private static async Task<BuildReactionSummary> ReactAsync(HttpClient client, HttpMethod method, string token, string kind) =>
        await ReadAsync<BuildReactionSummary>(await TestSignIn.SendJsonAsync(client, method, $"{Root}/saved/{token}/reactions/{kind}", null));

    private static async Task<PagedResult<CommunityBuildCard>> GalleryAsync(HttpClient client) =>
        await ReadAsync<PagedResult<CommunityBuildCard>>(await client.GetAsync(Community));

    private static async Task<PagedResult<CustomerBuildModerationView>> QueueAsync(HttpClient admin, string status = "Pending") =>
        await ReadAsync<PagedResult<CustomerBuildModerationView>>(await admin.GetAsync($"{AdminBuilds}?status={status}"));

    [Fact]
    public async Task A_build_is_by_link_unless_its_creator_chooses_and_a_private_build_opens_for_its_owner_only()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var owner = await env.SignedInAsync("owner");
        using var stranger = await env.SignedInAsync("stranger");

        var byLink = await SaveAsync(owner, Parts(cpu));
        var anonymous = await SaveAsync(env.Anonymous, Parts(cpu));
        var hidden = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Private);
        var unchanged = await UpdateAsync(owner, hidden, Parts(cpu), visibility: null, name: "Renamed");

        Assert.Equal((BuildVisibility.Link, BuildVisibility.Link), (byLink.Visibility, anonymous.Visibility));
        Assert.Null(byLink.Moderation);
        Assert.Equal((BuildVisibility.Private, BuildVisibility.Private), (hidden.Visibility, unchanged.Visibility));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(owner, HttpMethod.Get, $"{Root}/saved/{hidden.Token}"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(stranger, HttpMethod.Get, $"{Root}/saved/{hidden.Token}"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(env.Anonymous, HttpMethod.Get, $"{Root}/saved/{hidden.Token}"));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(env.Anonymous, HttpMethod.Get, $"{Root}/saved/{byLink.Token}"));
        var mine = await ReadAsync<List<SavedBuildSummary>>(await owner.GetAsync("/api/v1/account/builds"));
        Assert.Equal([BuildVisibility.Private, BuildVisibility.Link], mine.Select(build => build.Visibility));
    }

    [Fact]
    public async Task Only_a_signed_in_creator_can_save_a_private_or_public_build()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");

        foreach (var visibility in new[] { "Private", "Public" })
            Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/saved", new { parts = Parts(cpu), visibility }));
    }

    [Fact]
    public async Task A_public_build_shows_in_the_gallery_only_after_an_admin_approves_it_and_an_edit_sends_it_back()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        var board = await env.ItemAsync("BOARD", "B650 Board", 800m, socket: "AM5");
        using var admin = await env.SignedInAsync("admin", Manage);
        using var owner = await env.SignedInAsync("owner");
        using var stranger = await env.SignedInAsync("stranger");

        var pending = await SaveAsAsync(owner, Parts(cpu, board), BuildVisibility.Public);
        var strangerView = await OpenAsync(stranger, pending.Token);

        Assert.Equal((BuildVisibility.Public, BuildModerationStatus.Pending), (pending.Visibility, pending.Moderation));
        Assert.Null(strangerView.Moderation);
        Assert.Empty((await GalleryAsync(env.Anonymous)).Items);
        var queued = Assert.Single((await QueueAsync(admin)).Items);
        Assert.Equal((pending.Token, "Streaming PC", "owner", 2, 1800m), (queued.Build.Token, queued.Build.Name, queued.Build.AuthorName, queued.Build.PartCount, queued.Build.Total));
        Assert.Equal(HttpStatusCode.Forbidden, await StatusAsync(owner, HttpMethod.Get, AdminBuilds));
        Assert.Equal(HttpStatusCode.Forbidden, await DecideAsync(owner, pending.Token, "Approved"));
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(env.Anonymous, HttpMethod.Get, AdminBuilds));

        Assert.Equal(HttpStatusCode.OK, await DecideAsync(admin, pending.Token, "Approved"));

        var listed = Assert.Single((await GalleryAsync(env.Anonymous)).Items);
        Assert.Equal((pending.Token, "owner", 1800m, true), (listed.Token, listed.AuthorName, listed.Total, listed.CanOrder));
        Assert.Equal(["CPU", "BOARD"], listed.Parts.Select(part => part.Slot));
        Assert.Empty((await QueueAsync(admin)).Items);
        var approved = await OpenAsync(owner, pending.Token);
        Assert.Equal(BuildModerationStatus.Approved, approved.Moderation);

        // Saving it again unchanged keeps the approval; renaming it sends it back to the queue and out of the gallery.
        var resaved = await UpdateAsync(owner, approved, Parts(cpu, board), visibility: null);
        Assert.Equal(BuildModerationStatus.Approved, resaved.Moderation);
        var renamed = await UpdateAsync(owner, resaved, Parts(cpu, board), visibility: null, name: "Better name");
        Assert.Equal(BuildModerationStatus.Pending, renamed.Moderation);
        Assert.Empty((await GalleryAsync(env.Anonymous)).Items);
        Assert.Single((await QueueAsync(admin)).Items);

        Assert.Equal(HttpStatusCode.OK, await DecideAsync(admin, pending.Token, "Rejected"));
        Assert.Empty((await GalleryAsync(env.Anonymous)).Items);
        Assert.Equal(pending.Token, Assert.Single((await QueueAsync(admin, "Rejected")).Items).Build.Token);
        Assert.Equal(HttpStatusCode.BadRequest, await DecideAsync(admin, pending.Token, "Pending"));
    }

    [Fact]
    public async Task A_build_that_is_not_public_cannot_be_decided_and_going_private_takes_it_out_of_the_gallery()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var admin = await env.SignedInAsync("admin", Manage);
        using var owner = await env.SignedInAsync("owner");
        var byLink = await SaveAsync(owner, Parts(cpu));
        var shown = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Public);
        await DecideAsync(admin, shown.Token, "Approved");

        Assert.Equal(HttpStatusCode.Conflict, await DecideAsync(admin, byLink.Token, "Approved"));
        Assert.Equal(HttpStatusCode.NotFound, await DecideAsync(admin, "no-such-token", "Approved"));
        Assert.Single((await GalleryAsync(env.Anonymous)).Items);
        await UpdateAsync(owner, await OpenAsync(owner, shown.Token), Parts(cpu), BuildVisibility.Private);

        Assert.Empty((await GalleryAsync(env.Anonymous)).Items);
        Assert.Equal(HttpStatusCode.Conflict, await DecideAsync(admin, shown.Token, "Rejected"));
    }

    [Fact]
    public async Task A_decision_is_audited_with_the_admin_who_made_it()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var admin = await env.SignedInAsync("admin", Manage);
        using var owner = await env.SignedInAsync("owner");
        var build = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Public);

        await DecideAsync(admin, build.Token, "Approved");

        await using var connection = new NpgsqlConnection(env.Database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"SELECT {AuditTable.Action}, {AuditTable.ActorUserId} FROM {AuditTable.Name} WHERE {AuditTable.Entity} = 'saved_build_moderations' ORDER BY {AuditTable.OccurredAtUtc}, {AuditTable.Id}",
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<(string Action, Guid? Actor)>();
        while (await reader.ReadAsync()) rows.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetGuid(1)));
        Assert.Equal(["Created", "Updated"], rows.Select(row => row.Action));
        Assert.All(rows, row => Assert.NotNull(row.Actor));
        Assert.NotEqual(rows[0].Actor, rows[1].Actor);
    }

    [Fact]
    public async Task A_signed_in_visitor_hearts_and_likes_a_shared_build_once_and_removing_what_is_not_there_changes_nothing()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var owner = await env.SignedInAsync("owner");
        using var fan = await env.SignedInAsync("fan");
        using var other = await env.SignedInAsync("other");
        var build = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Link);
        string Url(string kind) => $"{Root}/saved/{build.Token}/reactions/{kind}";

        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(env.Anonymous, HttpMethod.Put, Url("Heart")));
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(env.Anonymous, HttpMethod.Delete, Url("Heart")));
        await ReactAsync(fan, HttpMethod.Put, build.Token, "Heart");
        var twice = await ReactAsync(fan, HttpMethod.Put, build.Token, "Heart");
        var liked = await ReactAsync(fan, HttpMethod.Put, build.Token, "Like");
        var byOther = await ReactAsync(other, HttpMethod.Put, build.Token, "Heart");

        Assert.Equal(new BuildReactionSummary(1, 0, true, false), twice);
        Assert.Equal(new BuildReactionSummary(1, 1, true, true), liked);
        Assert.Equal(new BuildReactionSummary(2, 1, true, false), byOther);
        Assert.Equal(new BuildReactionSummary(2, 1, false, false), (await OpenAsync(env.Anonymous, build.Token)).Reactions);
        Assert.Equal(new BuildReactionSummary(2, 1, true, true), (await OpenAsync(fan, build.Token)).Reactions);

        var removed = await ReactAsync(fan, HttpMethod.Delete, build.Token, "Heart");
        var again = await ReactAsync(fan, HttpMethod.Delete, build.Token, "Heart");
        Assert.Equal(new BuildReactionSummary(1, 1, false, true), removed);
        Assert.Equal(removed, again);
    }

    [Fact]
    public async Task Nobody_can_react_to_a_private_or_missing_build_and_a_deleted_build_takes_its_reactions_with_it()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var owner = await env.SignedInAsync("owner");
        using var fan = await env.SignedInAsync("fan");
        var hidden = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Private);
        var shown = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Link);
        await ReactAsync(fan, HttpMethod.Put, shown.Token, "Like");

        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(fan, HttpMethod.Put, $"{Root}/saved/{hidden.Token}/reactions/Heart"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(fan, HttpMethod.Put, $"{Root}/saved/no-such-token/reactions/Heart"));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(owner, HttpMethod.Put, $"{Root}/saved/{hidden.Token}/reactions/Heart"));
        Assert.Equal(HttpStatusCode.NoContent, await StatusAsync(owner, HttpMethod.Delete, $"{Root}/saved/{shown.Token}"));
        await using var connection = new NpgsqlConnection(env.Database.ConnectionString);
        await connection.OpenAsync();
        await using var count = new NpgsqlCommand("SELECT count(*) FROM saved_build_reactions WHERE kind = 'Like'", connection);
        Assert.Equal(0L, await count.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Hearted_shared_builds_show_on_the_favourites_list_until_they_become_private_and_the_gallery_carries_the_counts()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var admin = await env.SignedInAsync("admin", Manage);
        using var owner = await env.SignedInAsync("owner");
        using var fan = await env.SignedInAsync("fan");
        var build = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Public);
        await DecideAsync(admin, build.Token, "Approved");
        await ReactAsync(fan, HttpMethod.Put, build.Token, "Heart");
        await ReactAsync(fan, HttpMethod.Put, build.Token, "Like");

        var favourites = await ReadAsync<FavouriteList>(await fan.GetAsync("/api/v1/account/favourites"));
        var card = Assert.Single(favourites.SharedBuilds);
        Assert.Equal((build.Token, new BuildReactionSummary(1, 1, true, true)), (card.Token, card.Reactions));
        Assert.Equal(new BuildReactionSummary(1, 1, true, true), Assert.Single((await GalleryAsync(fan)).Items).Reactions);
        Assert.Equal(new BuildReactionSummary(1, 1, false, false), Assert.Single((await GalleryAsync(env.Anonymous)).Items).Reactions);
        Assert.Empty((await ReadAsync<FavouriteList>(await owner.GetAsync("/api/v1/account/favourites"))).SharedBuilds);

        await UpdateAsync(owner, await OpenAsync(owner, build.Token), Parts(cpu), BuildVisibility.Private);
        Assert.Empty((await ReadAsync<FavouriteList>(await fan.GetAsync("/api/v1/account/favourites"))).SharedBuilds);
    }
    [Fact]
    public async Task The_owner_changes_who_can_open_a_saved_build_without_touching_its_parts_and_a_stranger_cannot()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var admin = await env.SignedInAsync("admin", Manage);
        using var owner = await env.SignedInAsync("owner");
        using var stranger = await env.SignedInAsync("stranger");
        var anonymous = await SaveAsync(env.Anonymous, Parts(cpu));
        var build = await SaveAsync(owner, Parts(cpu));
        string Url(string token) => $"{Root}/saved/{token}/visibility";

        var made = await ReadAsync<SavedBuildSummary>(await TestSignIn.SendJsonAsync(owner, HttpMethod.Put, Url(build.Token), new { visibility = "Public" }));
        var hidden = await ReadAsync<SavedBuildSummary>(await TestSignIn.SendJsonAsync(owner, HttpMethod.Put, Url(build.Token), new { visibility = "Private" }));

        Assert.Equal((BuildVisibility.Public, BuildModerationStatus.Pending, 1), (made.Visibility, made.Moderation, made.PartCount));
        Assert.Equal((BuildVisibility.Private, BuildModerationStatus.Pending), (hidden.Visibility, hidden.Moderation));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(stranger, HttpMethod.Get, $"{Root}/saved/{build.Token}"));
        Assert.Equal(3L, (await OpenAsync(owner, build.Token)).RowVersion);
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(stranger, HttpMethod.Put, Url(build.Token), new { visibility = "Public" }));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(owner, HttpMethod.Put, Url(anonymous.Token), new { visibility = "Public" }));
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(env.Anonymous, HttpMethod.Put, Url(build.Token), new { visibility = "Public" }));
    }
}

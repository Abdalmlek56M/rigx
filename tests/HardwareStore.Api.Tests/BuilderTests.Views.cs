using System.Net;
using HardwareStore.Modules.PcBuilder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>"Save to my builds" and the view counter of a shared build (roadmap 4.11 E2).</summary>
public sealed partial class BuilderTests
{
    private const string Browser = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36";

    private static async Task<HttpStatusCode> ViewAsync(HttpClient client, string token, string? userAgent = Browser)
    {
        client.DefaultRequestHeaders.UserAgent.Clear();
        if (userAgent is not null) client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        return (await TestSignIn.SendJsonAsync(client, HttpMethod.Post, $"{Root}/saved/{token}/views", null)).StatusCode;
    }

    private static async Task FlushViewsAsync(Env env)
    {
        using var scope = env.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<BuildViewStore>().FlushAsync();
    }

    [Fact]
    public async Task Save_to_my_builds_keeps_one_private_copy_of_a_shared_build_and_never_of_a_private_one()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var owner = await env.SignedInAsync("owner");
        using var visitor = await env.SignedInAsync("visitor");
        var shared = await SaveAsync(owner, Parts(cpu));
        var hidden = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Private, "Hidden");
        string Copy(string token) => $"{Root}/saved/{token}/copy";

        var copy = await ReadAsync<SavedBuildSummary>(await TestSignIn.SendJsonAsync(visitor, HttpMethod.Post, Copy(shared.Token), null));
        var again = await ReadAsync<SavedBuildSummary>(await TestSignIn.SendJsonAsync(visitor, HttpMethod.Post, Copy(shared.Token), null));
        var own = await ReadAsync<SavedBuildSummary>(await TestSignIn.SendJsonAsync(owner, HttpMethod.Post, Copy(shared.Token), null));

        Assert.NotEqual(shared.Token, copy.Token);
        Assert.Equal((BuildVisibility.Private, shared.Name, 1), (copy.Visibility, copy.Name, copy.PartCount));
        Assert.Equal(copy.Token, again.Token);
        Assert.Equal(shared.Token, own.Token);
        Assert.Equal([copy.Token], (await ReadAsync<List<SavedBuildSummary>>(await visitor.GetAsync("/api/v1/account/builds"))).Select(build => build.Token));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(visitor, HttpMethod.Post, Copy(hidden.Token)));
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(env.Anonymous, HttpMethod.Post, Copy(shared.Token)));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(visitor, HttpMethod.Get, $"{Root}/saved/{copy.Token}"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(owner, HttpMethod.Get, $"{Root}/saved/{copy.Token}"));
    }

    [Fact]
    public async Task A_view_counts_once_per_visitor_per_day_and_never_for_robots_the_owner_or_a_private_build()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        using var owner = await env.SignedInAsync("owner");
        using var fan = await env.SignedInAsync("fan");
        var build = await SaveAsync(owner, Parts(cpu));
        var hidden = await SaveAsAsync(owner, Parts(cpu), BuildVisibility.Private, "Hidden");

        Assert.Equal(HttpStatusCode.NoContent, await ViewAsync(env.Anonymous, build.Token));
        await ViewAsync(env.Anonymous, build.Token);
        await ViewAsync(env.Anonymous, build.Token, "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0) Safari/604.1");
        await ViewAsync(fan, build.Token);
        await ViewAsync(env.Anonymous, build.Token, "Googlebot/2.1 (+http://www.google.com/bot.html)");
        await ViewAsync(env.Anonymous, build.Token, userAgent: null);
        await ViewAsync(owner, build.Token);
        await ViewAsync(owner, hidden.Token);
        Assert.Equal(HttpStatusCode.NoContent, await ViewAsync(env.Anonymous, "no-such-build"));

        Assert.Equal(0, (await OpenAsync(env.Anonymous, build.Token)).Reactions.Views);
        await FlushViewsAsync(env);
        await FlushViewsAsync(env);

        Assert.Equal(3, (await OpenAsync(env.Anonymous, build.Token)).Reactions.Views);
        Assert.Equal(0, (await OpenAsync(owner, hidden.Token)).Reactions.Views);
        var mine = await ReadAsync<List<SavedBuildSummary>>(await owner.GetAsync("/api/v1/account/builds"));
        Assert.Equal(3, mine.Single(summary => summary.Token == build.Token).Views);
    }
}

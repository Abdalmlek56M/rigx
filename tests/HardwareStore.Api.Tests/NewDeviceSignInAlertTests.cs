using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class NewDeviceSignInAlertTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Setting = "/api/v1/identity/settings/new-device-alerts";
    private const string Email = "alerts@example.test";
    private const string DesktopChrome =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    private const string IPhoneSafari =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";

    [Fact]
    public async Task First_ever_sign_in_sends_no_alert()
    {
        await using var factory = await StartAsync();
        using var client = NewClient(factory, DesktopChrome);

        await TestSignIn.SignInAsync(client, factory.Services, Email);

        Assert.Empty(await DeliveredAlertsAsync(factory));
    }

    [Fact]
    public async Task Sign_in_from_the_same_user_agent_sends_no_alert()
    {
        await using var factory = await StartAsync();
        using var first = NewClient(factory, DesktopChrome);
        await TestSignIn.SignInAsync(first, factory.Services, Email);
        using var second = NewClient(factory, DesktopChrome);

        await TestSignIn.SignInAsync(second, factory.Services, Email);

        Assert.Empty(await DeliveredAlertsAsync(factory));
    }

    [Fact]
    public async Task Sign_in_from_a_new_user_agent_sends_one_alert_with_device_and_browser()
    {
        await using var factory = await StartAsync();
        using var first = NewClient(factory, DesktopChrome);
        await TestSignIn.SignInAsync(first, factory.Services, Email);
        using var phone = NewClient(factory, IPhoneSafari);

        await TestSignIn.SignInAsync(phone, factory.Services, Email);

        var alert = Assert.Single(await DeliveredAlertsAsync(factory));
        Assert.Equal(Email, alert.Email);
        Assert.Equal("Mobile", alert.Device);
        Assert.Equal("Safari", alert.Browser);
    }

    [Fact]
    public async Task Setting_defaults_on_and_turning_it_off_suppresses_alerts()
    {
        await using var factory = await StartAsync();
        using var first = NewClient(factory, DesktopChrome);
        await TestSignIn.SignInAsync(first, factory.Services, Email);
        Assert.True(await ReadSettingAsync(first));

        using var off = await TestSignIn.SendJsonAsync(first, HttpMethod.Put, Setting, new { enabled = false });
        Assert.True(off.IsSuccessStatusCode);
        Assert.False(await ReadSettingAsync(first));

        using var phone = NewClient(factory, IPhoneSafari);
        await TestSignIn.SignInAsync(phone, factory.Services, Email);
        Assert.Empty(await DeliveredAlertsAsync(factory));

        using var on = await TestSignIn.SendJsonAsync(first, HttpMethod.Put, Setting, new { enabled = true });
        Assert.True(on.IsSuccessStatusCode);
        Assert.True(await ReadSettingAsync(first));
    }

    [Fact]
    public async Task Setting_endpoints_require_a_signed_in_user()
    {
        await using var factory = await StartAsync();
        using var client = NewClient(factory, DesktopChrome);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Setting)).StatusCode);
        using var put = await TestSignIn.SendJsonAsync(client, HttpMethod.Put, Setting, new { enabled = false });
        Assert.Equal(HttpStatusCode.Unauthorized, put.StatusCode);
    }

    private async Task<IdentityLoginApiFactory> StartAsync()
    {
        var database = await fixture.CreateDatabaseAsync();
        return new IdentityLoginApiFactory(database.ConnectionString);
    }

    private static HttpClient NewClient(IdentityLoginApiFactory factory, string userAgent)
    {
        var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
        return client;
    }

    private static async Task<bool> ReadSettingAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<SettingPayload>(Setting))!.Enabled;

    private static async Task<IReadOnlyCollection<NewDeviceSignInMessage>> DeliveredAlertsAsync(
        IdentityLoginApiFactory factory)
    {
        await TestSignIn.DeliverOutboxAsync(factory.Services);
        return factory.Services.GetRequiredService<InMemoryTestEmailProvider>().NewDeviceSignIns;
    }

    private sealed record SettingPayload(bool Enabled);
}

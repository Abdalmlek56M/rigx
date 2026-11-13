using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Signs a test client in the way a real user does: request an email code, read it, enter it.</summary>
internal static class TestSignIn
{
    public static async Task SignInAsync(HttpClient client, IServiceProvider services, string email, bool rememberDevice = false)
    {
        using var request = await PostJsonAsync(client, "/api/v1/identity/email-code/request", new { email });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var code = await LatestCodeAsync(services, email);
        using var verify = await PostJsonAsync(client, "/api/v1/identity/email-code/verify",
            new { email, code = code.Code, rememberDevice });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
    }

    /// <summary>Delivers due outbox emails now (instead of waiting for the background poll) and returns the newest code.</summary>
    public static async Task<EmailCodeMessage> LatestCodeAsync(IServiceProvider services, string email, string? differentFrom = null)
    {
        await DeliverOutboxAsync(services);
        var provider = services.GetRequiredService<InMemoryTestEmailProvider>();
        return provider.EmailCodes.LastOrDefault(item =>
                   string.Equals(item.Email, email, StringComparison.OrdinalIgnoreCase) && item.Code != differentFrom)
               ?? throw new InvalidOperationException($"No email code was delivered to {email}.");
    }

    public static Task DeliverOutboxAsync(IServiceProvider services) =>
        services.GetServices<IHostedService>().OfType<IdentityEmailOutboxProcessor>().Single()
            .ProcessDueAsync(CancellationToken.None);

    public static async Task<HttpResponseMessage> PostJsonAsync(
        HttpClient client, string url, object body, IDictionary<string, string>? headers = null) =>
        await SendJsonAsync(client, HttpMethod.Post, url, body, headers);

    public static async Task<HttpResponseMessage> SendJsonAsync(
        HttpClient client, HttpMethod method, string url, object? body, IDictionary<string, string>? headers = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        foreach (var header in headers ?? new Dictionary<string, string>())
            request.Headers.Add(header.Key, header.Value);
        return await client.SendAsync(request);
    }

    public static async Task<string> CsrfAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/identity/antiforgery");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CsrfPayload>();
        return payload!.RequestToken;
    }

    private sealed record CsrfPayload(string RequestToken);
}

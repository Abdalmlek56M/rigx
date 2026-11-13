using System.Net;
using System.Net.Http.Headers;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class CsrfProtectionTests
{
    [Fact]
    public async Task Unsafe_request_without_token_is_rejected_and_valid_same_origin_token_is_accepted()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "HardwareStore.Test.CSRF";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
            });
        await using var app = builder.Build();
        app.UseMiddleware<CsrfProtectionMiddleware>();
        app.Run(context => context.Response.WriteAsync("accepted"));
        await app.StartAsync();
        using var client = app.GetTestClient();

        var withoutToken = await client.PostAsync("/unsafe", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.BadRequest, withoutToken.StatusCode);

        var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        var tokens = antiforgery.GetAndStoreTokens(context);
        var cookie = context.Response.Headers.SetCookie.ToString().Split(';', 2)[0];
        Assert.NotNull(tokens.RequestToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/unsafe")
        {
            Content = new StringContent("{}")
        };
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("X-CSRF-TOKEN", tokens.RequestToken);
        var accepted = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task Safe_get_does_not_require_an_antiforgery_token()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAntiforgery();
        await using var app = builder.Build();
        app.UseMiddleware<CsrfProtectionMiddleware>();
        app.Run(context => context.Response.WriteAsync("accepted"));
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>
/// Phase 3 contract: the OpenAPI document generated from the endpoints must match the committed
/// <c>packages/api-client/openapi.v1.json</c> (the frontend client is generated from it), every operation
/// declares its success body, and every error is a ProblemDetails body.
/// Regenerate after an API change: set <c>UPDATE_API_CONTRACT=1</c> and run these tests.
/// </summary>
public sealed class ApiContractTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string[] Methods = ["get", "post", "put", "patch", "delete"];

    [Fact]
    public async Task Openapi_document_matches_the_committed_contract()
    {
        var generated = await GenerateAsync();
        var path = ContractPath();
        if (Environment.GetEnvironmentVariable("UPDATE_API_CONTRACT") == "1")
            await File.WriteAllTextAsync(path, generated);

        Assert.True(File.Exists(path), $"Missing {path}. Run the tests with UPDATE_API_CONTRACT=1.");
        var committed = (await File.ReadAllTextAsync(path)).ReplaceLineEndings("\n");
        Assert.True(committed == generated,
            "The API changed but packages/api-client/openapi.v1.json was not regenerated. Run the tests with UPDATE_API_CONTRACT=1.");
    }

    [Fact]
    public async Task Every_operation_declares_its_success_response_and_problem_errors()
    {
        var document = JsonNode.Parse(await GenerateAsync())!;
        var untyped = new List<string>();
        foreach (var (path, item) in document["paths"]!.AsObject())
        foreach (var method in Methods)
        {
            if (item![method] is not JsonObject operation) continue;
            var responses = operation["responses"]!.AsObject();
            Assert.True(responses.ContainsKey("default"), $"{method} {path} has no ProblemDetails error response.");
            var success = responses.Where(pair => pair.Key.StartsWith('2')).ToList();
            var described = success.Any(pair =>
                pair.Key is "204" or "202" && pair.Value!["content"] is null ||
                pair.Value!["content"] is JsonObject content && content.Count > 0);
            if (!described) untyped.Add($"{method.ToUpperInvariant()} {path}");
        }

        Assert.True(untyped.Count == 0, "Operations without a typed success response:\n" + string.Join("\n", untyped));
    }

    [Fact]
    public async Task Auth_and_csrf_failures_are_problem_details()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var unauthorized = await client.GetAsync("/api/v1/admin/audit/filters");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal("application/problem+json", unauthorized.Content.Headers.ContentType?.MediaType);

        using var noCsrf = await client.PostAsJsonAsync("/api/v1/identity/email-code/request", new { email = "a@b.c" });
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        var problem = await noCsrf.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Invalid anti-forgery token.", problem.GetProperty("title").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Problem_responses_are_in_Arabic_for_an_Arabic_caller_only()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        async Task<string?> TitleAsync(string? language)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/email-code/request")
            { Content = JsonContent.Create(new { email = "a@b.c" }) };
            if (language is not null) request.Headers.AcceptLanguage.ParseAdd(language);
            using var response = await client.SendAsync(request);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString();
        }

        Assert.Equal("Invalid anti-forgery token.", await TitleAsync(null));
        var arabic = await TitleAsync("ar");
        Assert.NotEqual("Invalid anti-forgery token.", arabic);
        Assert.Matches("[؀-ۿ]", arabic);

        using var unauthorized = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit/filters");
        unauthorized.Headers.AcceptLanguage.ParseAdd("ar");
        using var response = await client.SendAsync(unauthorized);
        Assert.Matches("[؀-ۿ]", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }

    private async Task<string> GenerateAsync()
    {
        using var client = factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");
        var node = JsonNode.Parse(json)!;
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n";
    }

    private static string ContractPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HARDWARE_STORE.slnx")))
            directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."),
            "packages", "api-client", "openapi.v1.json");
    }
}

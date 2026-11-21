using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Arabic API errors: the language switch, the status fallback, templates, and that no error message written in the code is missing from the catalogue.</summary>
public sealed partial class ApiErrorCatalogTests
{
    private static DefaultHttpContext Context(string? acceptLanguage)
    {
        var context = new DefaultHttpContext();
        if (acceptLanguage is not null) context.Request.Headers.AcceptLanguage = acceptLanguage;
        return context;
    }

    [Fact]
    public void An_Arabic_caller_gets_the_Arabic_text_and_any_other_caller_keeps_the_English_one()
    {
        var arabic = new ProblemDetails { Status = 401, Title = "Sign in first." };
        ApiErrorCatalog.Localize(Context("ar,en;q=0.8"), arabic);
        Assert.Equal("سجّل الدخول أولًا.", arabic.Title);

        foreach (var language in new[] { null, "en-US,en;q=0.9", "fr" })
        {
            var english = new ProblemDetails { Status = 401, Title = "Sign in first." };
            ApiErrorCatalog.Localize(Context(language), english);
            Assert.Equal("Sign in first.", english.Title);
        }
    }

    [Fact]
    public void A_message_without_an_entry_falls_back_to_the_Arabic_sentence_of_its_status_never_English()
    {
        var problem = new ProblemDetails { Status = 404, Title = "Something nobody translated.", Detail = "More English." };
        ApiErrorCatalog.Localize(Context("ar"), problem);
        Assert.Equal(ApiErrorCatalog.ForStatus(404), problem.Title);
        Assert.Equal(ApiErrorCatalog.ForStatus(404), problem.Detail);

        var framework = new HttpValidationProblemDetails(new Dictionary<string, string[]> { ["name"] = ["The name field is required."] })
        { Status = 400, Title = "One or more validation errors occurred." };
        ApiErrorCatalog.Localize(Context("ar"), framework);
        Assert.Equal(ApiErrorCatalog.ForStatus(400), framework.Title);
        Assert.Equal(ApiErrorCatalog.ForStatus(400), Assert.Single(framework.Errors["name"]));
    }

    [Fact]
    public void Every_status_has_an_Arabic_fallback_sentence()
    {
        foreach (var status in new[] { 400, 401, 403, 404, 405, 409, 413, 415, 422, 429, 500, 503 })
            Assert.Matches(ArabicLetters(), ApiErrorCatalog.ForStatus(status));
    }

    [Fact]
    public void Every_template_marker_of_the_English_pattern_is_in_the_Arabic_text()
    {
        foreach (var (english, arabic) in ApiErrorMessages.Templates)
            foreach (Match marker in Marker().Matches(english))
                Assert.True(arabic.Contains(marker.Value, StringComparison.Ordinal), $"{english} → {arabic}");
    }

    [Fact]
    public void Every_Arabic_text_is_Arabic_and_has_no_colloquial_wording()
    {
        var texts = ApiErrorMessages.Exact.Values.Concat(ApiErrorMessages.Templates.Select(entry => entry.Arabic)).ToList();
        Assert.NotEmpty(texts);
        foreach (var text in texts)
        {
            Assert.Matches(ArabicLetters(), text);
            Assert.DoesNotContain("  ", text);
        }
    }

    /// <summary>Every English message the code throws as a business error (a literal in an exception's constructor) has an Arabic text.</summary>
    [Fact]
    public void Every_error_message_written_in_the_code_has_an_Arabic_text()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "HARDWARE_STORE.slnx"))) root = Path.GetDirectoryName(root);
        Assert.NotNull(root);

        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var total = 0;
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}")))
        {
            foreach (Match match in ThrownMessage().Matches(File.ReadAllText(file)))
            {
                total++;
                var message = Sample(match.Groups[1].Value == "$", match.Groups[2].Value);
                if (ApiErrorCatalog.Find(message) is null) missing.Add(message);
            }
        }

        Assert.True(total > 300, $"The scan found only {total} messages; the pattern no longer matches the code.");
        Assert.True(missing.Count == 0, $"{missing.Count} error messages have no Arabic text:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    /// <summary>The literal as the text it is at run time; an interpolated value ({x}) becomes a sample word so a template can match it.</summary>
    private static string Sample(bool interpolated, string literal)
    {
        var text = literal.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("{{", "\u0001").Replace("}}", "\u0002");
        if (interpolated) text = Interpolation().Replace(text, "sample");
        return text.Replace("\u0001", "{").Replace("\u0002", "}");
    }

    [GeneratedRegex("""new\s+(?:Retail|Purchasing|Identity|Storefront|CatalogValidation|CatalogNotFound|CatalogConflict|Review|PcBuilder|Audit|ApiProblem|WarehouseDirectory|Backup|DataReset|Export|Trash|Unauthenticated|XlsxRead)\w*Exception\(\s*(?:[\w.]+\s*,\s*)?(\$?)"((?:[^"\\]|\\.)*)"(?!\s*\+)""", RegexOptions.CultureInvariant)]
    private static partial Regex ThrownMessage();

    [GeneratedRegex(@"\{[^{}]+\}")]
    private static partial Regex Interpolation();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Marker();

    [GeneratedRegex(@"[؀-ۿ]")]
    private static partial Regex ArabicLetters();
}

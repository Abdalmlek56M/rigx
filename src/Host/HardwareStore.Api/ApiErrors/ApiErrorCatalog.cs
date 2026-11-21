using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Formal-Arabic error messages. The domain keeps throwing English messages (they are the stable, tested text); this catalogue turns
/// them into Arabic at the one place every problem response passes through (<see cref="Localize"/>), when the caller asks for Arabic
/// with <c>Accept-Language: ar</c>. A message with no entry falls back to a short Arabic sentence for its HTTP status, so an Arabic
/// caller never sees English. <see cref="ApiErrorMessages"/> holds the texts: <c>Exact</c> for fixed messages and <c>Templates</c> for
/// messages with values in them (<c>{1}</c>, <c>{2}</c> … in the English pattern and in the Arabic text).
/// </summary>
public static partial class ApiErrorCatalog
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static readonly Lazy<(Regex Pattern, string Arabic)[]> Compiled = new(() =>
        [.. ApiErrorMessages.Templates.Select(entry => (ToRegex(entry.English), entry.Arabic))]);

    /// <summary>True when the caller's first preferred language is Arabic.</summary>
    public static bool WantsArabic(HttpContext context)
    {
        var header = context.Request.Headers.AcceptLanguage.ToString();
        var first = header.Split(',', 2, StringSplitOptions.TrimEntries)[0];
        return first.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The Arabic text of an English message, or null when the catalogue has none.</summary>
    public static string? Find(string english)
    {
        if (ApiErrorMessages.Exact.TryGetValue(english, out var exact)) return exact;
        // ASP.NET's own validation sentences ("The Name field is required.") would only half-match the label templates: use the status sentence.
        if (FrameworkValidation().IsMatch(english)) return null;
        foreach (var (pattern, arabic) in Compiled.Value)
        {
            var match = pattern.Match(english);
            if (!match.Success) continue;
            return Cache.GetOrAdd(english, _ => match.Groups.Cast<Group>().Skip(1)
                .Aggregate(arabic, (text, group) => text.Replace($"{{{group.Name}}}", group.Value, StringComparison.Ordinal)));
        }
        return null;
    }

    /// <summary>A short Arabic sentence for a status, used when a message has no entry.</summary>
    public static string ForStatus(int status) => status switch
    {
        400 or 422 => "البيانات المُرسلة غير صحيحة.",
        401 => "سجّل الدخول أولًا.",
        403 => "ليست لديك صلاحية لتنفيذ هذا الإجراء.",
        404 => "العنصر المطلوب غير موجود.",
        405 => "هذا الطلب غير مسموح به.",
        409 => "تعذّر تنفيذ الطلب لأن البيانات تغيّرت أو تتعارض مع حالتها الحالية.",
        413 => "حجم الملف أو الطلب أكبر من المسموح به.",
        415 => "نوع الملف غير مدعوم.",
        429 => "عدد المحاولات كبير، حاول مرة أخرى بعد قليل.",
        >= 500 => "حدث خطأ غير متوقع. حاول مرة أخرى.",
        _ => "تعذّر تنفيذ الطلب."
    };

    /// <summary>Puts the title, detail and field errors of a problem response into Arabic when the caller asks for Arabic.</summary>
    public static void Localize(HttpContext context, ProblemDetails problem)
    {
        if (!WantsArabic(context)) return;
        var status = problem.Status ?? context.Response.StatusCode;
        problem.Title = problem.Title is { Length: > 0 } title ? Find(title) ?? ForStatus(status) : ForStatus(status);
        if (problem.Detail is { Length: > 0 } detail) problem.Detail = Find(detail) ?? ForStatus(status);
        if (problem is HttpValidationProblemDetails validation)
            foreach (var key in validation.Errors.Keys.ToList())
                validation.Errors[key] = [.. validation.Errors[key].Select(message => Find(message) ?? ForStatus(400))];
    }

    [GeneratedRegex(@"^The .+ field ", RegexOptions.CultureInvariant)]
    private static partial Regex FrameworkValidation();

    private static Regex ToRegex(string template)
    {
        var parts = Regex.Split(Regex.Escape(template), @"\\\{(\d+)\}");
        var pattern = string.Concat(parts.Select((part, index) => index % 2 == 0 ? part : $"(?<{part}>.+?)"));
        return new Regex($"^{pattern}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }
}

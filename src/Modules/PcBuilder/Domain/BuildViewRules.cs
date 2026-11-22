using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>
/// What counts as a view of a shared build: one visit per visitor per business day, never a robot, never the build's own owner, and
/// never a private build (only its owner can open one).
/// </summary>
public static partial class BuildViewRules
{
    public static bool Counts(BuildVisibility visibility, Guid? ownerId, Guid? viewerId) =>
        visibility != BuildVisibility.Private && !(ownerId is not null && ownerId == viewerId);

    /// <summary>A missing user agent, a known crawler, a link-preview fetcher or a command-line client is not a person.</summary>
    public static bool IsRobot(string? userAgent) => string.IsNullOrWhiteSpace(userAgent) || Robot().IsMatch(userAgent);

    /// <summary>
    /// Who the visitor is for the day's "one view each" rule: the account when signed in, else a hash of address and browser.
    /// It lives only in memory until the day ends; it is never stored.
    /// </summary>
    public static string VisitorKey(Guid? viewerId, string? address, string? userAgent) =>
        viewerId is { } account
            ? account.ToString("N")
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{address}|{userAgent}")));

    [GeneratedRegex("bot|crawl|spider|slurp|preview|facebookexternalhit|whatsapp|telegram|lighthouse|headless|monitor|curl|wget|python|httpclient|go-http|java/|okhttp|axios|node-fetch",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Robot();
}

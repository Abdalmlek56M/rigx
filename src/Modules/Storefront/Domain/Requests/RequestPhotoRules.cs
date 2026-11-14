using System.Security.Cryptography;
using System.Text;

namespace HardwareStore.Modules.Storefront;

/// <summary>A freshly issued upload token: the customer gets <see cref="Token"/>, the request keeps only the hash and the expiry.</summary>
public sealed record IssuedPhotoToken(string Token, string Hash, DateTimeOffset ExpiresAtUtc);

/// <summary>Photos a customer attaches to a request: which types accept them, how long the upload token lives, how many photos fit.</summary>
public static class RequestPhotoRules
{
    public const int MaxPhotos = 5;
    private const int TokenBytes = 32;
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    public static bool AcceptsPhotos(string type) =>
        type is CustomerRequestType.Sell or CustomerRequestType.TradeIn or CustomerRequestType.Upgrade or CustomerRequestType.Support;

    public static IssuedPhotoToken IssueToken(DateTimeOffset now)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(TokenBytes));
        return new IssuedPhotoToken(token, Hash(token), now + TokenLifetime);
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Unknown, replaced or expired token is a 404 (the customer learns nothing); a full request is a 409.</summary>
    public static CustomerRequest EnsureCanAdd(CustomerRequest? request, DateTimeOffset now)
    {
        if (request is null || request.PhotoTokenExpiresAtUtc is not { } expires || expires <= now)
            throw new StorefrontException(404, "The photo upload link is not valid.");
        if (request.Photos.Count >= MaxPhotos)
            throw new StorefrontException(409, $"A request can have at most {MaxPhotos} photos.");
        return request;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

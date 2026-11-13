using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace HardwareStore.Modules.Identity.Authentication;

/// <summary>What RIGX needs from a Google ID token after it has been validated.</summary>
public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified);

/// <summary>Validates a Google ID token (from the "Sign in with Google" button) and returns its identity.</summary>
public interface IGoogleIdTokenValidator
{
    Task<GoogleIdentity> ValidateAsync(string? idToken, CancellationToken cancellationToken);
}

/// <summary>
/// Google sign-in configuration. Only the OAuth client id is needed: the browser gets an ID token
/// from Google and the server checks its signature, issuer, audience and lifetime. No client secret
/// is stored, because the server never exchanges authorization codes.
/// </summary>
public sealed record GoogleSignInOptions(string? ClientId)
{
    public const string SectionKey = "Identity:Google:ClientId";

    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId);

    public static GoogleSignInOptions FromConfiguration(IConfiguration configuration) => new(configuration[SectionKey]);
}

public sealed class GoogleIdTokenValidator(GoogleSignInOptions options) : IGoogleIdTokenValidator
{
    private static readonly string[] Issuers = ["https://accounts.google.com", "accounts.google.com"];

    // Google's signing keys rotate; the manager caches the discovery document and refreshes it.
    private static readonly ConfigurationManager<OpenIdConnectConfiguration> GoogleConfiguration = new(
        "https://accounts.google.com/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever { RequireHttps = true });

    private readonly JsonWebTokenHandler _handler = new();

    public async Task<GoogleIdentity> ValidateAsync(string? idToken, CancellationToken cancellationToken)
    {
        if (!options.Enabled)
            throw new IdentityException(404, "Google sign-in is not configured.");
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 8192)
            throw InvalidToken();

        var configuration = await GoogleConfiguration.GetConfigurationAsync(cancellationToken);
        var result = await _handler.ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuers = Issuers,
            ValidAudience = options.ClientId,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        });
        if (!result.IsValid)
            throw InvalidToken();

        var subject = result.Claims.TryGetValue("sub", out var sub) ? sub as string : null;
        var email = result.Claims.TryGetValue("email", out var mail) ? mail as string : null;
        var verified = result.Claims.TryGetValue("email_verified", out var flag) &&
                       (flag is true || string.Equals(flag as string, "true", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            throw InvalidToken();
        return new GoogleIdentity(subject, email, verified);
    }

    private static IdentityException InvalidToken() => new(401, "Google sign-in failed. Try again.");
}

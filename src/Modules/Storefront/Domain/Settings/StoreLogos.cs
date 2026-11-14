namespace HardwareStore.Modules.Storefront;

/// <summary>The two logo kinds: the one shown on the site, and the one drawn on photos (falls back to the store logo).</summary>
public static class StoreLogoKind
{
    public const string Store = "store";
    public const string Watermark = "watermark";

    /// <summary>The route value as a known kind, or a 404 (the route does not exist for other values).</summary>
    public static string Require(string kind) =>
        kind is Store or Watermark ? kind : throw new StorefrontException(404, "Unknown logo kind.");
}

/// <summary>
/// Storage keys of the uploaded logos. Not a settings section: <c>PUT settings/{section}</c> can never change them, only the logo
/// endpoints do.
/// </summary>
public sealed record StoreLogos
{
    public string? StoreLogoKey { get; init; }
    public string? WatermarkLogoKey { get; init; }

    public string? KeyOf(string kind) => StoreLogoKind.Require(kind) == StoreLogoKind.Store ? StoreLogoKey : WatermarkLogoKey;

    public StoreLogos With(string kind, string? key) =>
        StoreLogoKind.Require(kind) == StoreLogoKind.Store ? this with { StoreLogoKey = key } : this with { WatermarkLogoKey = key };

    /// <summary>The logo to draw on photos: the watermark logo, else the store logo.</summary>
    public string? WatermarkSourceKey => WatermarkLogoKey ?? StoreLogoKey;
}

/// <summary>The settings after a logo change, and the key of the logo it replaced (so the caller can delete that file).</summary>
public sealed record StoreLogoChange(StoreSettingsAdminView Settings, string? PreviousKey);

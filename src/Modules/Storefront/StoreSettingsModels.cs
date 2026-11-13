using HardwareStore.Common.Domain;
namespace HardwareStore.Modules.Storefront;

public sealed record StoreSettingsView(
    string? WhatsAppNumber,
    string? WhatsAppMessage,
    string? FacebookUrl,
    string? TelegramUrl,
    string? TikTokUrl,
    string? YouTubeUrl,
    string? InstagramUrl,
    string? PickupAddress,
    string? PickupHours,
    string? PickupInstructions,
    string? WarrantySummary,
    string? ServiceSummary,
    string? ConsultationSummary,
    long RowVersion);

public sealed record StoreSettingsUpdateRequest(
    string? WhatsAppNumber,
    string? WhatsAppMessage,
    string? FacebookUrl,
    string? TelegramUrl,
    string? TikTokUrl,
    string? YouTubeUrl,
    string? InstagramUrl,
    string? PickupAddress,
    string? PickupHours,
    string? PickupInstructions,
    string? WarrantySummary,
    string? ServiceSummary,
    string? ConsultationSummary,
    long RowVersion);

public sealed class StorefrontException(int statusCode, string message) : AppException(statusCode, message);

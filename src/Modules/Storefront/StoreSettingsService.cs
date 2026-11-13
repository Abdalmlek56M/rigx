using System.Text.Json;
using HardwareStore.Modules.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// Public storefront settings and their admin write path. Values are optional:
/// empty means the UI must hide the corresponding public entry rather than
/// inventing a phone number, address, social link, or service promise.
/// </summary>
public sealed class StoreSettingsService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    public async Task<StoreSettingsView> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.StoreSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return row is null ? Empty : Map(row);
    }

    public async Task<StoreSettingsView> UpdateAsync(
        StoreSettingsUpdateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.RowVersion < 0)
            throw new StorefrontException(400, "A non-negative settings row version is required.");

        var values = Normalize(request);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var row = await db.StoreSettings.SingleOrDefaultAsync(cancellationToken);
            if (row is null)
            {
                if (request.RowVersion != 0)
                    throw new StorefrontException(409, "The store settings changed by another user.");
                row = new StoreSettings
                {
                    Id = 1,
                    CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(),
                    RowVersion = 1
                };
                db.StoreSettings.Add(row);
            }
            else
            {
                if (request.RowVersion != row.RowVersion)
                    throw new StorefrontException(409, "The store settings changed by another user.");
                row.RowVersion++;
            }

            row.WhatsAppNumber = values.WhatsAppNumber;
            row.WhatsAppMessage = values.WhatsAppMessage;
            row.FacebookUrl = values.FacebookUrl;
            row.TelegramUrl = values.TelegramUrl;
            row.TikTokUrl = values.TikTokUrl;
            row.YouTubeUrl = values.YouTubeUrl;
            row.InstagramUrl = values.InstagramUrl;
            row.PickupAddress = values.PickupAddress;
            row.PickupHours = values.PickupHours;
            row.PickupInstructions = values.PickupInstructions;
            row.WarrantySummary = values.WarrantySummary;
            row.ServiceSummary = values.ServiceSummary;
            row.ConsultationSummary = values.ConsultationSummary;
            row.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            await db.SaveChangesAsync(cancellationToken);
            await auditWriter.WriteAsync(
                new SecurityAuditEntry(
                    "Storefront.SettingsUpdated",
                    "Succeeded",
                    "User",
                    ActorUserAccountId: actorUserId,
                    SafeMetadata: JsonSerializer.Serialize(new
                    {
                        whatsappConfigured = row.WhatsAppNumber is not null,
                        socialLinks = new[]
                        {
                            row.FacebookUrl, row.TelegramUrl, row.TikTokUrl,
                            row.YouTubeUrl, row.InstagramUrl
                        }.Count(value => value is not null),
                        pickupConfigured = row.PickupAddress is not null
                    })),
                db.Database.GetDbConnection(),
                transaction.GetDbTransaction(),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(row);
        }
        catch (StorefrontException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new StorefrontException(409, "The store settings changed by another user.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    private static NormalizedStoreSettings Normalize(StoreSettingsUpdateRequest request) =>
        new(
            NormalizeWhatsApp(request.WhatsAppNumber),
            Optional(request.WhatsAppMessage, 500, "WhatsApp message"),
            Url(request.FacebookUrl, "Facebook URL"),
            Url(request.TelegramUrl, "Telegram URL"),
            Url(request.TikTokUrl, "TikTok URL"),
            Url(request.YouTubeUrl, "YouTube URL"),
            Url(request.InstagramUrl, "Instagram URL"),
            Optional(request.PickupAddress, 500, "Pickup address"),
            Optional(request.PickupHours, 255, "Pickup hours"),
            Optional(request.PickupInstructions, 1000, "Pickup instructions"),
            Optional(request.WarrantySummary, 1000, "Warranty summary"),
            Optional(request.ServiceSummary, 1000, "Service summary"),
            Optional(request.ConsultationSummary, 1000, "Consultation summary"));

    private static string? NormalizeWhatsApp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length is < 8 or > 15)
            throw new StorefrontException(400, "WhatsApp number must contain 8 to 15 digits.");
        return digits;
    }

    private static string? Url(string? value, string label)
    {
        var normalized = Optional(value, 512, label);
        if (normalized is null)
            return null;
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new StorefrontException(400, $"{label} must be an absolute http or https URL.");
        return normalized;
    }

    private static string? Optional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (normalized.Length > maxLength)
            throw new StorefrontException(400, $"{label} is too long.");
        return normalized;
    }

    private static StoreSettingsView Map(StoreSettings item) => new(
        item.WhatsAppNumber,
        item.WhatsAppMessage,
        item.FacebookUrl,
        item.TelegramUrl,
        item.TikTokUrl,
        item.YouTubeUrl,
        item.InstagramUrl,
        item.PickupAddress,
        item.PickupHours,
        item.PickupInstructions,
        item.WarrantySummary,
        item.ServiceSummary,
        item.ConsultationSummary,
        item.RowVersion);

    private static readonly StoreSettingsView Empty = new(
        null, null, null, null, null, null, null,
        null, null, null, null, null, null, 0);

    private static async Task SafeRollbackAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
            // Preserve the original failure.
        }
    }

    private sealed record NormalizedStoreSettings(
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
        string? ConsultationSummary);
}

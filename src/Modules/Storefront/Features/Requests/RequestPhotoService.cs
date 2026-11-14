using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// Photos on a customer request. The customer proves access with the upload token from the submission receipt; staff read the photos
/// by request id. The image files themselves are stored by the host (private media); this service only keeps the keys.
/// </summary>
public sealed class RequestPhotoService(IDbContextFactory<StorefrontDbContext> contextFactory, TimeProvider timeProvider)
{
    private const int MaxTokenLength = 64;

    /// <summary>Checks the token and the photo limit before the image is processed; returns the request the photo will belong to.</summary>
    public async Task<Guid> AuthorizeAsync(string? token, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return RequestPhotoRules.EnsureCanAdd(await FindByTokenAsync(db, token, cancellationToken), Now()).Id;
    }

    /// <summary>Adds the stored photo key and returns how many photos the request has now; the checks run again because the request may have changed since <see cref="AuthorizeAsync"/>.</summary>
    public async Task<int> AttachAsync(string? token, string storageKey, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = Now();
        var request = RequestPhotoRules.EnsureCanAdd(await FindByTokenAsync(db, token, cancellationToken), now);
        request.Photos = [.. request.Photos, storageKey];
        request.UpdatedAtUtc = now;
        request.RowVersion++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new StorefrontException(409, "The request changed, try again.");
        }
        return request.Photos.Count;
    }

    public async Task<string> GetPhotoKeyAsync(Guid requestId, int index, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var photos = await db.CustomerRequests.AsNoTracking().Where(item => item.Id == requestId)
            .Select(item => item.Photos).SingleOrDefaultAsync(cancellationToken)
            ?? throw new StorefrontException(404, "Request was not found.");
        return index >= 0 && index < photos.Count ? photos[index] : throw new StorefrontException(404, "The photo was not found.");
    }

    private DateTimeOffset Now() => timeProvider.GetUtcNow().ToUniversalTime();

    private static async Task<CustomerRequest?> FindByTokenAsync(StorefrontDbContext db, string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength) return null;
        var hash = RequestPhotoRules.Hash(token.Trim());
        return await db.CustomerRequests.SingleOrDefaultAsync(item => item.PhotoTokenHash == hash, cancellationToken);
    }
}

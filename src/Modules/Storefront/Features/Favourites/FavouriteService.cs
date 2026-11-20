using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>A signed-in customer's favourites (RX codes). Host turns the codes into product cards.</summary>
public sealed class FavouriteService(IDbContextFactory<StorefrontDbContext> contextFactory, TimeProvider timeProvider)
{
    /// <summary>The account's codes, newest first.</summary>
    public async Task<IReadOnlyList<string>> ListCodesAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Favourites.AsNoTracking().Where(item => item.UserAccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc).ThenBy(item => item.ItemCode)
            .Select(item => item.ItemCode).ToListAsync(cancellationToken);
    }

    /// <summary>Adds one code; a code already saved changes nothing. 409 when the list is full.</summary>
    public async Task AddAsync(Guid accountId, string? itemCode, CancellationToken cancellationToken = default)
    {
        var code = FavouriteRules.Code(itemCode);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var saved = db.Favourites.Where(item => item.UserAccountId == accountId);
        if (await saved.AnyAsync(item => item.ItemCode == code, cancellationToken)) return;
        if (await saved.CountAsync(cancellationToken) >= FavouriteRules.Max)
            throw new StorefrontException(409, $"Favourites are limited to {FavouriteRules.Max} items.");
        await InsertAsync(db, accountId, [code], cancellationToken);
    }

    public async Task RemoveAsync(Guid accountId, string? itemCode, CancellationToken cancellationToken = default)
    {
        var code = FavouriteRules.Code(itemCode);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Favourites.Where(item => item.UserAccountId == accountId && item.ItemCode == code).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>Adds a guest's local list on sign-in: invalid codes are dropped, duplicates ignored, the list never passes the limit.</summary>
    public async Task MergeAsync(Guid accountId, IEnumerable<string?> itemCodes, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var saved = await db.Favourites.Where(item => item.UserAccountId == accountId).Select(item => item.ItemCode).ToListAsync(cancellationToken);
        var codes = FavouriteRules.NewCodes(itemCodes, saved);
        if (codes.Count > 0) await InsertAsync(db, accountId, [.. codes], cancellationToken);
    }

    /// <summary>Moves a merged account's favourites to the account that was kept; safe to run again.</summary>
    public async Task MoveAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        await MergeAsync(targetId, await ListCodesAsync(sourceId, cancellationToken), cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Favourites.Where(item => item.UserAccountId == sourceId).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>Insert that skips a code saved meanwhile (a double click), so it never fails on the key.</summary>
    private Task InsertAsync(StorefrontDbContext db, Guid accountId, string[] codes, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO favourites (user_account_id, item_code, created_at_utc) SELECT {accountId}, code, {now} FROM unnest({codes}) AS code ON CONFLICT DO NOTHING",
            cancellationToken);
    }
}

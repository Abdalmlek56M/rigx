using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Purchasing;

/// <summary>Sellers we buy from: search (phone first), create and edit. One seller per phone number.</summary>
public sealed class SellerService(IDbContextFactory<PurchasingDbContext> contextFactory, TimeProvider timeProvider)
{
    private const int MaxResults = 50;

    /// <summary>An exact <paramref name="phone"/> (any common spelling) or a text <paramref name="search"/> on name and phone.</summary>
    public async Task<IReadOnlyList<SellerView>> ListAsync(string? phone, string? search, bool includeNationalId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var sellers = db.Sellers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalized = PurchaseRules.NormalizePhone(phone);
            sellers = sellers.Where(seller => seller.Phone == normalized);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var namePattern = PurchasingQueries.ContainsPattern(search);
            var digits = string.Concat(search.Where(char.IsAsciiDigit));
            sellers = sellers.Where(seller => EF.Functions.ILike(seller.Name, namePattern)
                || (digits.Length >= 3 && seller.Phone.Contains(digits)));
        }
        var rows = await sellers.OrderBy(seller => seller.Name).ThenBy(seller => seller.Id).Take(MaxResults).ToListAsync(cancellationToken);
        return rows.Select(seller => ToView(seller, includeNationalId)).ToList();
    }

    public async Task<SellerView> GetAsync(Guid id, bool includeNationalId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var seller = await db.Sellers.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw NotFound();
        return ToView(seller, includeNationalId);
    }

    public async Task<SellerView> CreateAsync(SellerRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var seller = new Seller { Id = Guid.CreateVersion7(), Kind = string.Empty, Name = string.Empty, Phone = string.Empty, CreatedAtUtc = now };
        SellerRules.Apply(seller, request);
        seller.UpdatedAtUtc = now;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsurePhoneIsFreeAsync(db, seller.Phone, seller.Id, cancellationToken);
        db.Sellers.Add(seller);
        await db.SaveChangesAsync(cancellationToken);
        return ToView(seller, true);
    }

    public async Task<SellerView> UpdateAsync(Guid id, SellerRequest request, CancellationToken cancellationToken = default)
    {
        var rowVersion = request.RowVersion ?? throw new PurchasingException(400, "The seller version is required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var seller = await db.Sellers.SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw NotFound();
        if (seller.RowVersion != rowVersion) throw new PurchasingException(409, "The seller was changed by another user.");
        SellerRules.Apply(seller, request);
        await EnsurePhoneIsFreeAsync(db, seller.Phone, seller.Id, cancellationToken);
        seller.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        seller.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return ToView(seller, true);
    }

    private static async Task EnsurePhoneIsFreeAsync(PurchasingDbContext db, string phone, Guid sellerId, CancellationToken cancellationToken)
    {
        var existing = await db.Sellers.AsNoTracking().Where(row => row.Phone == phone && row.Id != sellerId)
            .Select(row => new { row.Id, row.Name }).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
            throw new PurchasingException(409, $"This phone number already belongs to the seller {existing.Name} ({existing.Id}).");
    }

    private static PurchasingException NotFound() => new(404, "Seller was not found.");

    private static SellerView ToView(Seller seller, bool includeNationalId) => new(seller.Id, seller.Kind, seller.Name, seller.Phone,
        includeNationalId ? seller.NationalId : null, seller.Governorate, seller.Area, seller.Source, seller.Notes,
        seller.CreatedAtUtc, seller.UpdatedAtUtc, seller.RowVersion);
}

using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Partner cards: list, detail with the parts it finances, create and update (one basis per partner).</summary>
public sealed class PartnerService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    public async Task<IReadOnlyList<PartnerView>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await PartnerViews.BuildAsync(db, await db.Partners.AsNoTracking().OrderBy(partner => partner.Name).ToListAsync(cancellationToken),
            cancellationToken);
    }

    public async Task<PartnerDetailView> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var partner = await db.Partners.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "The partner was not found.");
        var parts = await db.Units.AsNoTracking().Include(unit => unit.StockItem)
            .Where(unit => unit.PartnerId == id && unit.Status != UnitStatus.Sold && unit.Status != UnitStatus.Scrapped)
            .OrderBy(unit => unit.ReceivedAtUtc).ThenBy(unit => unit.Id).ToListAsync(cancellationToken);
        return new PartnerDetailView(await PartnerViews.BuildOneAsync(db, partner, cancellationToken), parts.Select(unit =>
            new FinancedPartView(unit.Id, unit.StockItem.Code, unit.Name, unit.Status, unit.AcquisitionCost,
                PartnerRules.EffectivePercent(unit.PartnerPercentOverride, partner.DefaultPercent), unit.PartnerPercentOverride is not null,
                unit.AssemblyId, unit.ReceivedAtUtc)).ToList());
    }

    /// <summary>Early check for callers that save a financing partner before the unit exists (purchase drafts).</summary>
    public async Task EnsureCanFinanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await PartnerFinancing.EnsureAsync(db, id, null, cancellationToken);
    }

    public async Task<PartnerView> CreateAsync(PartnerRequest request, CancellationToken cancellationToken = default)
    {
        var (name, phone, notes) = PartnerRules.CleanCard(request.Name, request.Phone, request.DefaultPercent, request.Basis, request.Notes);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Partners.AnyAsync(item => item.Name == name, cancellationToken)) throw NameTaken();
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var partner = new Partner
        {
            Id = Guid.CreateVersion7(), Name = name, Phone = phone, Basis = request.Basis!,
            JoinDate = request.JoinDate ?? BusinessDay.Of(now, businessZone), DefaultPercent = request.DefaultPercent,
            LossSharingEnabled = request.LossSharingEnabled, IsActive = request.IsActive, Notes = notes,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
        await EnsureWholeShopRoomAsync(db, partner, cancellationToken);
        db.Partners.Add(partner);
        await SaveAsync(db, cancellationToken);
        return await PartnerViews.BuildOneAsync(db, partner, cancellationToken);
    }

    public async Task<PartnerView> UpdateAsync(Guid id, PartnerRequest request, CancellationToken cancellationToken = default)
    {
        var (name, phone, notes) = PartnerRules.CleanCard(request.Name, request.Phone, request.DefaultPercent, request.Basis, request.Notes);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var partner = await db.Partners.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "The partner was not found.");
        if (request.RowVersion != partner.RowVersion) throw new RetailException(409, "The partner was changed by another user.");
        if (await db.Partners.AnyAsync(item => item.Id != id && item.Name == name, cancellationToken)) throw NameTaken();
        var unsold = await db.Units.CountAsync(unit => unit.PartnerId == id && unit.Status != UnitStatus.Sold && unit.Status != UnitStatus.Scrapped,
            cancellationToken);
        PartnerRules.EnsureBasisChange(partner.Basis, request.Basis!, unsold);
        partner.Name = name; partner.Phone = phone; partner.Notes = notes; partner.Basis = request.Basis!;
        partner.JoinDate = request.JoinDate ?? partner.JoinDate; partner.DefaultPercent = request.DefaultPercent;
        partner.LossSharingEnabled = request.LossSharingEnabled; partner.IsActive = request.IsActive;
        partner.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        partner.RowVersion++;
        await EnsureWholeShopRoomAsync(db, partner, cancellationToken);
        await SaveAsync(db, cancellationToken);
        return await PartnerViews.BuildOneAsync(db, partner, cancellationToken);
    }

    /// <summary>Whole-shop partners share one month base, so their percents together stay within 100%.</summary>
    private static async Task EnsureWholeShopRoomAsync(RetailDbContext db, Partner partner, CancellationToken cancellationToken)
    {
        if (!partner.IsActive || partner.Basis != PartnerBasis.WholeShop) return;
        var others = await db.Partners.AsNoTracking()
            .Where(item => item.Id != partner.Id && item.IsActive && item.Basis == PartnerBasis.WholeShop)
            .SumAsync(item => item.DefaultPercent, cancellationToken);
        PartnerRules.EnsureWholeShopTotal(others, partner.DefaultPercent);
    }

    private static async Task SaveAsync(RetailDbContext db, CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new RetailException(409, "The partner was changed by another user."); }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation()) { throw NameTaken(); }
    }

    private static RetailException NameTaken() => new(409, "A partner with this name already exists.");
}

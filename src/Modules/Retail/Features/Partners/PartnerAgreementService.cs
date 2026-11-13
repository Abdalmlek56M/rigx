using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Partner default agreements (default percent, dates, loss sharing).
/// </summary>
public sealed class PartnerAgreementService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PartnerAgreement>> ListAgreementsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.PartnerAgreements.AsNoTracking().OrderByDescending(item => item.EffectiveFrom).ToListAsync(cancellationToken);
    }

    public async Task<PartnerAgreement> UpsertAgreementAsync(PartnerAgreementRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerName) || request.DefaultPercent is < 0 or > 100)
            throw new RetailException(400, "Partner name and a percentage between 0 and 100 are required.");
        if (request.EffectiveTo is not null && request.EffectiveTo < request.EffectiveFrom)
            throw new RetailException(400, "The agreement end date cannot precede its start date.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var agreement = await db.PartnerAgreements.SingleOrDefaultAsync(item => item.PartnerName == request.PartnerName.Trim(), cancellationToken);
        if (agreement is null)
        {
            agreement = new PartnerAgreement { Id = Guid.CreateVersion7(), PartnerName = request.PartnerName.Trim(), CreatedAtUtc = now };
            db.PartnerAgreements.Add(agreement);
        }
        agreement.DefaultPercent = request.DefaultPercent; agreement.IsActive = request.IsActive;
        agreement.LossSharingEnabled = request.LossSharingEnabled;
        agreement.EffectiveFrom = request.EffectiveFrom; agreement.EffectiveTo = request.EffectiveTo;
        agreement.UpdatedAtUtc = now; agreement.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return agreement;
    }
}

using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>The one check for giving a unit a financing partner (receipt line, build part edit, unit assignment).</summary>
internal static class PartnerFinancing
{
    /// <summary>A percent needs a partner; the partner must exist and finance parts; the percent must be 0..100.</summary>
    public static async Task EnsureAsync(RetailDbContext db, Guid? partnerId, decimal? percent, CancellationToken cancellationToken)
    {
        if (percent is not null) PartnerRules.EnsurePercent(percent.Value);
        if (partnerId is null)
        {
            if (percent is not null) throw new RetailException(400, "Choose the financing partner for the partner percentage.");
            return;
        }
        var partner = await db.Partners.AsNoTracking().SingleOrDefaultAsync(item => item.Id == partnerId, cancellationToken)
            ?? throw new RetailException(404, "The partner was not found.");
        PartnerRules.EnsureCanFinance(partner);
    }
}

using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Trash;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Trashed blacklist entries. A restored entry matches new orders again at once.</summary>
public sealed class BlacklistTrashSource(IDbContextFactory<RetailDbContext> contextFactory)
    : SoftDeleteTrashSource<RetailDbContext, BlacklistEntry>(contextFactory)
{
    public const string TrashKind = "blacklist";

    public override string Kind => TrashKind;

    protected override DbSet<BlacklistEntry> Set(RetailDbContext db) => db.Blacklist;

    protected override string Label(BlacklistEntry item) => item.Name ?? item.PhoneNumber ?? item.Email ?? item.AddressText ?? item.Reason;

    protected override AppException NotFound() => new RetailException(404, "Blacklist entry was not found in the trash.");
}

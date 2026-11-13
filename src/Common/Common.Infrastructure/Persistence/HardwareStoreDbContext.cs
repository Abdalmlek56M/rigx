using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Common.Infrastructure.Persistence;

/// <summary>
/// Foundation-only context. Commercial entities and migrations belong to their approved milestones.
/// </summary>
public sealed class HardwareStoreDbContext(DbContextOptions<HardwareStoreDbContext> options)
    : DbContext(options)
{
}

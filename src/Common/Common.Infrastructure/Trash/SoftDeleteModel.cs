using System.Reflection;
using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Common.Infrastructure.Trash;

/// <summary>
/// The one EF mapping of <see cref="ISoftDeletable"/>: the two columns and a global query filter that hides trashed rows.
/// Call it last in <c>OnModelCreating</c>. Trash code reads trashed rows with <c>IgnoreQueryFilters()</c>.
/// </summary>
public static class SoftDeleteModel
{
    /// <summary>The partial-index condition that makes unique indexes ignore trashed rows.</summary>
    public const string LiveFilter = "deleted_at_utc IS NULL";

    public static ModelBuilder ApplySoftDelete(this ModelBuilder model)
    {
        var configure = typeof(SoftDeleteModel).GetMethod(nameof(Configure), BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var clrType in model.Model.GetEntityTypes().Select(type => type.ClrType)
                     .Where(type => typeof(ISoftDeletable).IsAssignableFrom(type)).ToList())
            configure.MakeGenericMethod(clrType).Invoke(null, [model]);
        return model;
    }

    private static void Configure<T>(ModelBuilder model) where T : class, ISoftDeletable
    {
        var entity = model.Entity<T>();
        entity.Property(item => item.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamp with time zone");
        entity.Property(item => item.DeletedByUserId).HasColumnName("deleted_by_user_id");
        entity.HasQueryFilter(item => item.DeletedAtUtc == null);
    }
}

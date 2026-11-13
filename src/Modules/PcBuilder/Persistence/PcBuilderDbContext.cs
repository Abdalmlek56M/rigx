using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HardwareStore.Modules.PcBuilder;

public sealed class PcBuilderDbContext(DbContextOptions<PcBuilderDbContext> options) : DbContext(options)
{
    public DbSet<SavedBuild> SavedBuilds => Set<SavedBuild>();

    protected override void OnModelCreating(ModelBuilder model) =>
        model.Entity<SavedBuild>(e =>
        {
            e.ToTable("saved_builds");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Token).HasColumnName("token").HasMaxLength(32).IsRequired();
            e.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(SavedBuildRules.MaxNameLength);
            e.Property(x => x.Usage).HasColumnName("usage").HasMaxLength(16);
            e.Property(x => x.Parts).HasColumnName("parts").HasColumnType("jsonb").IsRequired()
                .HasConversion(
                    new ValueConverter<List<SavedBuildPart>, string>(value => ToJson(value), text => FromJson(text)),
                    new ValueComparer<List<SavedBuildPart>>(
                        (left, right) => ToJson(left!) == ToJson(right!), value => ToJson(value).GetHashCode(), value => value.ToList()));
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => x.Token).IsUnique().HasDatabaseName("ux_saved_builds_token");
            e.HasIndex(x => x.UserAccountId).HasDatabaseName("ix_saved_builds_user_account_id");
        });

    private static string ToJson(List<SavedBuildPart> parts) => JsonSerializer.Serialize(parts);

    private static List<SavedBuildPart> FromJson(string text) => JsonSerializer.Deserialize<List<SavedBuildPart>>(text) ?? [];
}

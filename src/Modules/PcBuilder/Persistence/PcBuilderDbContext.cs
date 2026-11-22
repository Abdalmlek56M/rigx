using HardwareStore.Common.Infrastructure.Audit;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HardwareStore.Modules.PcBuilder;

public sealed class PcBuilderDbContext(DbContextOptions<PcBuilderDbContext> options) : DbContext(options)
{
    public DbSet<SavedBuild> SavedBuilds => Set<SavedBuild>();
    public DbSet<BuildModeration> BuildModerations => Set<BuildModeration>();
    public DbSet<BuildReaction> BuildReactions => Set<BuildReaction>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SavedBuild>(e =>
        {
            e.ToTable("saved_builds");
            e.ExcludeFromAudit();
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Token).HasColumnName("token").HasMaxLength(32).IsRequired();
            e.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(SavedBuildRules.MaxNameLength);
            e.Property(x => x.Usage).HasColumnName("usage").HasMaxLength(16);
            e.Property(x => x.Visibility).HasColumnName("visibility").HasConversion<string>().HasMaxLength(16);
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
            e.HasIndex(x => x.Visibility).HasDatabaseName("ix_saved_builds_visibility");
            e.HasOne(x => x.Moderation).WithOne().HasForeignKey<BuildModeration>(x => x.SavedBuildId).OnDelete(DeleteBehavior.Cascade);
        });

        // Audited (the default): the log shows who approved or rejected which build.
        model.Entity<BuildModeration>(e =>
        {
            e.ToTable("saved_build_moderations");
            e.HasKey(x => x.SavedBuildId);
            e.Property(x => x.SavedBuildId).HasColumnName("saved_build_id").ValueGeneratedNever();
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.SubmittedAtUtc).HasColumnName("submitted_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.DecidedByUserId).HasColumnName("decided_by_user_id");
            e.Property(x => x.DecidedAtUtc).HasColumnName("decided_at_utc").HasColumnType("timestamp with time zone");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_saved_build_moderations_status");
        });

        // High volume and one click each: not audited.
        model.Entity<BuildReaction>(e =>
        {
            e.ToTable("saved_build_reactions");
            e.ExcludeFromAudit();
            e.HasKey(x => new { x.SavedBuildId, x.UserAccountId, x.Kind });
            e.Property(x => x.SavedBuildId).HasColumnName("saved_build_id");
            e.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            e.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.HasOne<SavedBuild>().WithMany().HasForeignKey(x => x.SavedBuildId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserAccountId, x.Kind }).HasDatabaseName("ix_saved_build_reactions_user_kind");
        });
    }

    private static string ToJson(List<SavedBuildPart> parts) => JsonSerializer.Serialize(parts);

    private static List<SavedBuildPart> FromJson(string text) => JsonSerializer.Deserialize<List<SavedBuildPart>>(text) ?? [];
}

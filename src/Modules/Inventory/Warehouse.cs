using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Inventory;

public sealed class Warehouse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? ManagerName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
            entity.HasIndex(item => item.Code).IsUnique().HasDatabaseName("ux_warehouses_code");
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
            entity.Property(item => item.Address).HasColumnName("address").HasMaxLength(255);
            entity.Property(item => item.ContactPhone).HasColumnName("contact_phone").HasMaxLength(32);
            entity.Property(item => item.ManagerName).HasColumnName("manager_name").HasMaxLength(128);
            entity.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(1000);
            entity.Property(item => item.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(item => item.IsActive).HasDatabaseName("ix_warehouses_is_active");
        });
    }
}

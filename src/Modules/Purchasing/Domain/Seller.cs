namespace HardwareStore.Modules.Purchasing;

/// <summary>Someone we buy from: a person, a trader or a company. One record per phone number.</summary>
public sealed class Seller
{
    public Guid Id { get; set; }
    public required string Kind { get; set; }
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public string? NationalId { get; set; }
    public string? Governorate { get; set; }
    public string? Area { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

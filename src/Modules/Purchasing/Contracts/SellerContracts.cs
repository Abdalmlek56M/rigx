namespace HardwareStore.Modules.Purchasing;

/// <summary>Create/update body; <paramref name="RowVersion"/> is required on update only.</summary>
public sealed record SellerRequest(string? Kind, string? Name, string? Phone, string? NationalId, string? Governorate,
    string? Area, string? Source, string? Notes, long? RowVersion = null);

/// <summary><paramref name="NationalId"/> is internal: null unless the caller may manage suppliers.</summary>
public sealed record SellerView(Guid Id, string Kind, string Name, string Phone, string? NationalId, string? Governorate,
    string? Area, string? Source, string? Notes, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion);

public sealed record SellerSummary(Guid Id, string Kind, string Name, string Phone);

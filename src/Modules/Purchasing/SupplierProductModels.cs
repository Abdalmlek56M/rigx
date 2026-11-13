namespace HardwareStore.Modules.Purchasing;

public sealed record SupplierProductPage(
    IReadOnlyList<SupplierProductView> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record SupplierProductView(
    Guid Id,
    Guid SupplierProfileId,
    Guid ProductVariantId,
    Guid ProductId,
    string ProductName,
    string VariantName,
    string VariantSku,
    string SupplierSku,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record SupplierProductCreateRequest(
    Guid ProductVariantId,
    string? SupplierSku,
    string? Notes,
    bool IsActive);

public sealed record SupplierProductUpdateRequest(
    string? SupplierSku,
    string? Notes,
    bool IsActive,
    long RowVersion);

public sealed record SupplierVariantOption(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string VariantName,
    string VariantSku);

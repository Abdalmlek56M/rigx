using HardwareStore.Common.Domain;
namespace HardwareStore.Modules.Inventory;

public sealed record WarehouseView(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string? ContactPhone,
    string? ManagerName,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record WarehouseCreateRequest(
    string? Code,
    string? Name,
    string? Address,
    string? ContactPhone,
    string? ManagerName,
    string? Notes,
    bool IsActive);

public sealed record WarehouseUpdateRequest(
    string? Code,
    string? Name,
    string? Address,
    string? ContactPhone,
    string? ManagerName,
    string? Notes,
    bool IsActive,
    long RowVersion);

public sealed class WarehouseDirectoryException(int statusCode, string message) : AppException(statusCode, message);

namespace HardwareStore.Modules.Purchasing;

public sealed record SupplierPage(
    IReadOnlyList<SupplierView> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record SupplierView(
    Guid Id,
    Guid PersonId,
    string PersonCode,
    string CompanyName,
    string? PhoneNumber,
    string? Email,
    string? TaxIdNumber,
    string? CommercialRegistration,
    string? ContactPersonName,
    string? ContactPersonPhone,
    int PaymentTermsDays,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record SupplierFinancialView(
    Guid SupplierId,
    string? BankName,
    string? BankAccountNumber,
    string? BankIban,
    long RowVersion);

public sealed record SupplierCreateRequest(
    string? CompanyName,
    string? PhoneNumber,
    string? Email,
    string? TaxIdNumber,
    string? CommercialRegistration,
    string? ContactPersonName,
    string? ContactPersonPhone,
    int PaymentTermsDays,
    string? Notes,
    bool IsActive);

public sealed record SupplierUpdateRequest(
    string? CompanyName,
    string? PhoneNumber,
    string? Email,
    string? TaxIdNumber,
    string? CommercialRegistration,
    string? ContactPersonName,
    string? ContactPersonPhone,
    int PaymentTermsDays,
    string? Notes,
    bool IsActive,
    long RowVersion);

public sealed record SupplierFinancialWriteRequest(
    string? BankName,
    string? BankAccountNumber,
    string? BankIban,
    long RowVersion);

public sealed class SupplierDirectoryException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

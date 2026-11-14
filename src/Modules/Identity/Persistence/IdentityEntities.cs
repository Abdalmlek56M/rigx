namespace HardwareStore.Modules.Identity.Persistence;

public sealed class Person
{
    public Guid Id { get; set; }
    public string PersonCode { get; private set; } = string.Empty;
    public required string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsCustomer { get; set; }
    public bool IsSupplier { get; set; }
    public bool IsEmployee { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Staff-only note about a customer (customer admin).</summary>
    public string? CustomerNote { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public UserAccount? UserAccount { get; set; }

    internal void AssignPersonCode(string personCode) => PersonCode = personCode;
}

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public required string Username { get; set; }
    public string NormalizedUsername { get; set; } = string.Empty;
    public required string Email { get; set; }
    public string NormalizedEmail { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string? AvatarStorageKey { get; set; }
    public bool IsActive { get; set; } = true;
    public bool NotifyNewDeviceSignIn { get; set; } = true;
    public int AuthorizationVersion { get; set; }
    /// <summary>Set when this customer account was merged into another one; the account is then inactive.</summary>
    public Guid? MergedIntoAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Person Person { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>A one-time 6-digit code sent by email (sign-in or step-up). Only the protected code is stored.</summary>
public sealed class EmailCode
{
    public Guid Id { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string Purpose { get; set; }
    public byte[] ProtectedCode { get; set; } = [];
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

public sealed class AuthSession
{
    public byte[] SessionKeyHash { get; set; } = [];
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid UserAccountId { get; set; }
    public byte[] ProtectedTicket { get; set; } = [];
    public string? UserAgent { get; set; }
    // Legacy compatibility only. New sessions do not collect raw IP addresses.
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastActivityAtUtc { get; set; }
    public DateTimeOffset IdleExpiresAtUtc { get; set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokeReason { get; set; }
    public int IdleTimeoutSeconds { get; set; }
    public int AuthorizationVersion { get; set; }
    public long RbacRevision { get; set; }
    public long RowVersion { get; set; }
}

public sealed class IdentityEmailOutboxMessage
{
    public Guid Id { get; set; }
    public Guid? UserAccountId { get; set; }
    public required string Kind { get; set; }
    public byte[] ProtectedPayload { get; set; } = [];
    public required string Status { get; set; } = IdentityEmailOutboxStatus.Pending;
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAtUtc { get; set; }
    public DateTimeOffset? ProcessingAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public static class IdentityEmailOutboxStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string DeadLetter = "DeadLetter";
}

public sealed class IdentityEmailChangeRequest
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public required string NewEmail { get; set; }
    public required string NormalizedNewEmail { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public static class AccountRestrictionType
{
    public const string Suspension = "Suspension";
    public const string Ban = "Ban";
}

public sealed class AccountRestriction
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public required string Type { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedByUserAccountId { get; set; }
    public string? ReinstatementReason { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class UserNotification
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public required string Kind { get; set; }
    public required string TitleKey { get; set; }
    public string? BodyKey { get; set; }
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
}

public sealed class CustomerAddress
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public required string Label { get; set; }
    public required string RecipientName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CountryCode { get; set; }
    public required string CountryCallingCode { get; set; }
    public required string Governorate { get; set; }
    public required string City { get; set; }
    public string? District { get; set; }
    public required string Street { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Landmark { get; set; }
    public string? PostalCode { get; set; }
    public string? Instructions { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class AuthorizationState
{
    public short Id { get; set; }
    public long RbacRevision { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class Role
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class Permission
{
    public required string Code { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class UserRole
{
    public Guid UserAccountId { get; set; }
    public Guid RoleId { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public required string PermissionCode { get; set; }
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

public sealed class PersonCodeCounter
{
    public required string YearMonth { get; set; }
    public int LastValue { get; set; }
}

/// <summary>An outside sign-in (today only Google) linked to one account. One link per provider per account.</summary>
public sealed class ExternalLogin
{
    public required string Provider { get; set; }
    public required string Subject { get; set; }
    public Guid UserAccountId { get; set; }
    public required string Email { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

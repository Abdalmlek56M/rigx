using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Modules.Identity;

public enum FirstOwnerBootstrapFailure
{
    None,
    InvalidInput,
    PasswordPolicy,
    AlreadyInitialized,
    IdentityCollision,
    Failed
}

public sealed class FirstOwnerBootstrapException : Exception
{
    public FirstOwnerBootstrapException(FirstOwnerBootstrapFailure failure, string message, Exception? inner = null)
        : base(message, inner)
    {
        Failure = failure;
    }

    public FirstOwnerBootstrapFailure Failure { get; }
}

public sealed record FirstOwnerBootstrapRequest(
    string Username,
    string Email,
    string FullName,
    string Password);

public sealed record FirstOwnerBootstrapResult(Guid UserAccountId);

/// <summary>
/// One-time, local first-owner bootstrap.
///
/// This service is deliberately not exposed through a public endpoint. It is
/// invoked only by the explicit <c>--bootstrap-owner</c> command, requires an
/// operator-supplied password, runs once, and records the success audit row in
/// the same database transaction as the Owner role/account/permissions.
/// </summary>
public sealed class FirstOwnerBootstrapService(
    IDbContextFactory<IdentityDbContext> identityFactory,
    IDbContextFactory<SecurityAuditDbContext> auditFactory,
    ISecurityAuditWriter auditWriter,
    IPasswordHashingService passwordHashing,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone,
    ILogger<FirstOwnerBootstrapService> logger)
{
    // A stable session-level advisory lock serializes concurrent bootstrap attempts.
    private const long AdvisoryLockKey = 0x524947584F574E; // RIGXOWN

    public async Task<FirstOwnerBootstrapResult> BootstrapAsync(
        FirstOwnerBootstrapRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAndValidate(request);
        if (normalized is null)
            throw new FirstOwnerBootstrapException(
                FirstOwnerBootstrapFailure.InvalidInput,
                "Owner bootstrap input is invalid.");

        await using var db = await identityFactory.CreateDbContextAsync(cancellationToken);
        await using var audit = await auditFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0})",
                new object[] { AdvisoryLockKey },
                cancellationToken);

            // Auditing shares the identity connection and transaction. A success
            // audit insert failure therefore rolls the Owner changes back too.
            audit.Database.SetDbConnection(db.Database.GetDbConnection());
            await audit.Database.UseTransactionAsync(
                transaction.GetDbTransaction(),
                cancellationToken);

            var alreadyInitialized =
                await db.Roles.AnyAsync(role => role.Code == "Owner", cancellationToken) ||
                await audit.SecurityAuditEvents.AnyAsync(
                    entry => entry.EventType == "Owner.Bootstrapped" && entry.Outcome == "Succeeded",
                    cancellationToken);
            if (alreadyInitialized)
                throw new FirstOwnerBootstrapException(
                    FirstOwnerBootstrapFailure.AlreadyInitialized,
                    "Owner bootstrap has already been completed.");

            var identityCollision = await db.UserAccounts.AnyAsync(
                account => account.NormalizedUsername == normalized.NormalizedUsername ||
                           account.NormalizedEmail == normalized.NormalizedEmail,
                cancellationToken);
            if (identityCollision)
                throw new FirstOwnerBootstrapException(
                    FirstOwnerBootstrapFailure.IdentityCollision,
                    "The owner username or email is already in use.");

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            await EnsurePermissionDefinitionsAsync(db, cancellationToken);

            var ownerRole = new Role
            {
                Id = Guid.CreateVersion7(),
                Code = "Owner",
                Name = "Owner"
            };
            db.Roles.Add(ownerRole);
            foreach (var permission in PermissionCatalog.All.OrderBy(code => code, StringComparer.Ordinal))
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = ownerRole.Id,
                    PermissionCode = permission
                });
            }

            var person = new Person
            {
                Id = Guid.CreateVersion7(),
                FullName = normalized.FullName,
                Email = normalized.Email,
                IsEmployee = true,
                IsActive = true
            };
            await new PersonCodeGenerator(db, timeProvider, businessTimeZone)
                .CreatePersonInCurrentTransactionAsync(person, cancellationToken);

            var account = new UserAccount
            {
                Id = Guid.CreateVersion7(),
                PersonId = person.Id,
                Person = person,
                Username = normalized.Username,
                NormalizedUsername = normalized.NormalizedUsername,
                Email = normalized.Email,
                NormalizedEmail = normalized.NormalizedEmail,
                IsEmailVerified = true,
                IsActive = true,
                AuthorizationVersion = 1,
                CreatedAtUtc = now
            };
            account.PasswordHash = passwordHashing.HashPassword(account, request.Password);
            db.UserAccounts.Add(account);
            await new UserAccountLoginIdentifierSynchronizer(db)
                .SetAsync(account, normalized.Username, normalized.Email, cancellationToken);
            db.UserRoles.Add(new UserRole
            {
                UserAccountId = account.Id,
                RoleId = ownerRole.Id
            });

            var state = await db.AuthorizationStates.SingleAsync(cancellationToken);
            state.RbacRevision++;
            state.UpdatedAtUtc = now;

            await db.SaveChangesAsync(cancellationToken);

            audit.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                EventType = "Owner.Bootstrapped",
                OccurredAtUtc = now,
                Outcome = "Succeeded",
                ActorKind = "System",
                SubjectUserAccountId = account.Id,
                SafeMetadata = JsonSerializer.Serialize(new
                {
                    source = "FirstOwnerBootstrap",
                    ownershipProof = "LocalOperator",
                    emailVerifiedByOperator = true
                })
            });
            await audit.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("First Owner bootstrap completed.");
            return new FirstOwnerBootstrapResult(account.Id);
        }
        catch (Exception exception)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            var failure = exception is FirstOwnerBootstrapException known
                ? known.Failure
                : FirstOwnerBootstrapFailure.Failed;
            await TryWriteFailureAuditAsync(failure, cancellationToken);
            if (exception is FirstOwnerBootstrapException)
                throw;
            throw new FirstOwnerBootstrapException(
                FirstOwnerBootstrapFailure.Failed,
                "Owner bootstrap failed before commit.",
                exception);
        }
    }

    public static void ValidateRequest(FirstOwnerBootstrapRequest request)
    {
        if (NormalizeAndValidate(request) is null)
            throw new FirstOwnerBootstrapException(
                FirstOwnerBootstrapFailure.InvalidInput,
                "Owner bootstrap input is invalid.");
    }

    private static NormalizedOwner? NormalizeAndValidate(FirstOwnerBootstrapRequest request)
    {
        var username = NormalizeOriginal(request.Username);
        var email = NormalizeOriginal(request.Email);
        var fullName = NormalizeOriginal(request.FullName);
        if (username is null || username.Length > 128 ||
            email is null || email.Length > 255 || !new EmailAddressAttribute().IsValid(email) ||
            fullName is null || fullName.Length > 255 ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var normalizedUsername = IdentityLoginService.NormalizeIdentifier(username);
        var normalizedEmail = IdentityLoginService.NormalizeIdentifier(email);
        if (normalizedUsername.Length > 128 || normalizedEmail.Length > 255)
            return null;

        if (new PasswordPolicyValidator().Validate(request.Password) != PasswordPolicyFailure.None)
            throw new FirstOwnerBootstrapException(
                FirstOwnerBootstrapFailure.PasswordPolicy,
                "Owner password does not satisfy the active password policy.");

        return new NormalizedOwner(username, normalizedUsername, email, normalizedEmail, fullName);
    }

    private static string? NormalizeOriginal(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Normalize(NormalizationForm.FormC);

    private static async Task EnsurePermissionDefinitionsAsync(
        IdentityDbContext db,
        CancellationToken cancellationToken)
    {
        var existing = await db.Permissions
            .Select(permission => permission.Code)
            .ToListAsync(cancellationToken);
        var missing = PermissionCatalog.All
            .Except(existing, StringComparer.Ordinal)
            .ToArray();
        foreach (var code in missing)
            db.Permissions.Add(new Permission { Code = code });
    }

    private static async Task SafeRollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
            // The original failure is more useful than a secondary rollback error.
        }
    }

    private async Task TryWriteFailureAuditAsync(
        FirstOwnerBootstrapFailure failure,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditWriter.WriteAsync(
                new SecurityAuditEntry(
                    "Owner.BootstrapFailed",
                    "Failed",
                    "System",
                    SafeMetadata: JsonSerializer.Serialize(new
                    {
                        failure = failure.ToString()
                    })),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not write the Owner bootstrap failure audit entry.");
        }
    }

    private sealed record NormalizedOwner(
        string Username,
        string NormalizedUsername,
        string Email,
        string NormalizedEmail,
        string FullName);
}

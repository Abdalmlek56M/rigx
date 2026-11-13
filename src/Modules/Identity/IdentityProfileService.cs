using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Identity;

public sealed record UpdateIdentityProfileRequest(string? FullName, string? PhoneNumber);

public sealed class IdentityProfileService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter)
{
    public async Task UpdateAsync(
        Guid accountId,
        UpdateIdentityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            throw new IdentityException(401, "The account is not authenticated.");
        var fullName = IdentityAdministrationSupport.NormalizeRequired(request.FullName, 255, "Full name");
        var phoneNumber = IdentityAdministrationSupport.NormalizeOptional(request.PhoneNumber, 32, "Phone number");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await db.UserAccounts
                .Include(item => item.Person)
                .SingleOrDefaultAsync(item => item.Id == accountId && item.IsActive, cancellationToken)
                ?? throw new IdentityException(401, "The account is not active or does not exist.");
            var changed =
                !string.Equals(account.Person.FullName, fullName, StringComparison.Ordinal) ||
                !string.Equals(account.Person.PhoneNumber, phoneNumber, StringComparison.Ordinal);
            account.Person.FullName = fullName;
            account.Person.PhoneNumber = phoneNumber;
            await db.SaveChangesAsync(cancellationToken);
            await auditWriter.WriteAsync(
                new SecurityAuditEntry(
                    "Identity.ProfileUpdated",
                    "Succeeded",
                    "User",
                    ActorUserAccountId: accountId,
                    SubjectUserAccountId: accountId,
                    SafeMetadata: JsonSerializer.Serialize(new { changed })),
                db.Database.GetDbConnection(),
                transaction.GetDbTransaction(),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (IdentityException)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }
}

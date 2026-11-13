using HardwareStore.Common.Domain;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

/// <summary>
/// Returned when a sensitive action needs a fresh email code. The <c>stepUpCode</c> field tells the
/// UI to ask for the code (and how many tries are left) instead of showing a permission error.
/// </summary>
public sealed class StepUpRequiredException(string detail)
    : AppException(403, "Confirm this action with the code sent to your email.",
        new Dictionary<string, string[]> { [StepUpService.CodeField] = [detail] });

/// <summary>
/// Step-up confirmation (security rule 9): a destructive action carries a fresh 6-digit code that was
/// emailed to the signed-in user. Each code confirms one action; the code rules are those of
/// <see cref="EmailCodeService"/>.
/// </summary>
public sealed class StepUpService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    EmailCodeService codes)
{
    /// <summary>Request header that carries the code.</summary>
    public const string HeaderName = "X-Step-Up-Code";
    public const string CodeField = "stepUpCode";

    public async Task<EmailCodeTicket> RequestAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await FindActiveAsync(db, accountId, cancellationToken);
        return await codes.IssueAsync(db,
            new EmailCodeRecipient(account.Email, account.NormalizedEmail, account.FullName, account.Id),
            EmailCodePurpose.StepUp, cancellationToken);
    }

    /// <summary>Consumes the code or throws <see cref="StepUpRequiredException"/>.</summary>
    public async Task ConfirmAsync(Guid accountId, string? code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new StepUpRequiredException("A code is required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await FindActiveAsync(db, accountId, cancellationToken);
        var check = await codes.VerifyAsync(db, account.NormalizedEmail, code.Trim(), EmailCodePurpose.StepUp, cancellationToken);
        if (!check.Succeeded)
            throw new StepUpRequiredException(check.AttemptsLeft > 0
                ? $"The code is wrong. {check.AttemptsLeft} tries left."
                : "The code is wrong or expired. Request a new one.");
    }

    private static async Task<StepUpAccount> FindActiveAsync(
        IdentityDbContext db, Guid accountId, CancellationToken cancellationToken) =>
        await db.UserAccounts.AsNoTracking()
            .Where(item => item.Id == accountId && item.IsActive)
            .Select(item => new StepUpAccount(item.Id, item.Email, item.NormalizedEmail, item.Person.FullName))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new IdentityException(401, "The account is not active.");

    private sealed record StepUpAccount(Guid Id, string Email, string NormalizedEmail, string FullName);
}

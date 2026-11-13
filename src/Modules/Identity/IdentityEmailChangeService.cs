using System.Security.Cryptography;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Identity;

public sealed record EmailChangeRequestInput(string? NewEmail);

public sealed record EmailChangeResult(string Status, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Changing the sign-in email: the request endpoint requires a step-up code (proving the current
/// address), then a one-time link sent to the new address completes the change.
/// </summary>
public sealed class IdentityEmailChangeService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IdentityEmailOutboxWriter outboxWriter,
    ISecurityAuditTransactionWriter auditWriter,
    IdentityLinks links,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    public async Task<EmailChangeResult> RequestAsync(
        Guid accountId,
        EmailChangeRequestInput input,
        CancellationToken cancellationToken = default)
    {
        var newEmail = NormalizeEmail(input.NewEmail);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await db.UserAccounts.Include(item => item.Person)
                .SingleOrDefaultAsync(item => item.Id == accountId && item.IsActive, cancellationToken)
                ?? throw new IdentityException(401, "The account is not active.");
            if (string.Equals(account.NormalizedEmail, newEmail, StringComparison.Ordinal))
                throw new IdentityException(400, "The new email matches the current email.");
            if (await db.UserAccounts.AnyAsync(
                    item => item.NormalizedEmail == newEmail && item.Id != accountId, cancellationToken))
                throw new IdentityException(409, "This email is already used by another account.");

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            await db.EmailChangeRequests
                .Where(request => request.UserAccountId == accountId &&
                                  request.ConsumedAtUtc == null &&
                                  request.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(request => request.RevokedAtUtc, now)
                    .SetProperty(request => request.RowVersion, request => request.RowVersion + 1),
                    cancellationToken);

            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = WebEncoders.Base64UrlEncode(tokenBytes);
            var requestRow = new IdentityEmailChangeRequest
            {
                Id = Guid.CreateVersion7(),
                UserAccountId = accountId,
                NewEmail = input.NewEmail!.Trim(),
                NormalizedNewEmail = newEmail,
                TokenHash = SHA256.HashData(tokenBytes),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(Lifetime)
            };
            db.EmailChangeRequests.Add(requestRow);
            await db.SaveChangesAsync(cancellationToken);

            var url = new UriBuilder(links.EmailChange)
            {
                Fragment = $"token={Uri.EscapeDataString(token)}"
            }.Uri;
            outboxWriter.Enqueue(db, IdentityEmailOutboxWriter.EmailChangeKind, accountId,
                new OutboxEmailChangePayload(requestRow.NewEmail, account.Person.FullName, url.ToString()));
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, accountId, "Identity.EmailChange.Requested",
                new { requestId = requestRow.Id }, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new EmailChangeResult("Pending", requestRow.ExpiresAtUtc);
        }
        catch
        {
            await SafeRollbackAsync(transaction);
            throw;
        }
    }

    public async Task<string> ConfirmAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new IdentityException(400, "A verification token is required.");
        byte[] tokenBytes;
        try
        {
            tokenBytes = WebEncoders.Base64UrlDecode(token);
        }
        catch (FormatException)
        {
            throw new IdentityException(400, "The verification token is invalid.");
        }
        var hash = SHA256.HashData(tokenBytes);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var request = await db.EmailChangeRequests
                .SingleOrDefaultAsync(item => item.TokenHash == hash, cancellationToken)
                ?? throw new IdentityException(400, "The verification token is invalid.");
            if (request.ConsumedAtUtc is not null || request.RevokedAtUtc is not null)
                throw new IdentityException(400, "The verification token was already used.");
            if (request.ExpiresAtUtc <= now)
                throw new IdentityException(400, "The verification token expired.");
            if (request.Attempts >= 5)
                throw new IdentityException(400, "Too many verification attempts.");
            var account = await db.UserAccounts.Include(item => item.Person)
                .SingleOrDefaultAsync(item => item.Id == request.UserAccountId, cancellationToken)
                ?? throw new IdentityException(400, "The account was not found.");
            if (!account.IsActive)
                throw new IdentityException(401, "The account is not active.");
            if (await db.UserAccounts.AnyAsync(
                    item => item.NormalizedEmail == request.NormalizedNewEmail && item.Id != account.Id,
                    cancellationToken))
            {
                request.Attempts++;
                request.RowVersion++;
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new IdentityException(409, "This email is already used by another account.");
            }

            account.Email = request.NewEmail;
            account.NormalizedEmail = request.NormalizedNewEmail;
            account.IsEmailVerified = true;
            account.AuthorizationVersion++;
            request.ConsumedAtUtc = now;
            request.RowVersion++;
            await db.AuthSessions
                .Where(session => session.UserAccountId == account.Id && session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevokeReason, "EmailChanged")
                    .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                    cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, account.Id, "Identity.EmailChange.Completed",
                new { requestId = request.Id }, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return "Completed";
        }
        catch
        {
            await SafeRollbackAsync(transaction);
            throw;
        }
    }

    private async Task WriteAuditAsync(
        IdentityDbContext db,
        IDbContextTransaction transaction,
        Guid actorId,
        string eventType,
        object metadata,
        CancellationToken cancellationToken) =>
        await auditWriter.WriteAsync(
            new SecurityAuditEntry(eventType, "Succeeded", "User",
                ActorUserAccountId: actorId,
                SubjectUserAccountId: actorId,
                SafeMetadata: JsonSerializer.Serialize(metadata)),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new IdentityException(400, "A new email is required.");
        var trimmed = email.Trim();
        if (trimmed.Length > 255 || !trimmed.Contains('@') || trimmed.StartsWith('@') || trimmed.EndsWith('@') ||
            trimmed.Contains(' '))
            throw new IdentityException(400, "The new email is invalid.");
        return trimmed.ToUpperInvariant();
    }

    private static async Task SafeRollbackAsync(IDbContextTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); }
        catch { /* preserve original failure */ }
    }
}


using System.Security.Cryptography;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace HardwareStore.Modules.Identity;

public sealed record EmailChangeRequestInput(string? CurrentPassword, string? NewEmail);

public sealed record EmailChangeResult(string Status, DateTimeOffset ExpiresAtUtc);

public sealed record NotificationView(
    Guid Id,
    string Kind,
    string TitleKey,
    string? BodyKey,
    string? LinkUrl,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);

public sealed record NotificationPage(IReadOnlyList<NotificationView> Items, int UnreadCount);

public sealed record RestrictionView(
    Guid Id,
    string Type,
    string Reason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool IsActive,
    DateTimeOffset? RevokedAtUtc,
    string? ReinstatementReason,
    long RowVersion);

public sealed record RestrictionRequest(string? Type, string? Reason, DateTimeOffset? ExpiresAtUtc);

public sealed record ReinstatementRequest(string? Reason, long RowVersion);

public sealed class IdentityEmailChangeService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IPasswordHashingService passwordHashing,
    UserAccountLoginIdentifierSynchronizer identifierSynchronizer,
    IdentityEmailOutboxWriter outboxWriter,
    ISecurityAuditTransactionWriter auditWriter,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    public async Task<EmailChangeResult> RequestAsync(
        Guid accountId,
        EmailChangeRequestInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.CurrentPassword))
            throw new IdentityException(400, "The current password is required.");
        var newEmail = NormalizeEmail(input.NewEmail);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await db.UserAccounts.Include(item => item.Person)
                .SingleOrDefaultAsync(item => item.Id == accountId && item.IsActive, cancellationToken)
                ?? throw new IdentityException(401, "The account is not active.");
            if (passwordHashing.VerifyPassword(account, account.PasswordHash, input.CurrentPassword) ==
                PasswordHashVerification.Failed)
                throw new IdentityException(400, "The current password is incorrect.");
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

            var baseUrl = configuration["Identity:EmailChangeBaseUrl"]
                ?? "http://localhost:3000/change-email";
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                throw new IdentityException(500, "Email change base URL is invalid.");
            var url = new UriBuilder(baseUri)
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
            await identifierSynchronizer.SetAsync(account, account.Username, account.Email, cancellationToken);
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

public sealed class NotificationService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task PublishAsync(
        Guid recipientAccountId,
        string kind,
        string titleKey,
        string? bodyKey = null,
        string? linkUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (recipientAccountId == Guid.Empty)
            return;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.UserNotifications.Add(new UserNotification
        {
            Id = Guid.CreateVersion7(),
            UserAccountId = recipientAccountId,
            Kind = kind,
            TitleKey = titleKey,
            BodyKey = bodyKey,
            LinkUrl = linkUrl,
            CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationPage> GetAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.UserNotifications.AsNoTracking()
            .Where(item => item.UserAccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
        var unread = await db.UserNotifications.CountAsync(
            item => item.UserAccountId == accountId && !item.IsRead, cancellationToken);
        return new NotificationPage(
            rows.Select(item => new NotificationView(
                item.Id, item.Kind, item.TitleKey, item.BodyKey, item.LinkUrl,
                item.IsRead, item.CreatedAtUtc)).ToList(),
            unread);
    }

    public async Task MarkReadAsync(Guid accountId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.UserNotifications
            .Where(item => item.Id == id && item.UserAccountId == accountId && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAtUtc, timeProvider.GetUtcNow().ToUniversalTime()),
                cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.UserNotifications
            .Where(item => item.UserAccountId == accountId && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAtUtc, timeProvider.GetUtcNow().ToUniversalTime()),
                cancellationToken);
    }
}

public sealed class AccountRestrictionService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<RestrictionView>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.AccountRestrictions.AsNoTracking()
            .Where(item => item.UserAccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return rows.Select(item => Map(item, now)).ToList();
    }

    public async Task<RestrictionView> ApplyAsync(
        Guid targetAccountId,
        Guid actorId,
        RestrictionRequest request,
        CancellationToken cancellationToken = default)
    {
        var type = request.Type?.Trim();
        if (type is not (AccountRestrictionType.Suspension or AccountRestrictionType.Ban))
            throw new IdentityException(400, "A supported restriction type is required.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
            throw new IdentityException(400, "A restriction reason of at least 5 characters is required.");
        if (request.Reason.Trim().Length > 1000)
            throw new IdentityException(400, "The restriction reason is too long.");
        if (targetAccountId == actorId)
            throw new IdentityException(409, "An administrator cannot restrict their own account.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        if (request.ExpiresAtUtc is not null && request.ExpiresAtUtc <= now)
            throw new IdentityException(400, "The restriction expiry must be in the future.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var target = await db.UserAccounts.Include(item => item.UserRoles).ThenInclude(item => item.Role)
                .SingleOrDefaultAsync(item => item.Id == targetAccountId, cancellationToken)
                ?? throw new IdentityException(404, "Account was not found.");
            if (target.UserRoles.Any(role => role.Role.Code == "Owner"))
                throw new IdentityException(409, "Owner accounts cannot be restricted.");
            var restriction = new AccountRestriction
            {
                Id = Guid.CreateVersion7(),
                UserAccountId = targetAccountId,
                Type = type,
                Reason = request.Reason.Trim(),
                CreatedAtUtc = now,
                EffectiveAtUtc = now,
                ExpiresAtUtc = request.ExpiresAtUtc?.ToUniversalTime(),
                CreatedByUserAccountId = actorId
            };
            db.AccountRestrictions.Add(restriction);
            target.AuthorizationVersion++;
            await db.AuthSessions
                .Where(session => session.UserAccountId == targetAccountId && session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevokeReason, "AccountRestricted")
                    .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                    cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, actorId, targetAccountId,
                type == AccountRestrictionType.Ban ? "Identity.AccountBanned" : "Identity.AccountSuspended",
                new { restrictionId = restriction.Id, type }, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(restriction, now);
        }
        catch
        {
            await SafeRollbackAsync(transaction);
            throw;
        }
    }

    public async Task<RestrictionView> ReinstateAsync(
        Guid restrictionId,
        Guid actorId,
        ReinstatementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
            throw new IdentityException(400, "A reinstatement reason of at least 5 characters is required.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var restriction = await db.AccountRestrictions
                .SingleOrDefaultAsync(item => item.Id == restrictionId, cancellationToken)
                ?? throw new IdentityException(404, "Restriction was not found.");
            if (restriction.RevokedAtUtc is not null)
                throw new IdentityException(409, "The restriction is already revoked.");
            if (restriction.RowVersion != request.RowVersion)
                throw new IdentityException(409, "The restriction was changed by another user.");
            restriction.RevokedAtUtc = now;
            restriction.RevokedByUserAccountId = actorId;
            restriction.ReinstatementReason = request.Reason.Trim();
            restriction.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, actorId, restriction.UserAccountId,
                "Identity.AccountReinstated",
                new { restrictionId = restriction.Id }, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(restriction, now);
        }
        catch
        {
            await SafeRollbackAsync(transaction);
            throw;
        }
    }

    public static bool IsActive(AccountRestriction restriction, DateTimeOffset now) =>
        restriction.RevokedAtUtc is null &&
        (restriction.Type == AccountRestrictionType.Ban ||
         (restriction.Type == AccountRestrictionType.Suspension &&
          (restriction.ExpiresAtUtc is null || restriction.ExpiresAtUtc > now)));

    private async Task WriteAuditAsync(
        IdentityDbContext db,
        IDbContextTransaction transaction,
        Guid actorId,
        Guid subjectId,
        string eventType,
        object metadata,
        CancellationToken cancellationToken) =>
        await auditWriter.WriteAsync(
            new SecurityAuditEntry(eventType, "Succeeded", "User",
                ActorUserAccountId: actorId,
                SubjectUserAccountId: subjectId,
                SafeMetadata: JsonSerializer.Serialize(metadata)),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);

    private static RestrictionView Map(AccountRestriction item, DateTimeOffset now) => new(
        item.Id, item.Type, item.Reason, item.CreatedAtUtc, item.EffectiveAtUtc,
        item.ExpiresAtUtc, IsActive(item, now), item.RevokedAtUtc,
        item.ReinstatementReason, item.RowVersion);

    private static async Task SafeRollbackAsync(IDbContextTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); }
        catch { /* preserve original failure */ }
    }
}

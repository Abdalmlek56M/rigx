using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// Versioned policies (e.g. Terms): draft → review → published. A published version is immutable;
/// publishing archives the previous one, and edits require a new version.
/// </summary>
public sealed class ContentPolicyService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PolicyView>> ListAdminAsync(
        string? kind,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.ContentPolicies.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(kind))
        {
            var value = kind.Trim();
            rows = rows.Where(policy => policy.Kind == value);
        }
        return (await rows
                .OrderBy(policy => policy.Kind)
                .ThenByDescending(policy => policy.VersionNumber)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();
    }

    public async Task<PolicyView> GetPublicAsync(
        string kind,
        CancellationToken cancellationToken = default)
    {
        if (!ContentPolicyKind.All.Contains(kind, StringComparer.Ordinal))
            throw new StorefrontException(404, "Policy was not found.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await db.ContentPolicies.AsNoTracking()
            .Where(item => item.Kind == kind && item.Status == ContentStatus.Published)
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new StorefrontException(404, "This policy has not been published yet.");
        return Map(policy);
    }

    public async Task<PolicyView> CreateVersionAsync(
        string kind,
        CancellationToken cancellationToken = default)
    {
        if (!ContentPolicyKind.All.Contains(kind, StringComparer.Ordinal))
            throw new StorefrontException(400, "Unsupported policy kind.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var latest = await db.ContentPolicies
            .Where(policy => policy.Kind == kind)
            .OrderByDescending(policy => policy.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is { Status: ContentStatus.Draft })
            throw new StorefrontException(409, "A draft version already exists for this policy.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var policy = new ContentPolicy
        {
            Id = Guid.CreateVersion7(),
            Kind = kind,
            TitleAr = kind,
            TitleEn = kind,
            BodyAr = string.Empty,
            BodyEn = string.Empty,
            Status = ContentStatus.Draft,
            VersionNumber = (latest?.VersionNumber ?? 0) + 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ContentPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<PolicyView> UpdateAsync(
        Guid id,
        PolicyInput input,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await db.ContentPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Policy was not found.");
        if (policy.Status is ContentStatus.Published or ContentStatus.Archived)
            throw new StorefrontException(409, "Published policy versions are immutable. Create a new version.");
        if (policy.RowVersion != input.RowVersion)
            throw new StorefrontException(409, "The policy was changed by another user.");
        policy.TitleAr = ContentText.Required(input.TitleAr, 200, "Arabic title");
        policy.TitleEn = ContentText.Required(input.TitleEn, 200, "English title");
        policy.BodyAr = ContentText.Optional(input.BodyAr, 40000, "Arabic body") ?? string.Empty;
        policy.BodyEn = ContentText.Optional(input.BodyEn, 40000, "English body") ?? string.Empty;
        policy.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        policy.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<PolicyView> MarkReviewAsync(
        Guid id,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await db.ContentPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Policy was not found.");
        if (policy.Status != ContentStatus.Draft)
            throw new StorefrontException(409, "Only a draft policy can move to review.");
        if (policy.RowVersion != rowVersion)
            throw new StorefrontException(409, "The policy was changed by another user.");
        policy.Status = ContentStatus.Review;
        policy.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        policy.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<PolicyView> PublishAsync(
        Guid id,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await db.ContentPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Policy was not found.");
        if (policy.Status is not (ContentStatus.Draft or ContentStatus.Review))
            throw new StorefrontException(409, "Only a draft or reviewed policy can be published.");
        if (policy.RowVersion != rowVersion)
            throw new StorefrontException(409, "The policy was changed by another user.");
        if (string.IsNullOrWhiteSpace(policy.BodyAr) || string.IsNullOrWhiteSpace(policy.BodyEn))
            throw new StorefrontException(400, "Both policy bodies are required before publishing.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var previousPublished = await db.ContentPolicies
            .Where(item => item.Kind == policy.Kind && item.Status == ContentStatus.Published)
            .ToListAsync(cancellationToken);
        foreach (var previous in previousPublished)
        {
            previous.Status = ContentStatus.Archived;
            previous.UpdatedAtUtc = now;
            previous.RowVersion++;
        }
        policy.Status = ContentStatus.Published;
        policy.PublishedAtUtc = now;
        policy.UpdatedAtUtc = now;
        policy.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    private static PolicyView Map(ContentPolicy policy) => new(
        policy.Id, policy.Kind, policy.TitleAr, policy.TitleEn, policy.BodyAr, policy.BodyEn,
        policy.Status, policy.VersionNumber, policy.PublishedAtUtc,
        policy.CreatedAtUtc, policy.UpdatedAtUtc, policy.RowVersion);
}

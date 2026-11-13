using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Homepage layout: one draft and one published version; publishing validates every rail reference.</summary>
public sealed class HomepageContentService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public const string DefaultLayoutJson =
        "{\"version\":1,\"sections\":[" +
        "{\"id\":\"hero\",\"visible\":true},{\"id\":\"categories\",\"visible\":true}," +
        "{\"id\":\"brands\",\"visible\":true},{\"id\":\"offers\",\"visible\":true}," +
        "{\"id\":\"discovery\",\"visible\":true}," +
        "{\"id\":\"guide\",\"visible\":true},{\"id\":\"latest\",\"visible\":true}," +
        "{\"id\":\"closing\",\"visible\":true}]," +
        "\"rails\":{" +
        "\"brands\":{\"title\":\"\",\"lead\":\"\",\"contentType\":\"brands\",\"limit\":10,\"itemOrder\":[],\"hiddenItemIds\":[]}," +
        "\"categories\":{\"title\":\"\",\"lead\":\"\",\"contentType\":\"categories\",\"limit\":10,\"itemOrder\":[],\"hiddenItemIds\":[]}}}";

    public async Task<HomepageLayoutView> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var published = await db.HomepageLayouts.AsNoTracking()
            .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Published, cancellationToken);
        return published is null
            ? new HomepageLayoutView(DefaultLayoutJson, 0, null)
            : new HomepageLayoutView(published.LayoutJson, published.RowVersion, published.PublishedAtUtc);
    }

    public async Task<HomepageLayoutView> GetDraftAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await db.HomepageLayouts.AsNoTracking()
            .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Draft, cancellationToken);
        return draft is null
            ? new HomepageLayoutView(DefaultLayoutJson, 0, null)
            : new HomepageLayoutView(draft.LayoutJson, draft.RowVersion, null);
    }

    public async Task<HomepageLayoutView> SaveDraftAsync(
        HomepageLayoutUpdateRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var layoutJson = HomepageLayoutRules.Normalize(request.LayoutJson, validateReferences: false);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await db.HomepageLayouts
            .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Draft, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        if (draft is null)
        {
            if (request.RowVersion != 0)
                throw new StorefrontException(409, "The homepage draft was changed by another user.");
            draft = new HomepageLayout
            {
                Id = Guid.CreateVersion7(),
                Status = ContentStatus.Draft,
                LayoutJson = layoutJson,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                UpdatedByUserAccountId = actorId
            };
            db.HomepageLayouts.Add(draft);
        }
        else
        {
            if (draft.RowVersion != request.RowVersion)
                throw new StorefrontException(409, "The homepage draft was changed by another user.");
            draft.LayoutJson = layoutJson;
            draft.UpdatedAtUtc = now;
            draft.UpdatedByUserAccountId = actorId;
            draft.RowVersion++;
        }
        await db.SaveChangesAsync(cancellationToken);
        return new HomepageLayoutView(draft.LayoutJson, draft.RowVersion, null);
    }

    public async Task<HomepageLayoutView> PublishAsync(
        long rowVersion,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var draft = await db.HomepageLayouts
                .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Draft, cancellationToken)
                ?? throw new StorefrontException(400, "There is no homepage draft to publish.");
            if (draft.RowVersion != rowVersion)
                throw new StorefrontException(409, "The homepage draft was changed by another user.");
            var layoutJson = HomepageLayoutRules.Normalize(draft.LayoutJson, validateReferences: true);
            await HomepageLayoutRules.ValidateReferencesAsync(db, layoutJson, cancellationToken);

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            draft.LayoutJson = layoutJson;
            draft.RowVersion++;
            draft.UpdatedAtUtc = now;
            draft.UpdatedByUserAccountId = actorId;

            var published = await db.HomepageLayouts
                .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Published, cancellationToken);
            if (published is null)
            {
                published = new HomepageLayout
                {
                    Id = Guid.CreateVersion7(),
                    Status = ContentStatus.Published,
                    LayoutJson = layoutJson,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    PublishedAtUtc = now,
                    UpdatedByUserAccountId = actorId
                };
                db.HomepageLayouts.Add(published);
            }
            else
            {
                published.LayoutJson = layoutJson;
                published.UpdatedAtUtc = now;
                published.PublishedAtUtc = now;
                published.UpdatedByUserAccountId = actorId;
                published.RowVersion++;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new HomepageLayoutView(published.LayoutJson, draft.RowVersion, published.PublishedAtUtc);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}

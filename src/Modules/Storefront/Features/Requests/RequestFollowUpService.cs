using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Admin follow-up on a request: status, offer, decision, internal note, WhatsApp reply and the hand-off to a purchase.</summary>
public sealed class RequestFollowUpService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    StoreSettingsService settings,
    TimeProvider timeProvider)
{
    public Task<CustomerRequestView> SetStatusAsync(Guid id, string? status, long rowVersion, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, rowVersion, (request, now) =>
        {
            CustomerRequestRules.EnsureCanSetStatus(request, status);
            CustomerRequestRules.SetStatus(request, status!, now);
        }, cancellationToken);

    /// <summary>Records the offer (a revised offer is allowed after a rejection) and moves the request to Offered.</summary>
    public Task<CustomerRequestView> OfferAsync(Guid id, decimal offerAmount, decimal? expectedResale, long rowVersion,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(id, rowVersion, (request, now) =>
        {
            CustomerRequestRules.EnsureCanOffer(request);
            CustomerRequestRules.CheckOfferAmounts(offerAmount, expectedResale);
            request.OfferAmount = offerAmount;
            request.ExpectedResale = expectedResale;
            CustomerRequestRules.SetStatus(request, CustomerRequestStatus.Offered, now);
        }, cancellationToken);

    public Task<CustomerRequestView> DecideAsync(Guid id, string? decision, long rowVersion, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, rowVersion, (request, now) =>
        {
            CustomerRequestRules.EnsureCanDecide(request, decision);
            CustomerRequestRules.SetStatus(request, decision!, now);
        }, cancellationToken);

    /// <summary>Writes the store's answer; the customer reads it in their account (the host adds the bell entry).</summary>
    public Task<CustomerRequestView> ReplyAsync(Guid id, string? reply, long rowVersion, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, rowVersion, (request, now) => CustomerRequestRules.ApplyReply(request, reply, now), cancellationToken);

    public Task<CustomerRequestView> SetNoteAsync(Guid id, string? note, long rowVersion, CancellationToken cancellationToken = default)
    {
        var clean = CustomerRequestRules.CheckNote(note);
        return UpdateAsync(id, rowVersion, (request, _) => request.InternalNote = clean, cancellationToken);
    }

    public async Task<WhatsAppReplyView> WhatsAppReplyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var request = await db.CustomerRequests.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw NotFound();
        var store = await settings.GetAdminAsync(cancellationToken);
        return WhatsAppReply.For(request, store.WhatsAppTemplates.RequestReply);
    }

    /// <summary>Matched notify-me requests the owner has not messaged yet, oldest match first, each with its WhatsApp message (<paramref name="itemUrl"/> = the item's page).</summary>
    public async Task<IReadOnlyList<BackInStockListItem>> BackInStockListAsync(Func<string, string> itemUrl, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var requests = await db.CustomerRequests.AsNoTracking().Where(NotifyMeRules.OpenFilter)
            .Where(item => item.AvailableItemCode != null && item.NotifiedAtUtc == null)
            .OrderBy(item => item.AvailableAtUtc).ThenBy(item => item.Code).ToListAsync(cancellationToken);
        var template = (await settings.GetAdminAsync(cancellationToken)).WhatsAppTemplates.BackInStock;
        return requests.Select(request => new BackInStockListItem(request.Id, request.Code, request.Name, request.Phone, request.Condition, request.AvailableItemCode!,
            request.ModelName ?? request.AvailableItemCode!, request.AvailableAtUtc!.Value,
            WhatsAppReply.For(request, template, itemUrl(request.AvailableItemCode!)))).ToList();
    }

    /// <summary>The owner sent the WhatsApp message: stamps it and closes the request.</summary>
    public Task<CustomerRequestView> MarkBackInStockSentAsync(Guid id, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, null, NotifyMeRules.MarkSent, cancellationToken);

    /// <summary>Checks the request can become a purchase (right type, accepted offer, same version) and returns it.</summary>
    public async Task<CustomerRequestView> EnsureConvertibleAsync(Guid id, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var request = await db.CustomerRequests.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw NotFound();
        EnsureVersion(request, rowVersion);
        CustomerRequestRules.EnsureCanConvert(request);
        return request.ToView();
    }

    /// <summary>One conditional update (still Accepted, same version) so two conversions of one request cannot both win.</summary>
    public async Task<CustomerRequestView> MarkConvertedAsync(Guid id, long rowVersion, Guid purchaseId, string purchaseCode,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var changed = await db.CustomerRequests
            .Where(item => item.Id == id && item.Status == CustomerRequestStatus.Accepted && item.RowVersion == rowVersion)
            .ExecuteUpdateAsync(set => set
                .SetProperty(item => item.Status, CustomerRequestStatus.Converted)
                .SetProperty(item => item.PurchaseId, (Guid?)purchaseId)
                .SetProperty(item => item.PurchaseCode, purchaseCode)
                .SetProperty(item => item.ClosedAtUtc, (DateTimeOffset?)now)
                .SetProperty(item => item.UpdatedAtUtc, now)
                .SetProperty(item => item.RowVersion, item => item.RowVersion + 1), cancellationToken);
        if (changed == 0) throw Changed();
        var request = await db.CustomerRequests.AsNoTracking().SingleAsync(item => item.Id == id, cancellationToken);
        return request.ToView();
    }

    private async Task<CustomerRequestView> UpdateAsync(Guid id, long? rowVersion, Action<CustomerRequest, DateTimeOffset> apply,
        CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var request = await db.CustomerRequests.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw NotFound();
        if (rowVersion is { } version) EnsureVersion(request, version);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        apply(request, now);
        request.UpdatedAtUtc = now;
        request.RowVersion++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw Changed();
        }
        return request.ToView();
    }

    private static void EnsureVersion(CustomerRequest request, long rowVersion)
    {
        if (request.RowVersion != rowVersion) throw Changed();
    }

    private static StorefrontException NotFound() => new(404, "Request was not found.");

    private static StorefrontException Changed() => new(409, "The request was changed by another user.");
}

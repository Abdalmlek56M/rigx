using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Customer requests: the public form submission and the admin inbox list and details.</summary>
public sealed class CustomerRequestService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    IStockModelDirectory stock,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone)
{
    private const int MaxPageSize = 100;
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(10);

    /// <summary>
    /// A filled honeypot stores nothing; the same request sent again within ten minutes returns the first code. A notify-me request
    /// waits for a model: the same phone asking again for a model it already waits for gets the same code, and a model that can be
    /// bought now is refused.
    /// </summary>
    public async Task<CustomerRequestReceipt> SubmitAsync(CustomerRequestSubmission submission, Guid? userAccountId,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(submission.Website)) return new CustomerRequestReceipt(null);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, businessTimeZone).DateTime);
        var draft = CustomerRequestRules.CheckSubmission(submission, today);
        var target = draft.Type == CustomerRequestType.NotifyMe ? await ResolveTargetAsync(draft, cancellationToken) : null;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = target is null
            ? await RecentDuplicateAsync(db, draft, now, cancellationToken)
            : await db.CustomerRequests.AsNoTracking().Where(NotifyMeRules.OpenFilter)
                .Where(item => item.Phone == draft.Phone && item.ModelKey == target.ModelKey)
                .Select(item => item.Code).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return new CustomerRequestReceipt(existing);
        if (target is not null && (await stock.FindAvailableAsync([target.ModelKey], cancellationToken)).ContainsKey(target.ModelKey))
            throw NotifyMeRules.AvailableNow();
        var code = await NextCodeAsync(db, cancellationToken);
        db.CustomerRequests.Add(new CustomerRequest
        {
            Id = Guid.CreateVersion7(), Code = code, Type = draft.Type, Status = CustomerRequestStatus.New, Name = draft.Name,
            Phone = draft.Phone, Email = draft.Email, Governorate = draft.Governorate, Source = draft.Source,
            UserAccountId = userAccountId, Subject = draft.Subject, Message = draft.Message, PartType = draft.PartType,
            ModelName = target?.ModelName ?? draft.ModelName, OwnedFor = draft.OwnedFor, ItemCode = target is null ? draft.ItemCode : target.ItemCode,
            ModelKey = target?.ModelKey, CategoryId = target?.CategoryId, ServiceKind = draft.ServiceKind,
            PreferredDate = draft.PreferredDate, CreatedAtUtc = now, UpdatedAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return new CustomerRequestReceipt(code);
    }

    public async Task<CustomerRequestPage> ListAsync(CustomerRequestQuery query, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.CustomerRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Type)) rows = rows.Where(item => item.Type == query.Type.Trim());
        if (!string.IsNullOrWhiteSpace(query.Status)) rows = rows.Where(item => item.Status == query.Status.Trim());
        if (!string.IsNullOrWhiteSpace(query.Search)) rows = Matching(rows, query.Search);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows.OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new CustomerRequestListItem(item.Id, item.Code, item.Type, item.Status, item.Name, item.Phone,
                item.Governorate, item.ModelName, item.ItemCode, item.ServiceKind, item.OfferAmount, item.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new CustomerRequestPage(items, page, pageSize, total, await NewByTypeAsync(db, cancellationToken));
    }

    public async Task<CustomerRequestView> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var request = await db.CustomerRequests.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Request was not found.");
        return request.ToView();
    }

    private async Task<NotifyMeTarget> ResolveTargetAsync(CustomerRequestDraft draft, CancellationToken cancellationToken)
    {
        var code = NotifyMeRules.ItemCodeOf(draft.ItemCode);
        var model = code is null ? null : await stock.FindByItemCodeAsync(code, cancellationToken);
        return NotifyMeRules.Target(code, draft.ModelName, model);
    }

    private static Task<string?> RecentDuplicateAsync(StorefrontDbContext db, CustomerRequestDraft draft, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var since = now - DuplicateWindow;
        var key = draft.Message ?? draft.ModelName ?? draft.ItemCode;
        return db.CustomerRequests.AsNoTracking()
            .Where(item => item.Phone == draft.Phone && item.Type == draft.Type && item.ServiceKind == draft.ServiceKind
                && item.CreatedAtUtc >= since && (item.Message ?? item.ModelName ?? item.ItemCode) == key)
            .Select(item => item.Code).FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task<string> NextCodeAsync(StorefrontDbContext db, CancellationToken cancellationToken)
    {
        var number = await db.Database
            .SqlQueryRaw<long>("SELECT nextval({0}::regclass) AS \"Value\"", CustomerRequestConfiguration.CodeSequence)
            .SingleAsync(cancellationToken);
        return PublicCode.Format(PublicCode.Request, number);
    }

    /// <summary>A request code (REQ-0012, "req 12"), or a part of the name, or at least three digits of the phone.</summary>
    private static IQueryable<CustomerRequest> Matching(IQueryable<CustomerRequest> rows, string search)
    {
        if (PublicCode.TryNormalize(search, PublicCode.Request, out var code)) return rows.Where(item => item.Code == code);
        var pattern = "%" + search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
        var digits = string.Concat(search.Where(char.IsAsciiDigit));
        return rows.Where(item => EF.Functions.ILike(item.Name, pattern) || (digits.Length >= 3 && item.Phone.Contains(digits)));
    }

    private static async Task<IReadOnlyDictionary<string, int>> NewByTypeAsync(StorefrontDbContext db, CancellationToken cancellationToken)
    {
        var counts = await db.CustomerRequests.AsNoTracking().Where(item => item.Status == CustomerRequestStatus.New)
            .GroupBy(item => item.Type).Select(group => new { Type = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Type, group => group.Count, cancellationToken);
        return CustomerRequestType.All.ToDictionary(type => type, type => counts.GetValueOrDefault(type));
    }
}

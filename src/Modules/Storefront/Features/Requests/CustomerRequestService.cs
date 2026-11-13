using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Customer requests: the public form submission and the admin inbox list and details.</summary>
public sealed class CustomerRequestService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone)
{
    private const int MaxPageSize = 100;
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(10);

    /// <summary>A filled honeypot stores nothing; the same request sent again within ten minutes returns the first code.</summary>
    public async Task<CustomerRequestReceipt> SubmitAsync(CustomerRequestSubmission submission, Guid? userAccountId,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(submission.Website)) return new CustomerRequestReceipt(null);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, businessTimeZone).DateTime);
        var draft = CustomerRequestRules.CheckSubmission(submission, today);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var since = now - DuplicateWindow;
        var key = draft.Message ?? draft.ModelName ?? draft.ItemCode;
        var existing = await db.CustomerRequests.AsNoTracking()
            .Where(item => item.Phone == draft.Phone && item.Type == draft.Type && item.ServiceKind == draft.ServiceKind
                && item.CreatedAtUtc >= since && (item.Message ?? item.ModelName ?? item.ItemCode) == key)
            .Select(item => item.Code).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return new CustomerRequestReceipt(existing);
        var code = await NextCodeAsync(db, cancellationToken);
        db.CustomerRequests.Add(new CustomerRequest
        {
            Id = Guid.CreateVersion7(), Code = code, Type = draft.Type, Status = CustomerRequestStatus.New, Name = draft.Name,
            Phone = draft.Phone, Email = draft.Email, Governorate = draft.Governorate, Source = draft.Source,
            UserAccountId = userAccountId, Subject = draft.Subject, Message = draft.Message, PartType = draft.PartType,
            ModelName = draft.ModelName, OwnedFor = draft.OwnedFor, ItemCode = draft.ItemCode, ServiceKind = draft.ServiceKind,
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

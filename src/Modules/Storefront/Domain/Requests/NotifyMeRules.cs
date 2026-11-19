using System.Linq.Expressions;
using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>Who a notify-me request waits for: a model (matched by <see cref="ModelKey"/>), reached by an RX code or by a typed model name.</summary>
public sealed record NotifyMeTarget(string? ItemCode, string ModelName, string ModelKey, Guid? CategoryId);

/// <summary>A stock item's customer-facing condition, as the Storefront matches it (same values as the Retail stock).</summary>
public static class NotifyMeCondition
{
    public const string New = "New";
    public const string Used = "Used";
}

/// <summary>The few fields of a notify-me request the demand report needs.</summary>
public sealed record NotifyMeEntry(string ModelKey, string ModelName, Guid? CategoryId, string Status, string? AvailableItemCode,
    DateTimeOffset? NotifiedAtUtc, DateTimeOffset CreatedAtUtc);

/// <summary>
/// Notify-me rules. A used item is one piece, so an RX code that sold out never returns: customers wait for the
/// model (<see cref="ModelName.Key"/>), and any published item of that model ends the wait.
/// </summary>
public static class NotifyMeRules
{
    private const string AnyCondition = "Any";

    /// <summary>A notify-me request still being followed (New or Following) that has a model to match.</summary>
    public static readonly Expression<Func<CustomerRequest, bool>> OpenFilter = request =>
        request.Type == CustomerRequestType.NotifyMe && request.ModelKey != null
        && (request.Status == CustomerRequestStatus.New || request.Status == CustomerRequestStatus.Following);

    /// <summary>The RX code the customer typed ("rx 142" becomes RX-0142); null when none. Anything else is not an item.</summary>
    public static string? ItemCodeOf(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null
        : PublicCode.TryNormalize(text, PublicCode.StockItem, out var code) ? code
        : throw new StorefrontException(404, "The item was not found.");

    /// <summary>
    /// With a code the target is that item's model; without one it is the typed model. <paramref name="stockModel"/> is what the
    /// stock says about the code (null = no such item).
    /// </summary>
    public static NotifyMeTarget Target(string? itemCode, string? typedModel, StockModel? stockModel)
    {
        if (itemCode is not null)
        {
            var model = stockModel ?? throw new StorefrontException(404, "The item was not found.");
            return new NotifyMeTarget(itemCode, model.ModelName, model.ModelKey, model.CategoryId);
        }
        var name = ModelName.Clean(typedModel ?? string.Empty);
        var key = ModelName.Key(name);
        return key.Length == 0 ? throw new StorefrontException(400, "Enter the item code or the model.") : new NotifyMeTarget(null, name, key, null);
    }

    /// <summary>The condition the customer waits for: "New" or "Used", or null for any (blank or "Any").</summary>
    public static string? CheckCondition(string? value) => value?.Trim() switch
    {
        null or "" or AnyCondition => null,
        NotifyMeCondition.New => NotifyMeCondition.New,
        NotifyMeCondition.Used => NotifyMeCondition.Used,
        _ => throw new StorefrontException(400, "Condition must be New, Used or Any.")
    };

    /// <summary>
    /// The item that ends the wait: the cheapest of <paramref name="available"/> (the model's sellable items, cheapest first) in the
    /// asked condition, or the cheapest of any condition when none was asked; null when nothing fits.
    /// </summary>
    public static AvailableStockItem? Pick(IReadOnlyList<AvailableStockItem>? available, string? condition) =>
        available?.FirstOrDefault(item => condition is null || item.Condition == condition);

    public static StorefrontException AvailableNow() => new(409, "This item is available now.");

    /// <summary>Only a matched request that is still open can be marked as messaged (which closes it).</summary>
    public static void EnsureCanMarkSent(CustomerRequest request)
    {
        if (request.Type != CustomerRequestType.NotifyMe) throw new StorefrontException(400, "Only a notify-me request can be marked as sent.");
        if (!IsOpen(request) || request.AvailableItemCode is null || request.NotifiedAtUtc is not null)
            throw new StorefrontException(409, "This request has no waiting back-in-stock message.");
    }

    public static void MarkSent(CustomerRequest request, DateTimeOffset now)
    {
        EnsureCanMarkSent(request);
        request.NotifiedAtUtc = now;
        CustomerRequestRules.SetStatus(request, CustomerRequestStatus.Closed, now);
    }

    /// <summary>Requests grouped by model, most asked first. Rows without a model key (older requests) cannot be grouped and are skipped.</summary>
    public static IReadOnlyList<NotifyMeDemandRow> Demand(IEnumerable<NotifyMeEntry> entries, IReadOnlySet<string> availableKeys) =>
        entries.GroupBy(entry => entry.ModelKey).Select(group =>
            {
                var latest = group.OrderByDescending(entry => entry.CreatedAtUtc).ToList();
                return new NotifyMeDemandRow(group.Key, latest[0].ModelName, latest.Select(entry => entry.CategoryId).FirstOrDefault(id => id is not null),
                    latest.Count, latest.Count(entry => IsWaiting(entry)), latest.Count(entry => entry.NotifiedAtUtc is not null),
                    latest[0].CreatedAtUtc, availableKeys.Contains(group.Key));
            })
            .OrderByDescending(row => row.Total).ThenByDescending(row => row.LastRequestedAtUtc).ThenBy(row => row.ModelKey, StringComparer.Ordinal).ToList();

    private static readonly Func<CustomerRequest, bool> IsOpen = OpenFilter.Compile();

    private static bool IsWaiting(NotifyMeEntry entry) =>
        entry.AvailableItemCode is null && entry.Status is CustomerRequestStatus.New or CustomerRequestStatus.Following;
}

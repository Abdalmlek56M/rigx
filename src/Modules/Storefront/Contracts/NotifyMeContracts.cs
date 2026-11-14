namespace HardwareStore.Modules.Storefront;

/// <summary>What the stock tells the Storefront about models. Implemented by the Host over Retail, so Storefront never references it.</summary>
public interface IStockModelDirectory
{
    /// <summary>The model of the stock item with this RX code (any item, published or not); null when no item has the code.</summary>
    Task<StockModel?> FindByItemCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>For each model key that has a published item ready to sell: its cheapest such item. Models with none are missing.</summary>
    Task<IReadOnlyDictionary<string, AvailableStockItem>> FindAvailableAsync(IReadOnlyCollection<string> modelKeys,
        CancellationToken cancellationToken = default);
}

public sealed record StockModel(string ModelName, string ModelKey, Guid? CategoryId);

public sealed record AvailableStockItem(string Code, string ModelName);

/// <summary>A waiting notify-me request that just found its item; the email is null when the customer gave none.</summary>
public sealed record BackInStockMatch(string Name, string? Email, Guid? UserAccountId, string ItemCode, string ItemName);

/// <summary>A matched notify-me request the owner has not yet messaged, with the prepared WhatsApp message.</summary>
public sealed record BackInStockListItem(Guid Id, string Code, string Name, string ItemCode, string ItemName,
    DateTimeOffset AvailableAtUtc, WhatsAppReplyView WhatsApp);

/// <summary>One model people asked to be told about. <paramref name="Open"/> = still waiting for stock; <paramref name="Notified"/> = the owner messaged them.</summary>
public sealed record NotifyMeDemandRow(string ModelKey, string ModelName, Guid? CategoryId, int Total, int Open, int Notified,
    DateTimeOffset LastRequestedAtUtc, bool AvailableNow);

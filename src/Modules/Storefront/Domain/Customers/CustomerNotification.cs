namespace HardwareStore.Modules.Storefront;

/// <summary>
/// One entry in a signed-in customer's notifications bell. The web app writes the text from
/// <see cref="Kind"/>, <see cref="Subject"/> and <see cref="Status"/>; <see cref="Target"/> is what the entry opens.
/// </summary>
public sealed class CustomerNotification
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public required string Kind { get; set; }
    /// <summary>Order reference (OrderStatus), item name (BackInStock) or request code (RequestReply).</summary>
    public required string Subject { get; set; }
    /// <summary>The order status the customer sees (OrderStatus only).</summary>
    public string? Status { get; set; }
    /// <summary>Order id (OrderStatus), RX code (BackInStock) or request code (RequestReply).</summary>
    public required string Target { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
}

public static class CustomerNotificationKind
{
    public const string OrderStatus = "OrderStatus";
    public const string BackInStock = "BackInStock";
    public const string RequestReply = "RequestReply";
}

public static class CustomerNotificationRules
{
    /// <summary>The bell lists this many of the newest entries.</summary>
    public const int ListSize = 30;

    public const int SubjectMaxLength = 200;

    public static string Subject(string text) => text.Length > SubjectMaxLength ? text[..SubjectMaxLength] : text;
}

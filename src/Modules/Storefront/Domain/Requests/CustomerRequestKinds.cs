namespace HardwareStore.Modules.Storefront;

/// <summary>What the customer is asking for. Each type has its own required fields (see <see cref="CustomerRequestRules"/>).</summary>
public static class CustomerRequestType
{
    public const string Sell = "Sell";
    public const string TradeIn = "TradeIn";
    public const string Upgrade = "Upgrade";
    public const string NotifyMe = "NotifyMe";
    public const string Support = "Support";
    public const string Contact = "Contact";
    public const string Service = "Service";
    public const string Complaint = "Complaint";
    public const string Suggestion = "Suggestion";
    public static readonly IReadOnlyList<string> All = [Sell, TradeIn, Upgrade, NotifyMe, Support, Contact, Service, Complaint, Suggestion];
}

public static class CustomerRequestStatus
{
    public const string New = "New";
    public const string Following = "Following";
    public const string Offered = "Offered";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Converted = "Converted";
    public const string Closed = "Closed";
    public static readonly IReadOnlyList<string> All = [New, Following, Offered, Accepted, Rejected, Converted, Closed];
}

/// <summary>The kind of workshop service a Service request asks for.</summary>
public static class ServiceKind
{
    public const string Consult = "Consult";
    public const string Build = "Build";
    public const string Test = "Test";
    public const string Maintenance = "Maintenance";
    public const string Sourcing = "Sourcing";
    public const string Upgrade = "Upgrade";
    public static readonly IReadOnlyList<string> All = [Consult, Build, Test, Maintenance, Sourcing, Upgrade];
}

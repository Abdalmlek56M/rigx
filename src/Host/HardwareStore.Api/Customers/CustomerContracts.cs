using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>One row of the admin customer list: the account plus the sales numbers Retail knows about it.</summary>
public sealed record CustomerListItem(CustomerListRow Customer, int OrderCount, decimal TotalSpent, DateTimeOffset? LastOrderAtUtc);

/// <summary>The customer 360 view, composed from Identity, Retail and Storefront.</summary>
public sealed record CustomerProfile(CustomerAccountView Account, LoginMethodsView LoginMethods, IReadOnlyList<RestrictionView> Restrictions,
    CustomerSalesView Sales, IReadOnlyList<CustomerRequestSummary> Requests, string? Governorate);

/// <summary>Exactly what the customer's own account pages show (read only).</summary>
public sealed record CustomerViewAs(CurrentIdentityUser Profile, IReadOnlyList<CustomerAddressView> Addresses, IReadOnlyList<OrderView> Orders,
    IReadOnlyList<WarrantyAccountView> Warranties, IReadOnlyList<InvoiceSummaryView> Invoices, IReadOnlyList<MyRequestView> Requests,
    IReadOnlyList<SavedBuildSummary> Builds);

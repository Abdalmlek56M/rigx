using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>
/// Composes the customer pages across modules (decision 11): the list, the 360 view and "view as customer". Each module
/// only supplies its own data through its services.
/// </summary>
public sealed class CustomerProfileComposer(
    CustomerAdminService customers,
    GoogleSignInService google,
    AccountRestrictionService restrictions,
    CustomerSalesService sales,
    AccountRequestService requests,
    CustomerAddressService addresses,
    IdentityCurrentUserService currentUsers,
    OrderService orders,
    WarrantyService warranties,
    InvoiceService invoices,
    SavedBuildService builds,
    StaffCustomerLinkService links,
    TimeZoneInfo businessZone)
{
    public async Task<PagedResult<CustomerListItem>> ListAsync(string? search, int page, CancellationToken cancellationToken)
    {
        var accounts = await customers.ListAsync(search, page, cancellationToken);
        var keys = accounts.Items.Select(row => new CustomerSalesKey(row.Id, row.Phone)).ToList();
        var stats = await sales.StatsAsync(keys, cancellationToken);
        var items = accounts.Items.Select(row =>
        {
            var summary = stats[row.Id];
            return new CustomerListItem(row, summary.OrderCount, summary.TotalSpent, summary.LastOrderAtUtc);
        }).ToList();
        return new PagedResult<CustomerListItem>(items, accounts.Page, accounts.PageSize, accounts.Total);
    }

    /// <summary>Customers registered in the range with their order numbers, for the customer export.</summary>
    public async Task<ExportTable> ExportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var accounts = await customers.ListForExportAsync(from, to, cancellationToken);
        var stats = new Dictionary<Guid, CustomerOrderStats>();
        foreach (var batch in accounts.Chunk(500))
            foreach (var (id, summary) in await sales.StatsAsync([.. batch.Select(row => new CustomerSalesKey(row.Id, row.Phone))], cancellationToken))
                stats[id] = summary;
        return new ExportTable(["code", "name", "email", "phone", "active", "registered", "orders", "total spent", "last order"],
            [.. accounts.Select(row => (IReadOnlyList<ExportCell>)
            [
                ExportCell.Text(row.PersonCode), ExportCell.Text(row.Name), ExportCell.Text(row.Email), ExportCell.Text(row.Phone),
                ExportCell.Flag(row.IsActive), ExportCell.Instant(row.CreatedAtUtc, businessZone), ExportCell.Number(stats[row.Id].OrderCount),
                ExportCell.Number(stats[row.Id].TotalSpent), ExportCell.Instant(stats[row.Id].LastOrderAtUtc, businessZone)
            ])]);
    }

    public async Task<CustomerProfile> GetAsync(Guid id, bool includeSerial, CancellationToken cancellationToken)
    {
        var account = await customers.GetAsync(id, cancellationToken);
        var salesView = await sales.GetAsync(id, account.Phone, account.Email, includeSerial, cancellationToken);
        var active = (await restrictions.ListAsync(id, cancellationToken)).Where(restriction => restriction.IsActive).ToList();
        return new CustomerProfile(account, await google.GetLoginMethodsAsync(id, cancellationToken), active, salesView,
            await requests.ListForCustomerAsync(id, account.Phone, cancellationToken), account.DefaultGovernorate ?? salesView.Stats.Governorate,
            await links.StaffOfCustomerAsync(id, cancellationToken));
    }

    public async Task<CustomerViewAs> ViewAsAsync(Guid id, CancellationToken cancellationToken)
    {
        await customers.GetAsync(id, cancellationToken);
        var profile = await currentUsers.GetAsync(id, cancellationToken, activeOnly: false);
        return new CustomerViewAs(profile!, await addresses.ListAsync(id, cancellationToken),
            await orders.ListMyOrdersAsync(id, cancellationToken), await warranties.ListMineAsync(id, cancellationToken),
            await invoices.ListMineAsync(id, cancellationToken), await requests.ListMineAsync(id, cancellationToken),
            await builds.ListMineAsync(id, cancellationToken));
    }
}

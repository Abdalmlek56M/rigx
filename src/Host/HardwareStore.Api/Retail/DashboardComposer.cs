using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;

/// <summary>The admin dashboard: the Retail money dashboard plus the category names (Catalog) and the new customer requests (Storefront).</summary>
public sealed record AdminDashboardView(DashboardView Dashboard, int NewRequests);

public sealed class DashboardComposer(DashboardService dashboard, TaxonomyService taxonomy, IDbContextFactory<StorefrontDbContext> storefrontFactory)
{
    public async Task<AdminDashboardView> BuildAsync(DateOnly from, DateOnly to, TimeZoneInfo zone, CancellationToken cancellationToken)
    {
        var view = await dashboard.BuildAsync(from, to, zone, cancellationToken);
        var names = await InvoiceDocuments.CategoryNamesAsync(taxonomy, cancellationToken);
        var named = view with
        {
            TopCategories = view.TopCategories
                .Select(row => row with { CategoryName = row.CategoryId is Guid id && names.TryGetValue(id, out var name) ? name : null }).ToList()
        };
        await using var storefront = await storefrontFactory.CreateDbContextAsync(cancellationToken);
        var newRequests = await storefront.CustomerRequests.AsNoTracking().CountAsync(item => item.Status == CustomerRequestStatus.New, cancellationToken);
        return new AdminDashboardView(named, newRequests);
    }
}

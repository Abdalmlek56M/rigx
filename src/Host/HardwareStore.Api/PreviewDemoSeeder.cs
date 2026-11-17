using HardwareStore.Common.Domain;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;

/// <summary>Counts of what one demo seed call created (0 for a part that was already there).</summary>
public sealed record PreviewDemoCounts(int StockItems, int Builds, int Customers, int OnlineOrders, int PosSales, int Expenses);

/// <summary>
/// Development-only demo store (roadmap 4.4g): stock items, builds, customers, orders, POS sales, expenses, policies and FAQs.
/// Every write goes through the module services with an actor, so stock, invoices, warranties, money and audit behave like real use.
/// Safe to call twice: each part is skipped when its demo data already exists.
/// </summary>
internal sealed partial class PreviewDemoSeeder
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ICatalogMediaStorage mediaStorage;
    private readonly TimeZoneInfo businessZone;
    private readonly TimeProvider baseTime;
    private readonly ILogger logger;
    private readonly DemoClock clock = new();
    private readonly Dictionary<string, Guid> itemIdsByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Guid>> unitsByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> customerIdsByEmail = new(StringComparer.OrdinalIgnoreCase);

    private IDbContextFactory<RetailDbContext> retailFactory = null!;
    private IDbContextFactory<StorefrontDbContext> storefrontFactory = null!;
    private IDbContextFactory<IdentityDbContext> identityFactory = null!;
    private IDbContextFactory<CatalogDbContext> catalogFactory = null!;
    private ISecurityAuditTransactionWriter auditWriter = null!;
    private Guid ownerId;
    private string ownerName = string.Empty;
    private DateTimeOffset seedNow;
    private DateOnly today;

    public PreviewDemoSeeder(
        IServiceScopeFactory scopeFactory,
        ICatalogMediaStorage mediaStorage,
        TimeZoneInfo businessZone,
        TimeProvider baseTime,
        ILogger logger)
    {
        this.scopeFactory = scopeFactory;
        this.mediaStorage = mediaStorage;
        this.businessZone = businessZone;
        this.baseTime = baseTime;
        this.logger = logger;
    }

    public async Task<PreviewDemoCounts> SeedAsync(Guid ownerUserId, string ownerFullName, CancellationToken cancellationToken)
    {
        ownerId = ownerUserId;
        ownerName = ownerFullName;
        seedNow = baseTime.GetUtcNow();
        clock.UtcNow = seedNow;
        today = BusinessDay.Today(clock, businessZone);

        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        retailFactory = services.GetRequiredService<IDbContextFactory<RetailDbContext>>();
        storefrontFactory = services.GetRequiredService<IDbContextFactory<StorefrontDbContext>>();
        identityFactory = services.GetRequiredService<IDbContextFactory<IdentityDbContext>>();
        catalogFactory = services.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
        auditWriter = services.GetRequiredService<ISecurityAuditTransactionWriter>();

        await SeedSettingsAsync(cancellationToken);
        await SeedContentAsync(cancellationToken);
        var customers = await SeedCustomersAsync(cancellationToken);
        var stockItems = await SeedStockAsync(cancellationToken);
        var builds = await SeedBuildsAsync(cancellationToken);
        var orders = await SeedOrdersAsync(cancellationToken);
        var posSales = await SeedPosAsync(cancellationToken);
        var expenses = await SeedExpensesAsync(cancellationToken);
        logger.LogInformation(
            "Development demo data: {StockItems} stock items, {Builds} builds, {Customers} customers, {Orders} online orders, " +
            "{PosSales} POS sales, {Expenses} expenses.",
            stockItems, builds, customers, orders, posSales, expenses);
        return new PreviewDemoCounts(stockItems, builds, customers, orders, posSales, expenses);
    }

    private StoreSettingsService Settings => new(storefrontFactory, auditWriter, clock);
    private ContentPolicyService Policies => new(storefrontFactory, clock);
    private ContentFaqService Faqs => new(storefrontFactory, new TaxonomyService(catalogFactory, clock), clock);
    private IdentityProfileService Profiles => new(identityFactory, auditWriter);
    private CustomerAddressService Addresses => new(identityFactory, auditWriter, clock);
    private InventoryUnitService Units => new(retailFactory, clock);
    private StockItemAdminService StockItems => new(retailFactory, clock);
    private AssemblyService Assemblies => new(retailFactory, clock);
    private OrderService Orders => new(retailFactory, clock);
    private OrderStatusService Statuses => new(retailFactory, clock);
    private OrderPaymentService Payments => new(retailFactory, clock, businessZone);
    private PosService Pos => new(retailFactory, clock, businessZone);
    private MoneyAccountService Money => new(retailFactory, clock, businessZone);
    private ExpenseService Expenses => new(retailFactory, new BusinessReportService(retailFactory, businessZone), clock, businessZone);

    /// <summary>Shipping groups, pickup address and the demo payment methods; each section only when it is still empty.</summary>
    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = Settings;
        var admin = await settings.GetAdminAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(admin.StoreInfo.PickupAddress))
        {
            var storeInfo = admin.StoreInfo with
            {
                PickupAddress = "شارع البيطاش الرئيسي، العجمي، الإسكندرية",
                PickupHours = "كل يوم من 12 الظهر لـ 10 بالليل",
                PickupInstructions = "كلّمنا قبل ما تيجي عشان نجهّز الطلب."
            };
            admin = await settings.UpdateSectionAsync(storeInfo, admin.RowVersion, ownerId, cancellationToken);
        }
        if (admin.Shipping.Groups.Count == 0 && admin.Shipping.DefaultFee == 0m)
        {
            var shipping = admin.Shipping with
            {
                PickupEnabled = true,
                DefaultFee = 90m,
                DefaultDeliveryTime = "خلال 4–6 أيام",
                Groups =
                [
                    Group("القاهرة والجيزة", 40m, "خلال 1–2 يوم", "C", "GZ"),
                    Group("الإسكندرية", 50m, "خلال 1–2 يوم", "ALX"),
                    Group("الدلتا", 60m, "خلال 2–3 أيام", "KB", "GH", "MNF", "DK", "BH", "KFS", "SHR", "DT"),
                    Group("القناة", 70m, "خلال 2–4 أيام", "PTS", "IS", "SUZ"),
                    Group("الصعيد", 80m, "خلال 3–5 أيام", "BNS", "MN", "AST", "SHG", "KN", "LX", "ASN", "FYM")
                ]
            };
            admin = await settings.UpdateSectionAsync(shipping, admin.RowVersion, ownerId, cancellationToken);
        }
        if (!admin.Payment.InstaPayEnabled && !admin.Payment.WalletEnabled)
        {
            var payment = admin.Payment with
            {
                InstaPayEnabled = true,
                InstaPayAddress = "rigx-demo@example.com",
                WalletEnabled = true,
                WalletNumber = "01000000001"
            };
            await settings.UpdateSectionAsync(payment, admin.RowVersion, ownerId, cancellationToken);
        }
    }

    private static ShippingGroup Group(string name, decimal fee, string deliveryTime, params string[] governorates) =>
        new() { Name = name, Fee = fee, DeliveryTime = deliveryTime, Governorates = governorates };

    /// <summary>Publishes the five missing policies and the six general FAQs (English is required by the domain but never shown as Arabic).</summary>
    private async Task SeedContentAsync(CancellationToken cancellationToken)
    {
        clock.UtcNow = seedNow.AddDays(-28);
        foreach (var policy in PreviewDemoContent.Policies())
        {
            var versions = await Policies.ListAdminAsync(policy.Kind, cancellationToken);
            if (versions.Any(version => version.Status == ContentStatus.Published)) continue;
            var draft = versions.FirstOrDefault(version => version.Status is ContentStatus.Draft or ContentStatus.Review)
                ?? await Policies.CreateVersionAsync(policy.Kind, cancellationToken);
            var updated = await Policies.UpdateAsync(draft.Id,
                new PolicyInput(policy.TitleAr, policy.TitleEn, policy.BodyAr, policy.BodyEn, draft.RowVersion), cancellationToken);
            await Policies.PublishAsync(updated.Id, updated.RowVersion, cancellationToken);
        }

        clock.UtcNow = seedNow.AddDays(-27);
        if ((await Faqs.ListAdminAsync(cancellationToken: cancellationToken)).Count > 0) return;
        foreach (var faq in PreviewDemoContent.Faqs())
        {
            var created = await Faqs.CreateAsync(new FaqInput(faq.QuestionAr, faq.QuestionEn, faq.AnswerAr, faq.AnswerEn,
                faq.Topic, FaqScope.General, null, null, false, null, null, null, 0), cancellationToken);
            await Faqs.SetPublishedAsync(created.Id, true, created.RowVersion, cancellationToken);
        }
    }

    private DateTimeOffset At(int daysAgo, int hour) =>
        BusinessDay.Range(today.AddDays(-daysAgo), today.AddDays(-daysAgo), businessZone).FromUtc.AddHours(hour);

    private ShippingRate Rate(ShippingSettings shipping, string governorateCode)
    {
        var quote = shipping.QuoteFor(governorateCode);
        return new ShippingRate(quote.Fee, quote.DeliveryTime);
    }

    private sealed class DemoClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}

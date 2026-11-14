using HardwareStore.Common.Infrastructure.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Storefront;

/// <summary>What the audit log can put back in Storefront: settings sections (not the logo files), articles and FAQ entries.</summary>
internal static class StorefrontAuditRestore
{
    public static IServiceCollection AddStorefrontAuditRestore(this IServiceCollection services) => services
        .AddAuditRestoreTarget<StorefrontDbContext, StoreSettings>(
            nameof(StoreSettings.StoreInfo), nameof(StoreSettings.ContactLinks), nameof(StoreSettings.Shipping), nameof(StoreSettings.Payment),
            nameof(StoreSettings.Watermark), nameof(StoreSettings.Controls), nameof(StoreSettings.WhatsAppTemplates), nameof(StoreSettings.Reviews),
            nameof(StoreSettings.Language), nameof(StoreSettings.Invoice), nameof(StoreSettings.LeadSources), nameof(StoreSettings.Sales),
            nameof(StoreSettings.WarrantyOptions), nameof(StoreSettings.Backup))
        .AddAuditRestoreTarget<StorefrontDbContext, ContentArticle>(
            nameof(ContentArticle.Slug), nameof(ContentArticle.TitleAr), nameof(ContentArticle.TitleEn), nameof(ContentArticle.SummaryAr),
            nameof(ContentArticle.SummaryEn), nameof(ContentArticle.BlocksArJson), nameof(ContentArticle.BlocksEnJson), nameof(ContentArticle.Status),
            nameof(ContentArticle.PublishedAtUtc))
        .AddAuditRestoreTarget<StorefrontDbContext, ContentFaq>(
            nameof(ContentFaq.QuestionAr), nameof(ContentFaq.QuestionEn), nameof(ContentFaq.AnswerAr), nameof(ContentFaq.AnswerEn),
            nameof(ContentFaq.Topic), nameof(ContentFaq.Scope), nameof(ContentFaq.CategoryId), nameof(ContentFaq.ItemCode), nameof(ContentFaq.HelpPageOnly),
            nameof(ContentFaq.ExcludedCategoryIds), nameof(ContentFaq.ExcludedItemCodes), nameof(ContentFaq.YouTubeId), nameof(ContentFaq.SortOrder),
            nameof(ContentFaq.Status), nameof(ContentFaq.PublishedAtUtc));
}

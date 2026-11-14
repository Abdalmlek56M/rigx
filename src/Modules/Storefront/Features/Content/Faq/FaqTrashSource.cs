using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Trash;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Trashed FAQ entries.</summary>
public sealed class FaqTrashSource(IDbContextFactory<StorefrontDbContext> contextFactory)
    : SoftDeleteTrashSource<StorefrontDbContext, ContentFaq>(contextFactory)
{
    public const string TrashKind = "faq";

    public override string Kind => TrashKind;

    protected override DbSet<ContentFaq> Set(StorefrontDbContext db) => db.ContentFaqs;

    protected override string Label(ContentFaq item) => item.QuestionAr;

    protected override AppException NotFound() => new StorefrontException(404, "FAQ was not found in the trash.");
}

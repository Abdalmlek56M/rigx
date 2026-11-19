using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string StockImportTemplateFileName = "rigx-stock-import-template.xlsx";
    private const string FallbackExampleCategory = "GPU";

    private static async Task<FileContentHttpResult> GetStockImportTemplateAsync(HttpContext context, TaxonomyService taxonomy, CancellationToken ct)
    {
        var first = (await LoadImportCategoriesAsync(taxonomy, ct)).FirstOrDefault();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.File(StockImportService.BuildTemplate(first?.Code ?? first?.Name ?? FallbackExampleCategory), XlsxContentType, StockImportTemplateFileName);
    }

    /// <summary>
    /// With <c>dryRun</c> (the default) the file is only checked and nothing is written; without it every row is imported in one
    /// transaction. A file with row errors is a 400 whose validation errors name each bad row; the same file twice is a 409.
    /// </summary>
    private static async Task<Ok<StockImportResult>> ImportStockAsync(HttpContext context, StockImportService imports, TaxonomyService taxonomy,
        StoreSettingsService settings, CancellationToken ct, bool dryRun = true)
    {
        var file = await ReadImportFileAsync(context, ct);
        var lookups = new StockImportLookups(await LoadImportCategoriesAsync(taxonomy, ct), await LoadWarrantyLabelsAsync(settings, ct));
        return TypedResults.Ok(dryRun
            ? StockImportService.DryRun(file, lookups)
            : await imports.ImportAsync(file, lookups, context.RequireUserId(), ct));
    }

    private static async Task<StockImportFile> ReadImportFileAsync(HttpContext context, CancellationToken ct)
    {
        if (!context.Request.HasFormContentType) throw ImportFileProblem("لازم ترفع الملف في فورم multipart");
        if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
            limit.MaxRequestBodySize = StockImportRules.MaxFileBytes + 64 * 1024;
        var form = await context.Request.ReadFormAsync(ct);
        var upload = form.Files.GetFile("file") ?? throw ImportFileProblem("ارفع ملف Excel");
        if (upload.Length > StockImportRules.MaxFileBytes) throw ImportFileProblem("حجم الملف لازم يكون أقل من 2 ميجا");
        using var content = new MemoryStream();
        await upload.CopyToAsync(content, ct);
        return new StockImportFile(Path.GetFileName(upload.FileName), content.ToArray());
    }

    private static ApiProblemException ImportFileProblem(string message) => new(StatusCodes.Status400BadRequest, message);

    private static async Task<IReadOnlyList<StockImportCategory>> LoadImportCategoriesAsync(TaxonomyService taxonomy, CancellationToken ct) =>
        [.. FlattenCategories(await taxonomy.GetCategoryTreeAsync(ct))];

    private static IEnumerable<StockImportCategory> FlattenCategories(IEnumerable<CategoryView> categories) =>
        categories.SelectMany(category => FlattenCategories(category.Children).Prepend(new StockImportCategory(category.Id, category.Code, category.Name)));

    /// <summary>The warranty list lives in the store settings, so a sheet's months are matched to a label from it (the first label for a month count).</summary>
    private static async Task<IReadOnlyDictionary<int, string>> LoadWarrantyLabelsAsync(StoreSettingsService settings, CancellationToken ct)
    {
        var labels = new Dictionary<int, string>();
        foreach (var option in await WarrantyChoice.LoadOptionsAsync(settings, ct))
            if (option.Months is { } months) labels.TryAdd(months, option.Label);
        return labels;
    }
}

using System.Security.Cryptography;
using HardwareStore.Common.Infrastructure.Export;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Opening-stock import: the owner fills the Excel template and imports it in one go. A dry run only reports each row;
/// the real import is all-or-nothing in one transaction and refuses a file that was already imported (same SHA-256).
/// </summary>
public sealed class StockImportService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    private static readonly ReceiptSource ImportSource = new(SourceName: null, SourceType: "Import", PurchaseKind: null, PurchaseDate: null);

    public static byte[] BuildTemplate(string exampleCategory) => XlsxExportWriter.Write(StockImportRules.Template(exampleCategory));

    public static StockImportResult DryRun(StockImportFile file, StockImportLookups lookups) => Summarize(Parse(file, lookups), imported: false, items: null);

    public async Task<StockImportResult> ImportAsync(StockImportFile file, StockImportLookups lookups, Guid actorId, CancellationToken cancellationToken = default)
    {
        var rows = Parse(file, lookups);
        if (rows.Any(row => row.Errors.Count > 0))
            throw new RetailException(400, "في صفوف فيها أخطاء، ومفيش حاجة اتستوردت. صلّح الملف وجرّب تاني.", RowErrors(rows));
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(file.Content));
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.StockImports.AnyAsync(import => import.Sha256 == sha256, cancellationToken))
            throw new RetailException(409, "الملف ده اتستورد قبل كده");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var items = new List<StockItem>();
            foreach (var row in rows)
                items.AddRange(await StockItemFactory.CreateAsync(db, row.Line!, ImportSource, receiptId: null, now, cancellationToken));
            db.StockImports.Add(new StockImport
            {
                Id = Guid.CreateVersion7(),
                Sha256 = sha256,
                FileName = file.FileName.Length > 200 ? file.FileName[..200] : file.FileName,
                RowCount = rows.Count,
                ItemCount = items.Count,
                ImportedByUserAccountId = actorId,
                ImportedAtUtc = now
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Summarize(rows, imported: true, [.. items.Select(item => new StockImportedItem(item.Code, item.Slug, item.ModelName))]);
        }
        catch
        {
            await transaction.RollbackQuietlyAsync();
            throw;
        }
    }

    /// <summary>Checks the file's size, type, header and row count, then parses every non-blank data row.</summary>
    private static List<StockImportRow> Parse(StockImportFile file, StockImportLookups lookups)
    {
        if (!file.FileName.EndsWith(StockImportRules.FileExtension, StringComparison.OrdinalIgnoreCase))
            throw new RetailException(400, "الملف لازم يكون Excel بصيغة xlsx");
        if (file.Content.Length is 0 or > StockImportRules.MaxFileBytes)
            throw new RetailException(400, "حجم الملف لازم يكون أقل من 2 ميجا");
        IReadOnlyList<IReadOnlyList<string>> sheet;
        try
        {
            sheet = XlsxImportReader.ReadFirstSheet(file.Content);
        }
        catch (XlsxReadException)
        {
            throw new RetailException(400, "الملف مش ملف Excel (xlsx) سليم");
        }
        var (indexes, missing) = StockImportRules.MapHeader(sheet.Count > 0 ? sheet[0] : []);
        if (missing.Count > 0)
            throw new RetailException(400, $"الأعمدة دي ناقصة في الصف الأول: {string.Join("، ", missing)}. استخدم القالب.");
        var rows = new List<StockImportRow>();
        for (var index = 1; index < sheet.Count; index++)
            if (sheet[index].Any(cell => cell.Length > 0)) rows.Add(StockImportRules.ParseRow(index + 1, sheet[index], indexes, lookups));
        if (rows.Count == 0) throw new RetailException(400, "مفيش صفوف بيانات في الملف");
        if (rows.Count > StockImportRules.MaxDataRows)
            throw new RetailException(400, $"الملف فيه صفوف أكتر من {StockImportRules.MaxDataRows}. قسّمه على أكتر من ملف.");
        var itemsToCreate = rows.Sum(row => row.ItemsToCreate);
        if (itemsToCreate > StockImportRules.MaxItems)
            throw new RetailException(400, $"الملف هيعمل {itemsToCreate} قطعة، والحد {StockImportRules.MaxItems} في المرة. قسّمه على أكتر من ملف.");
        return rows;
    }

    private static Dictionary<string, string[]> RowErrors(IEnumerable<StockImportRow> rows) =>
        rows.Where(row => row.Errors.Count > 0).ToDictionary(row => $"الصف {row.Row}", row => row.Errors.ToArray());

    private static StockImportResult Summarize(List<StockImportRow> rows, bool imported, IReadOnlyList<StockImportedItem>? items) => new(
        [.. rows.Select(row => new StockImportRowView(row.Row, row.ModelName, row.Condition, row.Quantity, row.ItemsToCreate, row.Errors))],
        rows.Count(row => row.Errors.Count == 0), rows.Count(row => row.Errors.Count > 0),
        rows.Sum(row => row.ItemsToCreate), imported, items);
}

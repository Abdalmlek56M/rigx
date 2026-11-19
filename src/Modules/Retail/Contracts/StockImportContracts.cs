namespace HardwareStore.Modules.Retail;

/// <summary>An uploaded workbook: its name (shown in the import history) and its bytes.</summary>
public sealed record StockImportFile(string FileName, byte[] Content);

/// <summary>A category the sheet may name, by code or name (Catalog owns categories, so the Host supplies them).</summary>
public sealed record StockImportCategory(Guid Id, string? Code, string Name);

/// <summary>A brand the sheet may name, by name or slug (Catalog owns brands, so the Host supplies them).</summary>
public sealed record StockImportBrand(Guid Id, string Slug, string Name);

/// <summary>What a row may refer to: the categories, the brands, and the warranty labels from the store settings by their months.</summary>
public sealed record StockImportLookups(IReadOnlyList<StockImportCategory> Categories, IReadOnlyList<StockImportBrand> Brands,
    IReadOnlyDictionary<int, string> WarrantyLabels);

/// <summary>One sheet row as the admin sees it; <paramref name="Errors"/> are Arabic messages, empty when the row is fine.</summary>
public sealed record StockImportRowView(int Row, string ModelName, string Condition, int Quantity, int ItemsToCreate, IReadOnlyList<string> Errors);

public sealed record StockImportedItem(string Code, string Slug, string ModelName);

public sealed record StockImportResult(IReadOnlyList<StockImportRowView> Rows, int ValidRows, int ErrorRows, int ItemsToCreate, bool Imported,
    IReadOnlyList<StockImportedItem>? Items);

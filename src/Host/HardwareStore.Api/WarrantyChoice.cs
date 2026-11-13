using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>Warranty is chosen from the store settings list (Storefront); Retail and Purchasing only keep the label and months.</summary>
public static class WarrantyChoice
{
    public static async Task<IReadOnlyList<WarrantyOption>> LoadOptionsAsync(StoreSettingsService settings, CancellationToken cancellationToken) =>
        (await settings.GetAdminAsync(cancellationToken)).WarrantyOptions.Options;

    /// <summary>No label means no warranty; a label must be one of the options.</summary>
    public static (string? Label, int? Months) Resolve(IReadOnlyList<WarrantyOption> options, string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return (null, null);
        var option = options.FirstOrDefault(entry => entry.Label == label.Trim())
            ?? throw new RetailException(400, "Choose the warranty from the list in the store settings.");
        return (option.Label, option.Months);
    }
}

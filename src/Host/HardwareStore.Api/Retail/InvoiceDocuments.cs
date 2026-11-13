using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>Store identity printed on invoices and quotes (from the store settings); the frontend renders the watermark and the QR.</summary>
public sealed record InvoiceBranding(string StoreName, bool ShowBackgroundLogo, bool ShowWarrantyQr, string? TermsLine,
    IReadOnlyList<ContactLink> Contacts, string? PickupAddress);

/// <summary>An invoice with the store branding, its public link and (when the QR is on) the warranty search link.</summary>
public sealed record InvoiceDocument(InvoiceView Invoice, InvoiceBranding Branding, string InvoiceUrl, string? WarrantyLookupUrl);

public sealed record QuoteDocument(QuoteView Quote, InvoiceBranding Branding);

/// <summary>Builds the documents served by the invoice and quote endpoints.</summary>
public static class InvoiceDocuments
{
    public static InvoiceBranding Branding(StoreSettingsAdminView settings) => new(settings.StoreInfo.Name, settings.Invoice.ShowBackgroundLogo,
        settings.Invoice.ShowWarrantyQr, settings.Invoice.TermsLine, settings.ContactLinks.Links.Where(link => link.ShowOnInvoice).ToList(),
        settings.StoreInfo.PickupAddress);

    /// <summary>Category names for the parts of an invoice (the part type), keyed by category id.</summary>
    public static async Task<IReadOnlyDictionary<Guid, string>> CategoryNamesAsync(TaxonomyService taxonomy, CancellationToken cancellationToken) =>
        (await taxonomy.GetCategoriesAsync(cancellationToken)).ToDictionary(category => category.Id, category => category.Name);

    /// <summary>
    /// Without <paramref name="includeContact"/> the phone is masked and the address removed; without <paramref name="includeSerials"/>
    /// the serial numbers are removed (the public link is the customer's own document and shows everything).
    /// </summary>
    public static InvoiceDocument Build(InvoiceView invoice, StoreSettingsAdminView settings, OrderLinks links,
        IReadOnlyDictionary<Guid, string> categoryNames, bool includeContact = true,
        bool includeSerials = true)
    {
        var shown = invoice with
        {
            PhoneNumber = includeContact ? invoice.PhoneNumber : WarrantyRules.MaskPhone(invoice.PhoneNumber),
            Address = includeContact ? invoice.Address : null,
            Lines = invoice.Lines.Select(line => line with
            {
                Parts = line.Parts.Select(part => part with
                {
                    SerialNumber = includeSerials ? part.SerialNumber : null,
                    Category = part.CategoryId is Guid id && categoryNames.TryGetValue(id, out var name) ? name : null
                }).ToList()
            }).ToList()
        };
        var branding = Branding(settings);
        return new InvoiceDocument(shown, branding, links.InvoiceUrl(invoice.AccessToken),
            branding.ShowWarrantyQr ? links.WarrantyLookupUrl(invoice.Number) : null);
    }
}

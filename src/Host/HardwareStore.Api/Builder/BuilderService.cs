using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>
/// The PC builder (roadmap 2.13). It spans Catalog (slots, compatibility specs), Retail (items, prices, quote) and PcBuilder (rules),
/// so it lives in Host. Checking never throws for items that are gone, so a saved link keeps working.
/// </summary>
public sealed class BuilderService(BuilderCatalogLoader catalogs, BuilderItemService items, QuoteService quotes, StoreSettingsService settings)
{
    private const int OptionPageSize = 24;

    private sealed record Resolved(BuilderItemView Item, int Quantity, BuilderPart Part);

    public async Task<IReadOnlyList<BuilderSlotView>> SlotsAsync(CancellationToken cancellationToken) =>
        (await catalogs.LoadAsync(cancellationToken)).Slots;

    public async Task<BuilderEvaluation> CheckAsync(IReadOnlyList<BuilderPartInput>? parts, CancellationToken cancellationToken)
    {
        var catalog = await catalogs.LoadAsync(cancellationToken);
        return await EvaluateAsync(catalog, parts, cancellationToken);
    }

    /// <summary>Items of one slot with the problems each would cause next to the parts already chosen; sold-out items stay in the list.</summary>
    public async Task<PagedResult<BuilderOptionView>> OptionsAsync(string? slotName, IReadOnlyList<BuilderPartInput>? parts, string? condition, string? search,
        int page, CancellationToken cancellationToken)
    {
        var slot = BuilderSlot.Parse(slotName);
        var catalog = await catalogs.LoadAsync(cancellationToken);
        var categoryIds = catalog.CategoryIdsOf(slot);
        if (categoryIds.Count == 0) return new PagedResult<BuilderOptionView>([], 1, OptionPageSize, 0);
        var (found, _) = await ResolveAsync(catalog, parts, cancellationToken);
        // A slot that takes one model is replaced by the option; a slot that takes several (storage) keeps what is already in it.
        var replaces = BuilderSlot.LimitOf(slot).MaxParts == 1;
        var chosen = found.Select(entry => entry.Part).Where(part => !replaces || part.Slot != slot).ToList();
        // A replacement keeps the quantity of the part it replaces (three fans stay three), so its power and limits are judged at that quantity.
        var quantity = replaces ? found.Select(entry => entry.Part).FirstOrDefault(part => part.Slot == slot)?.Quantity ?? 1 : 1;
        var baseline = BuilderRules.Evaluate(chosen, catalog.CompatibilityKeys).Issues;
        var list = await items.ListOptionsAsync(categoryIds, condition, search, page, OptionPageSize, cancellationToken);
        var options = list.Items.Select(item =>
        {
            var others = chosen.Where(part => part.Code != item.Code).ToList();
            var check = BuilderRules.Evaluate([.. others, new BuilderPart(slot, item.Code, quantity, item.Specs)], catalog.CompatibilityKeys);
            var issues = check.Issues.Where(issue => IsCausedBy(issue, slot, baseline)).ToList();
            return new BuilderOptionView(item, issues, !issues.Any(issue => issue.Level == BuilderIssueLevel.Conflict), item.Available > 0);
        }).ToList();
        return new PagedResult<BuilderOptionView>(options, list.Page, list.PageSize, list.Total);
    }

    /// <summary>"Edit this build": the same models from free stock as a new selection; parts that cannot be matched are listed.</summary>
    public async Task<BuilderFromBuildView> FromBuildAsync(string code, CancellationToken cancellationToken)
    {
        var catalog = await catalogs.LoadAsync(cancellationToken);
        var match = await items.MatchBuildAsync(code, cancellationToken);
        var unmatched = new List<UnmatchedPartView>();
        var heldBySlot = new Dictionary<string, List<(string Code, int Quantity)>>();
        var quantities = new Dictionary<string, int>();
        foreach (var part in match.Parts)
        {
            var found = part.Match;
            var slot = found is null ? null : catalog.SlotOf(found.CategoryId);
            var held = slot is null ? [] : heldBySlot.TryGetValue(slot, out var list) ? list : heldBySlot[slot] = [];
            if (found is null || slot is null || !BuilderRules.Fits(slot, held, found.Code))
            {
                unmatched.Add(new UnmatchedPartView(part.ModelName, part.Condition));
                continue;
            }
            quantities[found.Code] = quantities.GetValueOrDefault(found.Code) + 1;
            held.RemoveAll(entry => entry.Code == found.Code);
            held.Add((found.Code, quantities[found.Code]));
        }
        var evaluation = await EvaluateAsync(catalog, quantities.Select(entry => new BuilderPartInput(entry.Key, entry.Value)).ToList(), cancellationToken);
        return new BuilderFromBuildView(match.Code, match.Title, match.Usage, evaluation, unmatched);
    }

    /// <summary>A shareable quote of the build, priced like an online order (the in-build price when it qualifies).</summary>
    public async Task<QuoteDocument> QuoteAsync(string? name, string? usage, IReadOnlyList<BuilderPartInput>? parts, CancellationToken cancellationToken)
    {
        var title = SavedBuildRules.CleanName(name);
        var parsedUsage = AssemblyUsage.Parse(usage);
        var catalog = await catalogs.LoadAsync(cancellationToken);
        var evaluation = await EvaluateAsync(catalog, parts, cancellationToken);
        if (!evaluation.CanOrder) throw new PcBuilderException(409, "This build cannot be ordered as it is.");
        var quote = await quotes.QuoteAsync(new QuoteRequest(evaluation.CartLines), catalog.BuildPriceRule, cancellationToken);
        return new QuoteDocument(quote with { Title = title, Usage = parsedUsage }, InvoiceDocuments.Branding(await settings.GetAdminAsync(cancellationToken)));
    }

    private async Task<BuilderEvaluation> EvaluateAsync(BuilderCatalog catalog, IReadOnlyList<BuilderPartInput>? input, CancellationToken cancellationToken)
    {
        var (found, missing) = await ResolveAsync(catalog, input, cancellationToken);
        var check = BuilderRules.Evaluate(found.Select(entry => entry.Part).ToList(), catalog.CompatibilityKeys);
        var usesBuildPrice = BuilderRules.UsesBuildPrice(found.Select(entry => entry.Part.Slot));
        var priceOption = usesBuildPrice ? OrderPriceOption.InBuild : OrderPriceOption.Alone;
        var parts = found.Select(entry =>
        {
            var unitPrice = usesBuildPrice ? entry.Item.Prices.InBuild : entry.Item.Prices.Alone;
            return new BuilderPartView(entry.Part.Slot, entry.Item, entry.Quantity, unitPrice, unitPrice * entry.Quantity, entry.Item.Available >= entry.Quantity);
        }).ToList();
        var canOrder = parts.Count > 0 && missing.Count == 0 && !check.HasConflicts && parts.All(part => part.IsAvailable);
        var cartLines = found.Select(entry => new OrderLineRequest(entry.Item.StockItemId, null, entry.Quantity, priceOption)).ToList();
        return new BuilderEvaluation(parts, missing, check.Issues, check.PowerEstimateWatts, check.PsuCapacityWatts, usesBuildPrice, priceOption,
            parts.Sum(part => part.LineTotal), canOrder, cartLines);
    }

    /// <summary>Loads the selected items in slot order; codes that are not published any more are returned apart.</summary>
    private async Task<(IReadOnlyList<Resolved> Found, IReadOnlyList<string> Missing)> ResolveAsync(BuilderCatalog catalog,
        IReadOnlyList<BuilderPartInput>? input, CancellationToken cancellationToken)
    {
        var selection = BuilderRules.NormalizeSelection(input?.Select(part => (part.Code, part.Quantity)).ToList());
        var loaded = await items.GetByCodesAsync(selection.Select(part => part.Code).ToList(), cancellationToken);
        var found = new List<Resolved>();
        var missing = new List<string>();
        foreach (var (code, quantity) in selection)
        {
            var item = loaded.FirstOrDefault(view => view.Code == code);
            if (item is null) missing.Add(code);
            else found.Add(new Resolved(item, quantity, new BuilderPart(catalog.SlotOf(item.CategoryId) ?? string.Empty, code, quantity, item.Specs)));
        }
        var ordered = BuilderRules.InSlotOrder(found.Select(entry => entry.Part).ToList());
        return (ordered.Select(part => found.Single(entry => entry.Part.Code == part.Code)).ToList(), missing);
    }

    /// <summary>
    /// An option's own problems: those that involve its slot, plus a power problem the chosen parts did not have before
    /// (a part that pushes the build over the power supply's output).
    /// </summary>
    private static bool IsCausedBy(BuilderIssue issue, string slot, IReadOnlyList<BuilderIssue> baseline) =>
        issue.Slot == slot || issue.OtherSlot == slot
        || ((issue.Code == BuilderIssueCode.PowerOverCapacity || issue.Code == BuilderIssueCode.PowerNearCapacity)
            && baseline.All(entry => entry.Code != issue.Code));
}

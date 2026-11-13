using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.PcBuilder;

public sealed class PcBuildTemplateService(
    IDbContextFactory<PcBuilderDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PcBuildView>> ListPublicAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var templates = await Query(db).Where(t => t.Status == PcBuildStatus.Published)
            .OrderByDescending(t => t.UpdatedAtUtc).ToListAsync(cancellationToken);
        return templates.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PcBuildView>> ListAdminAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var templates = await Query(db).OrderByDescending(t => t.UpdatedAtUtc).ToListAsync(cancellationToken);
        return templates.Select(Map).ToList();
    }

    public async Task<PcBuildView> GetPublicAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var template = await Query(db).SingleOrDefaultAsync(
            t => t.Id == id && t.Status == PcBuildStatus.Published, cancellationToken)
            ?? throw new PcBuildException(404, "Build template was not found.");
        return Map(template);
    }

    public async Task<PcBuildView> CreateAsync(PcBuildInput input, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var normalized = await NormalizeAsync(db, input, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var template = new PcBuildTemplate
        {
            Id = Guid.CreateVersion7(),
            TitleAr = normalized.TitleAr,
            TitleEn = normalized.TitleEn,
            SummaryAr = normalized.SummaryAr,
            SummaryEn = normalized.SummaryEn,
            Status = PcBuildStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        ApplySlots(template, normalized.Slots);
        db.Templates.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(db, template.Id, cancellationToken);
    }

    public async Task<PcBuildView> UpdateAsync(Guid id, PcBuildUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var normalized = await NormalizeAsync(db, new PcBuildInput(
            request.TitleAr, request.TitleEn, request.SummaryAr, request.SummaryEn, request.Slots), cancellationToken);
        var template = await db.Templates.Include(t => t.Slots).ThenInclude(s => s.Options)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new PcBuildException(404, "Build template was not found.");
        if (template.RowVersion != request.RowVersion)
            throw new PcBuildException(409, "The build template was changed by another user.");
        template.TitleAr = normalized.TitleAr;
        template.TitleEn = normalized.TitleEn;
        template.SummaryAr = normalized.SummaryAr;
        template.SummaryEn = normalized.SummaryEn;
        template.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        template.RowVersion++;
        db.Slots.RemoveRange(template.Slots);
        template.Slots.Clear();
        ApplySlots(template, normalized.Slots);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(db, template.Id, cancellationToken);
    }

    public async Task<PcBuildView> SetPublishedAsync(Guid id, bool published, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var template = await db.Templates.Include(t => t.Slots).ThenInclude(s => s.Options)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new PcBuildException(404, "Build template was not found.");
        if (template.RowVersion != rowVersion)
            throw new PcBuildException(409, "The build template was changed by another user.");
        if (published)
        {
            if (template.Slots.Count == 0)
                throw new PcBuildException(400, "A build template needs at least one slot.");
            var publishedIds = await db.Products.AsNoTracking()
                .Where(product => product.Status == "Published").Select(product => product.Id)
                .ToListAsync(cancellationToken);
            if (template.Slots.SelectMany(slot => slot.Options).Any(option => !publishedIds.Contains(option.ProductId)))
                throw new PcBuildException(400, "Every build option must reference a published product.");
        }
        template.Status = published ? PcBuildStatus.Published : PcBuildStatus.Draft;
        template.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        template.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(template);
    }

    public async Task<PcCompatibilityResult> EvaluateAsync(
        Guid templateId,
        PcBuildCompatibilityRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var template = await Query(db).SingleOrDefaultAsync(
            t => t.Id == templateId && t.Status == PcBuildStatus.Published, cancellationToken)
            ?? throw new PcBuildException(404, "Build template was not found.");
        var selections = request.Selections ?? new Dictionary<string, Guid>();
        var chosen = new Dictionary<string, PcSlotOptionView?>();
        var messages = new List<PcCompatibilityMessage>();
        var total = 0m;

        foreach (var slot in template.Slots.OrderBy(s => s.SortOrder))
        {
            PcBuildSlotOption? option = null;
            if (selections.TryGetValue(slot.SlotKey, out var optionId))
                option = slot.Options.SingleOrDefault(item => item.Id == optionId)
                    ?? throw new PcBuildException(400, $"The selected option does not belong to slot {slot.SlotKey}.");
            option ??= slot.Options.OrderByDescending(item => item.IsDefault).ThenBy(item => item.SortOrder).FirstOrDefault();
            if (option is null)
            {
                if (slot.IsRequired)
                    messages.Add(new PcCompatibilityMessage("Error", "MissingRequiredSlot", slot.SlotKey, null));
                chosen[slot.SlotKey] = null;
                continue;
            }
            total += option.Price;
            chosen[slot.SlotKey] = MapOption(option);
        }

        var cpu = chosen.GetValueOrDefault("CPU");
        var motherboard = chosen.GetValueOrDefault("Motherboard");
        var ram = chosen.GetValueOrDefault("RAM");
        var gpu = chosen.GetValueOrDefault("GPU");
        var psu = chosen.GetValueOrDefault("PSU");

        CompareSpec(cpu, motherboard, ["socket", "cpu socket"], "SocketMismatch", "CPU", "Motherboard", messages);
        CompareMemory(motherboard, ram, messages);
        CompareWattage(cpu, gpu, psu, messages);

        var status = messages.Any(m => m.Level == "Error")
            ? "Incompatible"
            : messages.Any(m => m.Level == "Unconfirmed") ? "Unconfirmed" : "Compatible";
        return new PcCompatibilityResult(status, total, messages);
    }

    private static IQueryable<PcBuildTemplate> Query(PcBuilderDbContext db) =>
        db.Templates.AsNoTracking().Include(t => t.Slots).ThenInclude(s => s.Options);

    private static void CompareSpec(
        PcSlotOptionView? left,
        PcSlotOptionView? right,
        string[] keys,
        string code,
        string leftSlot,
        string rightSlot,
        List<PcCompatibilityMessage> messages)
    {
        if (left is null || right is null) return;
        var leftValue = FindSpec(left, keys);
        var rightValue = FindSpec(right, keys);
        if (leftValue is null || rightValue is null)
        {
            messages.Add(new PcCompatibilityMessage("Unconfirmed", "Missing" + code, leftSlot, rightSlot));
            return;
        }
        messages.Add(string.Equals(Normalize(leftValue), Normalize(rightValue), StringComparison.Ordinal)
            ? new PcCompatibilityMessage("Ok", code, leftSlot, rightSlot)
            : new PcCompatibilityMessage("Error", code, leftSlot, rightSlot));
    }

    private static void CompareMemory(PcSlotOptionView? motherboard, PcSlotOptionView? ram, List<PcCompatibilityMessage> messages)
    {
        if (ram is null || motherboard is null) return;
        var ramType = FindSpec(ram, ["memory type", "ram type", "type"]);
        var boardType = FindSpec(motherboard, ["memory type", "ram type", "supported memory"]);
        var ramGen = ramType is null ? null : Generation(ramType);
        var boardGen = boardType is null ? null : Generation(boardType);
        if (ramGen is null || boardGen is null)
        {
            messages.Add(new PcCompatibilityMessage("Unconfirmed", "RamGenerationUnconfirmed", "RAM", "Motherboard"));
            return;
        }
        messages.Add(ramGen == boardGen
            ? new PcCompatibilityMessage("Ok", "RamGenerationMatches", "RAM", "Motherboard")
            : new PcCompatibilityMessage("Error", "RamGenerationMismatch", "RAM", "Motherboard"));
    }

    private static void CompareWattage(
        PcSlotOptionView? cpu,
        PcSlotOptionView? gpu,
        PcSlotOptionView? psu,
        List<PcCompatibilityMessage> messages)
    {
        if (psu is null) return;
        var psuWatts = Watts(FindSpec(psu, ["wattage", "power", "watt"]));
        var cpuWatts = cpu is null ? null : Watts(FindSpec(cpu, ["tdp", "wattage", "power"]));
        var gpuWatts = gpu is null ? null : Watts(FindSpec(gpu, ["tdp", "wattage", "power", "board power"]));
        if (psuWatts is null || cpuWatts is null || gpuWatts is null)
        {
            messages.Add(new PcCompatibilityMessage("Unconfirmed", "WattageUnconfirmed", "PSU", null));
            return;
        }
        var estimate = cpuWatts.Value + gpuWatts.Value + 80;
        if (estimate > psuWatts.Value)
            messages.Add(new PcCompatibilityMessage("Error", "PsuOverloaded", "PSU", "GPU"));
        else if (estimate > psuWatts.Value * 0.8m)
            messages.Add(new PcCompatibilityMessage("Warning", "PsuHeadroomLow", "PSU", "GPU"));
        else
            messages.Add(new PcCompatibilityMessage("Ok", "PsuSuitable", "PSU", null));
    }

    private static string? FindSpec(PcSlotOptionView option, string[] keys) =>
        option.Specifications.FirstOrDefault(spec => keys.Any(key =>
            spec.Key.Contains(key, StringComparison.OrdinalIgnoreCase) ||
            spec.Key.Contains(key.Split(' ')[0], StringComparison.OrdinalIgnoreCase))).Value;

    private static string Normalize(string value) =>
        new(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static int? Generation(string value)
    {
        var normalized = Normalize(value);
        if (normalized.Contains("DDR5")) return 5;
        if (normalized.Contains("DDR4")) return 4;
        if (normalized.Contains("DDR3")) return 3;
        return null;
    }

    private static decimal? Watts(string? value)
    {
        if (value is null) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return decimal.TryParse(digits, out var parsed) ? parsed : null;
    }

    private static async Task<NormalizedBuild> NormalizeAsync(
        PcBuilderDbContext db,
        PcBuildInput input,
        CancellationToken cancellationToken)
    {
        var slots = input.Slots ?? [];
        if (slots.Count is < 1 or > 12)
            throw new PcBuildException(400, "A build template needs between 1 and 12 slots.");
        if (slots.Select(slot => slot.SlotKey?.Trim().ToUpperInvariant()).Distinct().Count() != slots.Count)
            throw new PcBuildException(400, "Slot keys must be unique.");

        var productIds = slots.SelectMany(slot => slot.Options ?? [])
            .Select(option => option.ProductId).Distinct().ToArray();
        var products = await db.Products.AsNoTracking()
            .Where(product => productIds.Contains(product.Id) && product.Status == "Published")
            .ToListAsync(cancellationToken);
        if (products.Count != productIds.Length)
            throw new PcBuildException(400, "Every build option must reference a published product.");
        var images = await db.ProductImages.AsNoTracking()
            .Where(image => productIds.Contains(image.ProductId))
            .OrderByDescending(image => image.IsPrimary)
            .ToListAsync(cancellationToken);
        var imageKeys = images.GroupBy(image => image.ProductId).ToDictionary(g => g.Key, g => g.First().ThumbnailStorageKey);
        var specs = await db.Specifications.AsNoTracking()
            .Where(spec => productIds.Contains(spec.ProductId))
            .ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(category => category.Id, category => category.Slug, cancellationToken);

        var normalizedSlots = new List<NormalizedSlot>();
        var sort = 0;
        foreach (var slot in slots)
        {
            var key = slot.SlotKey?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!PcSlotKey.All.Contains(key, StringComparer.Ordinal))
                throw new PcBuildException(400, "A supported slot key is required.");
            var options = slot.Options ?? [];
            if (options.Count is < 1 or > 10)
                throw new PcBuildException(400, "Each slot needs between 1 and 10 product options.");
            if (options.Select(option => option.ProductId).Distinct().Count() != options.Count)
                throw new PcBuildException(400, "A slot cannot contain the same product twice.");
            normalizedSlots.Add(new NormalizedSlot(
                key,
                Required(slot.LabelAr, 120, "Arabic slot label"),
                Required(slot.LabelEn, 120, "English slot label"),
                slot.IsRequired,
                sort++,
                options.Select((option, index) =>
                {
                    var product = products.Single(item => item.Id == option.ProductId);
                    var specJson = specs.Where(spec => spec.ProductId == product.Id)
                        .GroupBy(spec => spec.Name)
                        .ToDictionary(group => group.Key, group => group.First().Value);
                    return new NormalizedOption(
                        product.Id, product.Name, product.Slug, product.Sku, product.BasePrice,
                        categories.GetValueOrDefault(product.CategoryId), imageKeys.GetValueOrDefault(product.Id),
                        JsonSerializer.Serialize(specJson), option.IsDefault, index);
                }).ToList()));
        }
        return new NormalizedBuild(
            Required(input.TitleAr, 200, "Arabic title"),
            Required(input.TitleEn, 200, "English title"),
            Optional(input.SummaryAr, 1000, "Arabic summary"),
            Optional(input.SummaryEn, 1000, "English summary"),
            normalizedSlots);
    }

    private static void ApplySlots(PcBuildTemplate template, IReadOnlyList<NormalizedSlot> slots)
    {
        foreach (var slot in slots)
        {
            template.Slots.Add(new PcBuildSlot
            {
                Id = Guid.CreateVersion7(),
                SlotKey = slot.SlotKey,
                LabelAr = slot.LabelAr,
                LabelEn = slot.LabelEn,
                IsRequired = slot.IsRequired,
                SortOrder = slot.SortOrder,
                Options = slot.Options.Select(option => new PcBuildSlotOption
                {
                    Id = Guid.CreateVersion7(),
                    ProductId = option.ProductId,
                    ProductName = option.ProductName,
                    ProductSlug = option.ProductSlug,
                    Sku = option.Sku,
                    Price = option.Price,
                    CategorySlug = option.CategorySlug,
                    PrimaryImageKey = option.PrimaryImageKey,
                    SpecificationsJson = option.SpecificationsJson,
                    IsDefault = option.IsDefault,
                    SortOrder = option.SortOrder
                }).ToList()
            });
        }
    }

    private static async Task<PcBuildView> GetByIdAsync(PcBuilderDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var template = await Query(db).SingleAsync(t => t.Id == id, cancellationToken);
        return Map(template);
    }

    private static PcBuildView Map(PcBuildTemplate template) => new(
        template.Id, template.TitleAr, template.TitleEn, template.SummaryAr, template.SummaryEn,
        template.Status, template.CreatedAtUtc, template.UpdatedAtUtc, template.RowVersion,
        template.Slots.OrderBy(slot => slot.SortOrder).Select(slot => new PcSlotView(
            slot.Id, slot.SlotKey, slot.LabelAr, slot.LabelEn, slot.IsRequired, slot.SortOrder,
            slot.Options.OrderBy(option => option.SortOrder).Select(MapOption).ToList())).ToList());

    private static PcSlotOptionView MapOption(PcBuildSlotOption option) => new(
        option.Id, option.ProductId, option.ProductName, option.ProductSlug, option.Sku,
        option.Price, option.CategorySlug, option.PrimaryImageKey, ParseSpecs(option.SpecificationsJson), option.IsDefault);

    private static IReadOnlyList<KeyValuePair<string, string>> ParseSpecs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)?.ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string Required(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new PcBuildException(400, $"{field} is required.");
        var text = value.Trim();
        if (text.Length > max || text.Contains('<') || text.Contains('>'))
            throw new PcBuildException(400, $"{field} is invalid.");
        return text;
    }

    private static string? Optional(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        if (text.Length > max || text.Contains('<') || text.Contains('>'))
            throw new PcBuildException(400, $"{field} is invalid.");
        return text;
    }

    private sealed record NormalizedBuild(string TitleAr, string TitleEn, string? SummaryAr, string? SummaryEn, IReadOnlyList<NormalizedSlot> Slots);
    private sealed record NormalizedSlot(string SlotKey, string LabelAr, string LabelEn, bool IsRequired, int SortOrder, IReadOnlyList<NormalizedOption> Options);
    private sealed record NormalizedOption(Guid ProductId, string ProductName, string ProductSlug, string Sku, decimal Price, string? CategorySlug, string? PrimaryImageKey, string? SpecificationsJson, bool IsDefault, int SortOrder);
}

public static class PcBuilderRegistration
{
    public static IServiceCollection AddPcBuilderModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddDbContextFactory<PcBuilderDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<PcBuildTemplateService>();
        return services;
    }
}

public sealed class PcBuilderDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<PcBuilderDbContext>
{
    public PcBuilderDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PcBuilderDbContext>()
            .UseNpgsql("Host=localhost;Database=pcbuilder_design;Username=unused;Password=unused")
            .Options);
}

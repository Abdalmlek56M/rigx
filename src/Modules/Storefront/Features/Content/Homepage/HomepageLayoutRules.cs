using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// Homepage layout JSON rules: known sections only (no duplicates), brand/category rails with bounded limits,
/// plain-text titles, GUID references (max 64) that must exist when publishing. Output is re-serialized canonical JSON.
/// </summary>
internal static class HomepageLayoutRules
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new StorefrontException(400, "A homepage layout is required.");
        if (raw.Length > 100_000)
            throw new StorefrontException(400, "The homepage layout is too large.");
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new StorefrontException(400, "The homepage layout must be a JSON object.");
            if (!root.TryGetProperty("version", out var version) || version.GetInt32() != 1)
                throw new StorefrontException(400, "Unsupported homepage layout version.");
            var sections = NormalizeSections(root);
            var rails = NormalizeRails(root);
            return JsonSerializer.Serialize(new { version = 1, sections, rails });
        }
        catch (JsonException)
        {
            throw new StorefrontException(400, "The homepage layout is not valid JSON.");
        }
    }

    private static List<object> NormalizeSections(JsonElement root)
    {
        string[] allowed = ["hero", "categories", "brands", "offers", "discovery", "guide", "latest", "closing"];
        if (!root.TryGetProperty("sections", out var sections) || sections.ValueKind != JsonValueKind.Array)
            throw new StorefrontException(400, "Homepage sections are required.");
        var result = new List<object>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in sections.EnumerateArray())
        {
            var id = section.GetProperty("id").GetString() ?? string.Empty;
            if (!allowed.Contains(id, StringComparer.Ordinal) || !seen.Add(id))
                throw new StorefrontException(400, "The homepage layout contains an unknown or duplicate section.");
            if (!section.TryGetProperty("visible", out var visible) ||
                visible.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new StorefrontException(400, "Each homepage section needs a visible flag.");
            result.Add(new { id, visible = visible.GetBoolean() });
        }
        if (result.Count == 0)
            throw new StorefrontException(400, "At least one homepage section is required.");
        return result;
    }

    private static Dictionary<string, object> NormalizeRails(JsonElement root)
    {
        if (!root.TryGetProperty("rails", out var rails) || rails.ValueKind != JsonValueKind.Object)
            throw new StorefrontException(400, "Homepage rails are required.");
        return new Dictionary<string, object>
        {
            ["brands"] = NormalizeRail(rails, "brands"),
            ["categories"] = NormalizeRail(rails, "categories")
        };
    }

    private static object NormalizeRail(JsonElement rails, string id)
    {
        if (!rails.TryGetProperty(id, out var rail) || rail.ValueKind != JsonValueKind.Object)
            throw new StorefrontException(400, $"Homepage rail '{id}' is required.");
        var contentType = rail.TryGetProperty("contentType", out var type) ? type.GetString() : "brands";
        if (contentType is not ("brands" or "categories" or "products"))
            throw new StorefrontException(400, "A homepage rail has an unsupported content type.");
        var limit = rail.TryGetProperty("limit", out var limitElement) && limitElement.TryGetInt32(out var parsed)
            ? Math.Clamp(parsed, 1, 24)
            : 10;
        return new
        {
            title = ContentText.Optional(ReadString(rail, "title"), 120, "Rail title") ?? "",
            lead = ContentText.Optional(ReadString(rail, "lead"), 300, "Rail lead") ?? "",
            contentType,
            limit,
            itemOrder = ReadIds(rail, "itemOrder"),
            hiddenItemIds = ReadIds(rail, "hiddenItemIds")
        };
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string[] ReadIds(JsonElement rail, string property)
    {
        if (!rail.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            return [];
        var ids = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            var id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (id is null || !Guid.TryParse(id, out _))
                throw new StorefrontException(400, "Homepage rail references must be valid identifiers.");
            if (ids.Count >= 64)
                throw new StorefrontException(400, "A homepage rail has too many references.");
            ids.Add(id);
        }
        return ids.ToArray();
    }

    public static async Task ValidateReferencesAsync(
        StorefrontDbContext db,
        string layoutJson,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(layoutJson);
        var rails = document.RootElement.GetProperty("rails");
        var brandIds = new HashSet<Guid>();
        var categoryIds = new HashSet<Guid>();
        var productIds = new HashSet<Guid>();
        foreach (var railId in new[] { "brands", "categories" })
        {
            var rail = rails.GetProperty(railId);
            var contentType = rail.GetProperty("contentType").GetString();
            var target = contentType switch
            {
                "brands" => brandIds,
                "categories" => categoryIds,
                "products" => productIds,
                _ => brandIds
            };
            foreach (var property in new[] { "itemOrder", "hiddenItemIds" })
            {
                foreach (var item in rail.GetProperty(property).EnumerateArray())
                {
                    if (Guid.TryParse(item.GetString(), out var id))
                        target.Add(id);
                }
            }
        }

        if (brandIds.Count > 0)
        {
            var found = await db.ContentBrands.AsNoTracking()
                .Where(brand => brandIds.Contains(brand.Id))
                .CountAsync(cancellationToken);
            if (found != brandIds.Count)
                throw new StorefrontException(400, "The homepage layout references a brand that does not exist.");
        }
        if (categoryIds.Count > 0)
        {
            var found = await db.WishlistCategories.AsNoTracking()
                .Where(category => categoryIds.Contains(category.Id))
                .CountAsync(cancellationToken);
            if (found != categoryIds.Count)
                throw new StorefrontException(400, "The homepage layout references a category that does not exist.");
        }
        if (productIds.Count > 0)
        {
            var found = await db.WishlistProducts.AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .CountAsync(cancellationToken);
            if (found != productIds.Count)
                throw new StorefrontException(400, "The homepage layout references a product that does not exist.");
        }
    }
}

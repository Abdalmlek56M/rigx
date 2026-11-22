using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HardwareStore.Common.Domain;

/// <summary>One field of an audited row: what it held before and after the save (null side = absent).</summary>
public sealed record AuditFieldChange(string Field, JsonNode? Before, JsonNode? After);

/// <summary>The stored form of a change list and the one way two stored values are compared.</summary>
public static class AuditChanges
{
    public static string Serialize(IReadOnlyList<AuditFieldChange> changes)
    {
        var array = new JsonArray();
        foreach (var change in changes)
            array.Add(new JsonObject { ["field"] = change.Field, ["before"] = change.Before?.DeepClone(), ["after"] = change.After?.DeepClone() });
        return array.ToJsonString();
    }

    public static IReadOnlyList<AuditFieldChange> Parse(string json) =>
        [.. JsonNode.Parse(json)!.AsArray().Select(item => new AuditFieldChange(
            item!["field"]!.GetValue<string>(), item["before"]?.DeepClone(), item["after"]?.DeepClone()))];

    /// <summary>Every id (a GUID written as text, alone or in a list) the changes hold on either side, each once; these are links to other records.</summary>
    public static IReadOnlyList<Guid> LinkedIds(IEnumerable<AuditFieldChange> changes) =>
        [.. changes.SelectMany(change => IdsIn(change.Before).Concat(IdsIn(change.After))).Distinct()];

    private static IEnumerable<Guid> IdsIn(JsonNode? value) => value switch
    {
        JsonArray array => array.SelectMany(IdsIn),
        JsonValue text when text.TryGetValue<string>(out var raw) && Guid.TryParse(raw, out var id) => [id],
        _ => []
    };

    private static decimal Number(JsonValue value) => decimal.Parse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>Structural equality: object key order is ignored and numbers compare by value (1.50 equals 1.5).</summary>
    public static bool ValuesEqual(JsonNode? left, JsonNode? right)
    {
        if (left is null || right is null) return left is null && right is null;
        switch (left)
        {
            case JsonObject leftObject when right is JsonObject rightObject:
                return leftObject.Count == rightObject.Count &&
                       leftObject.All(pair => rightObject.TryGetPropertyValue(pair.Key, out var other) && ValuesEqual(pair.Value, other));
            case JsonArray leftArray when right is JsonArray rightArray:
                return leftArray.Count == rightArray.Count && leftArray.Zip(rightArray).All(pair => ValuesEqual(pair.First, pair.Second));
            case JsonValue leftValue when right is JsonValue rightValue:
                return leftValue.GetValueKind() == JsonValueKind.Number && rightValue.GetValueKind() == JsonValueKind.Number
                    ? Number(leftValue) == Number(rightValue)
                    : leftValue.ToJsonString() == rightValue.ToJsonString();
            default:
                return false;
        }
    }
}

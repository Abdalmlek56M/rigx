using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HardwareStore.Common.Infrastructure.Audit;

/// <summary>The one conversion between a property's CLR value and the JSON the audit log stores (and back, for a restore).</summary>
public static class AuditValues
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static bool IsMasked(IProperty property) =>
        property.ClrType == typeof(byte[]) || property.FindAnnotation(AuditModel.MaskedAnnotation) is not null;

    /// <summary>jsonb text columns are stored as JSON (not as an escaped string); decimals stay exact and instants are ISO text.</summary>
    public static JsonNode? ToJson(IProperty property, object? value)
    {
        if (value is null) return null;
        if (IsMasked(property)) return JsonValue.Create(AuditRules.MaskedValue);
        if (value is string text && property.GetColumnType() == "jsonb") return JsonNode.Parse(text);
        return JsonSerializer.SerializeToNode(value, value.GetType(), Options);
    }

    public static object? FromJson(IProperty property, JsonNode? json)
    {
        if (json is null) return null;
        if (property.ClrType == typeof(string) && property.GetColumnType() == "jsonb") return json.ToJsonString();
        return json.Deserialize(property.ClrType, Options);
    }

    /// <summary>Raw equality used for masked fields, where the JSON is the mask for both sides.</summary>
    public static bool RawEqual(object? left, object? right) =>
        left is byte[] leftBytes && right is byte[] rightBytes ? leftBytes.AsSpan().SequenceEqual(rightBytes) : Equals(left, right);
}

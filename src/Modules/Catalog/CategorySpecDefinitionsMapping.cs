using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HardwareStore.Modules.Catalog;

/// <summary>Stores a category's spec definitions as one jsonb document.</summary>
internal static class CategorySpecDefinitionsMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Configure(PropertyBuilder<List<SpecDefinition>> property) =>
        property.HasColumnName("spec_definitions").HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'[]'::jsonb")
            .HasConversion(
                new ValueConverter<List<SpecDefinition>, string>(value => JsonSerializer.Serialize(value, Json),
                    text => JsonSerializer.Deserialize<List<SpecDefinition>>(text, Json) ?? new List<SpecDefinition>()),
                new ValueComparer<List<SpecDefinition>>(
                    (left, right) => JsonSerializer.Serialize(left, Json) == JsonSerializer.Serialize(right, Json),
                    value => JsonSerializer.Serialize(value, Json).GetHashCode(),
                    value => JsonSerializer.Deserialize<List<SpecDefinition>>(JsonSerializer.Serialize(value, Json), Json)!));
}

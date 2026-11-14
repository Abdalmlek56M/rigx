using System.Text.Json;
using System.Text.Json.Serialization;

namespace HardwareStore.Modules.Storefront;

/// <summary>Stores an article body (a block list) as camelCase JSON in the jsonb columns and revision snapshots.</summary>
internal static class ArticleBlocksJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(IReadOnlyList<ArticleBlock> blocks) => JsonSerializer.Serialize(blocks, Options);

    public static IReadOnlyList<ArticleBlock> Deserialize(string json) =>
        JsonSerializer.Deserialize<List<ArticleBlock>>(json, Options) ?? [];
}

namespace HardwareStore.Modules.Catalog;

/// <summary>
/// Cross-module guard: modules that snapshot media keys can block generic catalog
/// cleanup/deletion without Catalog depending on their persistence.
/// </summary>
public sealed record MediaReferenceInfo(string Module, string EntityType, Guid EntityId, string Description);

public interface IMediaReferenceGuard
{
    Task<IReadOnlyList<MediaReferenceInfo>> FindReferencesAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}

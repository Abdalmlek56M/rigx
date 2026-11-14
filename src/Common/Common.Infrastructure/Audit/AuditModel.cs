using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwareStore.Common.Infrastructure.Audit;

/// <summary>
/// The two model switches of the audit log. Everything is audited by default; a kind that is technical or high-volume opts out,
/// and a field that holds a secret is shown as <see cref="HardwareStore.Common.Domain.AuditRules.MaskedValue"/>.
/// </summary>
public static class AuditModel
{
    internal const string ExcludedAnnotation = "Rigx:AuditExcluded";
    internal const string MaskedAnnotation = "Rigx:AuditMasked";

    public static EntityTypeBuilder<T> ExcludeFromAudit<T>(this EntityTypeBuilder<T> entity) where T : class =>
        entity.HasAnnotation(ExcludedAnnotation, true);

    /// <summary>Opts several already-configured entity types out; a type missing from the model is a mistake, so it throws.</summary>
    public static ModelBuilder ExcludeFromAudit(this ModelBuilder model, params Type[] entityTypes)
    {
        foreach (var type in entityTypes)
            (model.Model.FindEntityType(type) ?? throw new InvalidOperationException($"{type.Name} is not part of this model.")).SetAnnotation(ExcludedAnnotation, true);
        return model;
    }

    public static PropertyBuilder<T> AuditMasked<T>(this PropertyBuilder<T> property) =>
        property.HasAnnotation(MaskedAnnotation, true);
}

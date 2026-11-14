using System.Text.Json.Nodes;
using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace HardwareStore.Common.Infrastructure.Audit;

/// <summary>
/// Restores the allowlisted fields of one entity kind over its own context: reads the current values, writes earlier ones back
/// through normal tracking (so the interceptor logs the restore) and bumps the row version when the kind has one.
/// </summary>
public sealed class DbContextAuditRestoreTarget<TContext, TEntity> : IAuditRestoreTarget
    where TContext : DbContext
    where TEntity : class
{
    private const string RowVersion = "RowVersion";
    private readonly IDbContextFactory<TContext> factory;

    public DbContextAuditRestoreTarget(IDbContextFactory<TContext> factory, IReadOnlyCollection<string> restorableFields)
    {
        this.factory = factory;
        RestorableFields = restorableFields;
        using var context = factory.CreateDbContext();
        var type = context.Model.FindEntityType(typeof(TEntity)) ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the model.");
        Module = typeof(TContext).Name.Replace("DbContext", string.Empty, StringComparison.Ordinal);
        Entity = type.GetTableName()!;
        var unknown = restorableFields.FirstOrDefault(field => type.FindProperty(field) is null);
        if (unknown is not null) throw new InvalidOperationException($"{typeof(TEntity).Name} has no field {unknown}.");
    }

    public string Module { get; }
    public string Entity { get; }
    public IReadOnlyCollection<string> RestorableFields { get; }

    public async Task<IReadOnlyDictionary<string, JsonNode?>?> ReadCurrentAsync(string entityId, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await FindAsync(context, entityId, cancellationToken);
        if (entity is null) return null;
        var entry = context.Entry(entity);
        return RestorableFields.ToDictionary(field => field, field => AuditValues.ToJson(entry.Property(field).Metadata, entry.Property(field).CurrentValue));
    }

    public async Task ApplyAsync(string entityId, IReadOnlyDictionary<string, JsonNode?> values, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await FindAsync(context, entityId, cancellationToken) ?? throw new AuditException(404, "The record no longer exists.");
        var entry = context.Entry(entity);
        foreach (var (field, json) in values)
            entry.Property(field).CurrentValue = AuditValues.FromJson(entry.Property(field).Metadata, json);
        if (entry.Metadata.FindProperty(RowVersion) is { } version)
            entry.Property(RowVersion).CurrentValue = Convert.ChangeType((Convert.ToInt64(entry.Property(RowVersion).CurrentValue) + 1), version.ClrType);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation })
        {
            throw new AuditException(409, "An earlier value is no longer allowed, because another record uses it now.");
        }
    }

    private static async Task<TEntity?> FindAsync(TContext context, string entityId, CancellationToken cancellationToken)
    {
        var key = context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties.Single();
        var value = key.ClrType == typeof(Guid) ? Guid.TryParse(entityId, out var guid) ? (object)guid : null : Convert.ChangeType(entityId, key.ClrType);
        return value is null ? null : await context.Set<TEntity>().FindAsync([value], cancellationToken);
    }
}

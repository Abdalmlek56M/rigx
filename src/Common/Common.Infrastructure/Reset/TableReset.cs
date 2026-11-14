using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Common.Infrastructure.Reset;

/// <summary>
/// Empties an explicit list of tables of one context, in one transaction, children before parents (read from the model's
/// foreign keys). Only the listed tables are touched: a table outside the list that still points at one of them makes the
/// database refuse the delete, which is the safety net against wiping kept data.
/// </summary>
public static class TableReset
{
    public static async Task<int> DeleteAllAsync(DbContext db, IReadOnlyCollection<string> tables, CancellationToken cancellationToken = default)
    {
        var ordered = ChildrenFirst(db.Model, tables);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        var deleted = 0;
        foreach (var table in ordered)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = $"DELETE FROM \"{table}\"";
            deleted += await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }

    /// <summary>The listed tables ordered so that a table comes before the tables it references.</summary>
    public static IReadOnlyList<string> ChildrenFirst(IModel model, IReadOnlyCollection<string> tables)
    {
        var known = model.GetEntityTypes().Select(type => type.GetTableName()).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var unknown = tables.FirstOrDefault(table => !known.Contains(table));
        if (unknown is not null)
            throw new InvalidOperationException($"Table '{unknown}' is not part of this context.");

        var referencedBy = tables.ToDictionary(table => table, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var type in model.GetEntityTypes())
            foreach (var key in type.GetForeignKeys())
            {
                var child = key.DeclaringEntityType.GetTableName();
                var parent = key.PrincipalEntityType.GetTableName();
                if (child is not null && parent is not null && child != parent && referencedBy.TryGetValue(parent, out var children) && referencedBy.ContainsKey(child))
                    children.Add(child);
            }

        var remaining = tables.ToHashSet(StringComparer.Ordinal);
        var ordered = new List<string>(remaining.Count);
        while (remaining.Count > 0)
        {
            var next = remaining.Where(table => !referencedBy[table].Overlaps(remaining)).Order(StringComparer.Ordinal).ToList();
            if (next.Count == 0)
                throw new InvalidOperationException("The tables to reset reference each other in a cycle.");
            ordered.AddRange(next);
            remaining.ExceptWith(next);
        }
        return ordered;
    }
}

using System.Data.Common;
using System.Runtime.CompilerServices;
using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Common.Infrastructure.Audit;

/// <summary>
/// Writes the business audit log in the same transaction as the save it describes: what changed (per field, before and after),
/// who did it and from where. Rows are inserted with plain SQL once the keys exist; a failed save leaves no audit row.
/// A transaction the caller already opened is joined and never committed here; otherwise one is opened and owned for the save.
/// </summary>
public sealed class AuditSaveChangesInterceptor(IAuditActor actor, TimeProvider time) : SaveChangesInterceptor
{
    private const int RowsPerInsert = 400;

    private readonly ConditionalWeakTable<DbContext, SaveState> states = new();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        // The same interceptor registered twice (a test overriding a module registration) must not open two transactions.
        if (context is null || states.TryGetValue(context, out _)) return result;
        var valuesRestore = AuditRestoreScope.EntryId;
        var pending = AuditChangeSet.Collect(context, valuesRestore is not null);
        if (pending.Count == 0) return result;
        var owned = context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync(cancellationToken) : null;
        states.AddOrUpdate(context, new SaveState(pending, owned, valuesRestore));
        return result;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        eventData.Context is { } context && AuditChangeSet.Collect(context, false).Count > 0
            ? throw new NotSupportedException("Audited contexts must save with SaveChangesAsync.")
            : result;

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (!Take(eventData.Context, out var state)) return result;
        try
        {
            await InsertAsync(eventData.Context!, state, cancellationToken);
            if (state.Owned is not null) await state.Owned.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollBackAsync(state);
            throw;
        }
        finally
        {
            if (state.Owned is not null) await state.Owned.DisposeAsync();
        }
        return result;
    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (Take(eventData.Context, out var state)) await RollBackAsync(state);
    }

    public override async Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        if (Take(eventData.Context, out var state)) await RollBackAsync(state);
    }

    private bool Take(DbContext? context, out SaveState state)
    {
        state = null!;
        if (context is null || !states.TryGetValue(context, out var found)) return false;
        states.Remove(context);
        state = found;
        return true;
    }

    private static async Task RollBackAsync(SaveState state)
    {
        if (state.Owned is null) return;
        await state.Owned.RollbackAsync(CancellationToken.None);
        await state.Owned.DisposeAsync();
    }

    private async Task InsertAsync(DbContext context, SaveState state, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction!.GetDbTransaction();
        var module = context.GetType().Name.Replace("DbContext", string.Empty, StringComparison.Ordinal);
        var now = time.GetUtcNow();
        foreach (var batch in state.Pending.Chunk(RowsPerInsert))
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            var values = new List<string>(batch.Length);
            foreach (var (item, index) in batch.Select((item, index) => (item, index)))
            {
                values.Add($"(@i{index}, @t{index}, @k{index}, @u{index}, @ip{index}, @ua{index}, @c{index}, @m{index}, @e{index}, @id{index}, @a{index}, CAST(@ch{index} AS jsonb), @r{index})");
                Add(command, $"i{index}", Guid.CreateVersion7(now));
                Add(command, $"t{index}", now.ToUniversalTime());
                Add(command, $"k{index}", actor.Kind.ToString());
                Add(command, $"u{index}", actor.UserId);
                Add(command, $"ip{index}", actor.IpAddress);
                Add(command, $"ua{index}", AuditRules.ShortenUserAgent(actor.UserAgent));
                Add(command, $"c{index}", actor.CorrelationId);
                Add(command, $"m{index}", module);
                Add(command, $"e{index}", item.Entity);
                Add(command, $"id{index}", item.EntityId);
                Add(command, $"a{index}", item.Action);
                Add(command, $"ch{index}", AuditChanges.Serialize(item.Changes));
                Add(command, $"r{index}", item.Action == AuditAction.ValuesRestored ? state.RestoredFrom : null);
            }
            command.CommandText = $"INSERT INTO {AuditTable.Name} ({AuditTable.Id}, {AuditTable.OccurredAtUtc}, {AuditTable.ActorKind}, {AuditTable.ActorUserId}, " +
                $"{AuditTable.ActorIp}, {AuditTable.ActorUserAgent}, {AuditTable.CorrelationId}, {AuditTable.Module}, {AuditTable.Entity}, {AuditTable.EntityId}, " +
                $"{AuditTable.Action}, {AuditTable.Changes}, {AuditTable.RestoredFromEntryId}) VALUES {string.Join(", ", values)}";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void Add(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private sealed record SaveState(IReadOnlyList<PendingAudit> Pending, IDbContextTransaction? Owned, Guid? RestoredFrom);
}

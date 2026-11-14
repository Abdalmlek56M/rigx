namespace HardwareStore.Common.Infrastructure.Audit;

/// <summary>While open, the saves of this async flow are logged as a restore of the given audit entry.</summary>
public static class AuditRestoreScope
{
    private static readonly AsyncLocal<Guid?> Current = new();

    public static Guid? EntryId => Current.Value;

    public static IDisposable Begin(Guid entryId)
    {
        var previous = Current.Value;
        Current.Value = entryId;
        return new Scope(previous);
    }

    private sealed class Scope(Guid? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}

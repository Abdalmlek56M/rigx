namespace HardwareStore.Modules.Identity;

/// <summary>Rules for merging a duplicate customer account (source) into the one to keep (target).</summary>
public static class CustomerMergeRules
{
    public static void EnsureCanMerge(MergeParty target, MergeParty source)
    {
        if (target.Id == source.Id)
            throw new IdentityException(400, "An account cannot be merged into itself.");
        if (!target.IsCustomer || !source.IsCustomer)
            throw new IdentityException(409, "Only customer accounts can be merged.");
        if (target.MergedIntoAccountId is not null || source.MergedIntoAccountId is not null)
            throw new IdentityException(409, "A merged account cannot be merged again.");
        if (!target.IsActive)
            throw new IdentityException(409, "The account to keep must be active.");
    }

    /// <summary>The target's note stays first; the source's note is appended under a line naming its person code.</summary>
    public static string? CombineNotes(string? targetNote, string? sourceNote, string sourcePersonCode)
    {
        if (string.IsNullOrWhiteSpace(sourceNote)) return targetNote;
        if (string.IsNullOrWhiteSpace(targetNote)) return Limit(sourceNote);
        return Limit($"{targetNote}\n--- {sourcePersonCode} ---\n{sourceNote}");
    }

    private static string Limit(string note) =>
        note.Length <= CustomerAccounts.MaxNoteLength ? note : note[..CustomerAccounts.MaxNoteLength];
}

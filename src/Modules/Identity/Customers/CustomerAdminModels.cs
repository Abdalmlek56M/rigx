namespace HardwareStore.Modules.Identity;

public sealed record CustomerListRow(Guid Id, string PersonCode, string Name, string Email, string? Phone, bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record CustomerListPage(IReadOnlyList<CustomerListRow> Items, int Page, int PageSize, int Total);

/// <summary>The Identity part of the customer 360 view.</summary>
public sealed record CustomerAccountView(Guid Id, string PersonCode, string Name, string Email, string? Phone, bool IsActive,
    bool IsEmailVerified, DateTimeOffset CreatedAtUtc, string? Note, string? DefaultGovernorate, int ActiveSessions,
    Guid? MergedIntoAccountId);

public sealed record CustomerNoteRequest(string? Note);

public sealed record CustomerMergeRequest(Guid SourceAccountId);

/// <summary>How many records the other modules moved to the target account (for the audit entry).</summary>
public sealed record CustomerMergeCounts(int Orders, int Warranties, int Requests, int SavedBuilds);

/// <summary>The facts the merge rules need about one account.</summary>
public sealed record MergeParty(Guid Id, bool IsCustomer, bool IsActive, Guid? MergedIntoAccountId);

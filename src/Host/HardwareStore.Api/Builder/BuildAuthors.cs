using HardwareStore.Modules.Identity;

/// <summary>What a shared build says about who made it: the creator's first name only (nothing else about an account is public).</summary>
public sealed class BuildAuthors(IdentityProfileService profiles)
{
    /// <summary>The first word of the creator's full name, or null for an anonymous build (no owner) or an account that is gone.</summary>
    public async Task<string?> FirstNameAsync(Guid? ownerId, CancellationToken cancellationToken)
    {
        if (ownerId is null) return null;
        var fullName = await profiles.GetFullNameAsync(ownerId.Value, cancellationToken);
        return fullName?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
    }
}

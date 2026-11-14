using HardwareStore.Common.Domain;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Trash retention rules and the owner-only purge permission, without a database.</summary>
public sealed class TrashRulesTests
{
    private sealed class Item : ISoftDeletable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTimeOffset? DeletedAtUtc { get; set; }
        public Guid? DeletedByUserId { get; set; }
    }

    [Fact]
    public void Items_are_kept_30_days_and_purge_is_due_for_anything_deleted_before_that()
    {
        var now = new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

        Assert.Equal(30, TrashRules.RetentionDays);
        Assert.Equal(new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero), TrashRules.PurgeDueBeforeUtc(now));
    }

    [Fact]
    public void Moving_to_the_trash_records_when_and_who_and_restoring_clears_both()
    {
        var item = new Item();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        TrashRules.MoveToTrash(item, actor, now);
        Assert.Equal((now, actor), (item.DeletedAtUtc, item.DeletedByUserId));

        TrashRules.Restore(item);
        Assert.Equal((null, null), (item.DeletedAtUtc, item.DeletedByUserId));
    }

    [Fact]
    public void Purge_is_owner_only_and_cannot_be_put_in_a_custom_role()
    {
        var owner = new IdentityActor(Guid.NewGuid(), true, PermissionCatalog.All);

        Assert.Contains("System.Purge", PermissionCatalog.OwnerOnly);
        Assert.Contains("System.Trash", PermissionCatalog.All);
        Assert.Equal(409, Assert.Throws<IdentityException>(() => IdentityDelegationPolicy.EnsureCanCreateRole(owner, ["System.Purge"])).StatusCode);
        IdentityDelegationPolicy.EnsureCanCreateRole(owner, ["System.Trash"]);
        var role = new IdentityRoleGrant(Guid.NewGuid(), "Editors", new HashSet<string>());
        Assert.Equal(409, Assert.Throws<IdentityException>(() => IdentityDelegationPolicy.EnsureCanEditRole(owner, role, ["System.Purge"])).StatusCode);
    }
}

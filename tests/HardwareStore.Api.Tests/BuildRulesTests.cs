using HardwareStore.Modules.PcBuilder;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Who may open a build, when a public build is listed or goes back to the queue, and what a decision may be (no database).</summary>
public sealed class BuildRulesTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    [Fact]
    public void A_private_build_opens_for_its_owner_only_and_every_other_build_for_whoever_has_the_link()
    {
        Assert.True(BuildVisibilityRules.CanView(BuildVisibility.Private, Owner, Owner));
        Assert.False(BuildVisibilityRules.CanView(BuildVisibility.Private, Owner, Stranger));
        Assert.False(BuildVisibilityRules.CanView(BuildVisibility.Private, Owner, null));
        Assert.False(BuildVisibilityRules.CanView(BuildVisibility.Private, null, null));
        foreach (var visibility in new[] { BuildVisibility.Link, BuildVisibility.Public })
            Assert.True(BuildVisibilityRules.CanView(visibility, Owner, null));
    }

    [Fact]
    public void Only_a_signed_in_creator_can_keep_a_build_private_or_make_it_public()
    {
        BuildVisibilityRules.EnsureAllowed(BuildVisibility.Link, ownerSignedIn: false);
        BuildVisibilityRules.EnsureAllowed(BuildVisibility.Private, ownerSignedIn: true);
        BuildVisibilityRules.EnsureAllowed(BuildVisibility.Public, ownerSignedIn: true);
        foreach (var visibility in new[] { BuildVisibility.Private, BuildVisibility.Public })
            Assert.Equal(400, Assert.Throws<PcBuilderException>(() => BuildVisibilityRules.EnsureAllowed(visibility, ownerSignedIn: false)).StatusCode);
        Assert.Equal(BuildVisibility.Link, BuildVisibilityRules.Default);
    }

    [Theory]
    // visibility, wasPublic, contentChanged, current  ->  status after the owner saves
    [InlineData(BuildVisibility.Public, false, true, null, BuildModerationStatus.Pending)]
    [InlineData(BuildVisibility.Public, false, false, null, BuildModerationStatus.Pending)]
    [InlineData(BuildVisibility.Public, true, true, BuildModerationStatus.Approved, BuildModerationStatus.Pending)]
    [InlineData(BuildVisibility.Public, true, false, BuildModerationStatus.Approved, BuildModerationStatus.Approved)]
    [InlineData(BuildVisibility.Public, false, false, BuildModerationStatus.Approved, BuildModerationStatus.Approved)]
    [InlineData(BuildVisibility.Public, true, false, BuildModerationStatus.Rejected, BuildModerationStatus.Rejected)]
    [InlineData(BuildVisibility.Public, false, false, BuildModerationStatus.Rejected, BuildModerationStatus.Pending)]
    [InlineData(BuildVisibility.Public, true, true, BuildModerationStatus.Rejected, BuildModerationStatus.Pending)]
    [InlineData(BuildVisibility.Public, true, false, BuildModerationStatus.Pending, BuildModerationStatus.Pending)]
    [InlineData(BuildVisibility.Link, true, false, BuildModerationStatus.Approved, BuildModerationStatus.Approved)]
    [InlineData(BuildVisibility.Link, true, true, BuildModerationStatus.Approved, BuildModerationStatus.Pending)]
    [InlineData(BuildVisibility.Link, false, true, null, null)]
    [InlineData(BuildVisibility.Private, false, false, null, null)]
    public void A_public_build_is_moderated_and_an_edit_sends_it_back_to_the_queue(BuildVisibility visibility, bool wasPublic, bool contentChanged,
        BuildModerationStatus? current, BuildModerationStatus? expected) =>
        Assert.Equal(expected, BuildModerationRules.AfterOwnerSave(visibility, wasPublic, contentChanged, current));

    [Fact]
    public void A_decision_is_approve_or_reject_and_only_on_a_public_build()
    {
        BuildModerationRules.EnsureCanDecide(BuildVisibility.Public, BuildModerationStatus.Approved);
        BuildModerationRules.EnsureCanDecide(BuildVisibility.Public, BuildModerationStatus.Rejected);
        Assert.Equal(400, Assert.Throws<PcBuilderException>(() => BuildModerationRules.EnsureCanDecide(BuildVisibility.Public, BuildModerationStatus.Pending)).StatusCode);
        Assert.Equal(409, Assert.Throws<PcBuilderException>(() => BuildModerationRules.EnsureCanDecide(BuildVisibility.Link, BuildModerationStatus.Approved)).StatusCode);
        Assert.Equal(409, Assert.Throws<PcBuilderException>(() => BuildModerationRules.EnsureCanDecide(BuildVisibility.Private, BuildModerationStatus.Rejected)).StatusCode);
    }

    [Fact]
    public void An_account_can_heart_builds_up_to_the_limit()
    {
        BuildReactionRules.EnsureRoomForAnotherHeart(BuildReactionRules.MaxHearts - 1);
        Assert.Equal(409, Assert.Throws<PcBuilderException>(() => BuildReactionRules.EnsureRoomForAnotherHeart(BuildReactionRules.MaxHearts)).StatusCode);
    }
}

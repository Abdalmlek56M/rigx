using System.Text.Json.Nodes;
using HardwareStore.Common.Domain;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Business audit log rules (roadmap 2.11): which action a save is, what can be restored, query limits, value comparison.</summary>
public sealed class AuditRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static AuditFieldChange Change(string field, JsonNode? before, JsonNode? after) => new(field, before, after);

    [Fact]
    public void An_edit_is_Updated_unless_it_moves_to_or_from_the_trash_or_restores_values()
    {
        Assert.Equal(AuditAction.Updated, AuditRules.ActionForUpdate(null, null, false, 1));
        Assert.Equal(AuditAction.ValuesRestored, AuditRules.ActionForUpdate(null, null, true, 1));
        Assert.Equal(AuditAction.SoftDeleted, AuditRules.ActionForUpdate(null, Now, false, 0));
        Assert.Equal(AuditAction.Restored, AuditRules.ActionForUpdate(Now, null, false, 0));
        Assert.Equal(AuditAction.Updated, AuditRules.ActionForUpdate(Now, Now, false, 2));
    }

    [Fact]
    public void An_edit_with_no_real_field_change_is_not_logged()
    {
        Assert.Null(AuditRules.ActionForUpdate(null, null, false, 0));
        Assert.Null(AuditRules.ActionForUpdate(null, null, true, 0));
    }

    [Theory]
    [InlineData("RowVersion", true)]
    [InlineData("UpdatedAtUtc", true)]
    [InlineData("DeletedAtUtc", true)]
    [InlineData("DeletedByUserId", true)]
    [InlineData("Price", false)]
    [InlineData("CreatedAtUtc", false)]
    public void Bookkeeping_fields_never_appear_in_a_change_list(string field, bool bookkeeping) =>
        Assert.Equal(bookkeeping, AuditRules.IsBookkeeping(field));

    [Fact]
    public void A_long_user_agent_is_cut_to_256_characters_and_a_blank_one_is_dropped()
    {
        Assert.Equal(AuditRules.MaxUserAgentLength, AuditRules.ShortenUserAgent(new string('x', 600))!.Length);
        Assert.Equal("Mozilla", AuditRules.ShortenUserAgent("Mozilla"));
        Assert.Null(AuditRules.ShortenUserAgent("  "));
        Assert.Null(AuditRules.ShortenUserAgent(null));
    }

    [Theory]
    [InlineData(0, 25, null)]
    [InlineData(1, 0, null)]
    [InlineData(1, 101, null)]
    [InlineData(1, 25, "Exploded")]
    public void Paging_and_action_filters_outside_the_limits_are_rejected(int page, int pageSize, string? action) =>
        Assert.Equal(400, Assert.Throws<AuditException>(() => AuditRules.ValidateQuery(page, pageSize, action)).StatusCode);

    [Fact]
    public void Valid_paging_and_every_known_action_pass()
    {
        AuditRules.ValidateQuery(1, AuditRules.MaxPageSize, null);
        foreach (var action in AuditAction.All) AuditRules.ValidateQuery(3, 10, action);
    }

    [Fact]
    public void Only_an_edit_of_an_allowlisted_kind_can_be_restored_and_only_for_its_restorable_fields()
    {
        IReadOnlyList<AuditFieldChange> changes = [Change("Price", 100, 150), Change("Code", "A", "B")];
        string[] restorable = ["Price", "Name"];

        var allowed = AuditRules.RestorableChanges(AuditAction.Updated, restorable, changes);

        Assert.Equal(["Price"], allowed.Select(change => change.Field));
        Assert.Empty(AuditRules.RestorableChanges(AuditAction.Created, restorable, changes));
        Assert.Empty(AuditRules.RestorableChanges(AuditAction.ValuesRestored, restorable, changes));
        Assert.Empty(AuditRules.RestorableChanges(AuditAction.Updated, null, changes));
        Assert.Empty(AuditRules.RestorableChanges(AuditAction.Updated, ["Name"], changes));
    }

    [Fact]
    public void A_restore_is_refused_with_the_fields_that_changed_since()
    {
        IReadOnlyList<AuditFieldChange> restorable = [Change("Price", 100, 150), Change("Name", "Old", "New")];
        var current = new Dictionary<string, JsonNode?> { ["Price"] = JsonValue.Create(175), ["Name"] = JsonValue.Create("New") };

        var error = Assert.Throws<AuditException>(() => AuditRules.EnsureUnchangedSince(restorable, current));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal(["Price"], error.Errors!["fields"]);
    }

    [Fact]
    public void A_restore_passes_when_every_field_still_holds_the_value_the_entry_saved()
    {
        IReadOnlyList<AuditFieldChange> restorable = [Change("Price", 100m, 150.00m), Change("Note", "a", null)];
        var current = new Dictionary<string, JsonNode?> { ["Price"] = JsonValue.Create(150), ["Note"] = null };

        AuditRules.EnsureUnchangedSince(restorable, current);
    }

    [Fact]
    public void Stored_values_compare_by_structure_and_number_value()
    {
        Assert.True(AuditChanges.ValuesEqual(JsonNode.Parse("1.50"), JsonNode.Parse("1.5")));
        Assert.True(AuditChanges.ValuesEqual(JsonNode.Parse("""{"a":1,"b":[1,2]}"""), JsonNode.Parse("""{"b":[1,2],"a":1}""")));
        Assert.False(AuditChanges.ValuesEqual(JsonNode.Parse("""{"a":1}"""), JsonNode.Parse("""{"a":2}""")));
        Assert.False(AuditChanges.ValuesEqual(JsonNode.Parse("[1,2]"), JsonNode.Parse("[2,1]")));
        Assert.False(AuditChanges.ValuesEqual(JsonNode.Parse("\"1\""), JsonNode.Parse("1")));
        Assert.True(AuditChanges.ValuesEqual(null, null));
        Assert.False(AuditChanges.ValuesEqual(null, JsonNode.Parse("0")));
    }

    [Fact]
    public void A_change_list_round_trips_through_its_stored_json_with_nulls_kept()
    {
        IReadOnlyList<AuditFieldChange> changes = [Change("Price", 100.25m, 150m), Change("Note", null, "hello")];

        var parsed = AuditChanges.Parse(AuditChanges.Serialize(changes));

        Assert.Equal(["Price", "Note"], parsed.Select(change => change.Field));
        Assert.True(AuditChanges.ValuesEqual(parsed[0].Before, JsonValue.Create(100.25m)));
        Assert.Null(parsed[1].Before);
        Assert.Equal("hello", parsed[1].After!.GetValue<string>());
    }
}

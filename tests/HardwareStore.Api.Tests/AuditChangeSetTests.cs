using System.Text.Json.Nodes;
using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Building the audit change set from the change tracker (no database): per-field before/after, masking, exclusion, bookkeeping.</summary>
public sealed class AuditChangeSetTests
{
    private sealed class Widget
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string? Note { get; set; }
        public string Secret { get; set; } = "";
        public byte[] Hash { get; set; } = [];
        public string Specs { get; set; } = "{}";
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public long RowVersion { get; set; } = 1;
    }

    private sealed class Counter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Value { get; set; }
    }

    private sealed class Draft : ISoftDeletable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public DateTimeOffset? DeletedAtUtc { get; set; }
        public Guid? DeletedByUserId { get; set; }
    }

    private sealed class Line
    {
        public Guid OrderId { get; set; }
        public int Number { get; set; }
        public string Text { get; set; } = "";
    }

    private sealed class WidgetContext() : DbContext(new DbContextOptionsBuilder<WidgetContext>().UseNpgsql("Host=unused;Database=unused").Options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Widget>(entity =>
            {
                entity.ToTable("widgets");
                entity.Property(widget => widget.Secret).AuditMasked();
                entity.Property(widget => widget.Specs).HasColumnType("jsonb");
                entity.Property(widget => widget.Price).HasPrecision(18, 2);
            });
            model.Entity<Counter>().ToTable("counters");
            model.Entity<Draft>().ToTable("drafts");
            model.Entity<Line>(entity =>
            {
                entity.ToTable("lines");
                entity.HasKey(line => new { line.OrderId, line.Number });
            });
            model.ExcludeFromAudit(typeof(Counter));
        }
    }

    private static Widget Saved(WidgetContext context, Action<Widget>? tweak = null)
    {
        var widget = new Widget { Name = "GPU", Price = 100m, Secret = "s1", Hash = [1, 2], Specs = """{"a":1}""" };
        tweak?.Invoke(widget);
        context.Attach(widget);
        return widget;
    }

    [Fact]
    public void A_new_row_logs_its_non_empty_fields_as_after_only_with_the_key_in_entity_id()
    {
        using var context = new WidgetContext();
        var widget = new Widget { Name = "GPU", Price = 100.50m, Note = null, Specs = """{"a":1}""" };
        context.Add(widget);

        var created = Assert.Single(AuditChangeSet.Collect(context, false));

        Assert.Equal(AuditAction.Created, created.Action);
        Assert.Equal("widgets", created.Entity);
        Assert.Equal(widget.Id.ToString(), created.EntityId);
        Assert.All(created.Changes, change => Assert.Null(change.Before));
        Assert.DoesNotContain(created.Changes, change => change.Field is "Id" or "Note" or "RowVersion" or "UpdatedAtUtc");
        Assert.Equal("100.50", created.Changes.Single(change => change.Field == "Price").After!.ToJsonString());
        Assert.Equal("""{"a":1}""", created.Changes.Single(change => change.Field == "Specs").After!.ToJsonString());
    }

    [Fact]
    public void An_edit_logs_only_the_fields_that_really_changed_with_both_sides()
    {
        using var context = new WidgetContext();
        var widget = Saved(context);
        widget.Price = 150m;
        widget.Name = "GPU";
        widget.Note = "fresh";

        var updated = Assert.Single(AuditChangeSet.Collect(context, false));

        Assert.Equal(AuditAction.Updated, updated.Action);
        Assert.Equal(["Note", "Price"], updated.Changes.Select(change => change.Field).Order());
        var price = updated.Changes.Single(change => change.Field == "Price");
        Assert.Equal(100m, price.Before!.GetValue<decimal>());
        Assert.Equal(150m, price.After!.GetValue<decimal>());
        Assert.Null(updated.Changes.Single(change => change.Field == "Note").Before);
    }

    [Fact]
    public void An_edit_of_only_bookkeeping_fields_or_to_the_same_value_is_not_logged()
    {
        using var context = new WidgetContext();
        var widget = Saved(context);
        widget.RowVersion++;
        widget.UpdatedAtUtc = DateTimeOffset.UtcNow;
        widget.Price = 100m;

        Assert.Empty(AuditChangeSet.Collect(context, false));
    }

    [Fact]
    public void A_jsonb_text_field_is_stored_as_json_and_an_equal_rewrite_is_not_a_change()
    {
        using var context = new WidgetContext();
        var widget = Saved(context);
        widget.Specs = """{ "a": 1 }""";
        Assert.Empty(AuditChangeSet.Collect(context, false));

        widget.Specs = """{"a":2}""";
        var change = Assert.Single(Assert.Single(AuditChangeSet.Collect(context, false)).Changes);
        Assert.Equal("""{"a":1}""", change.Before!.ToJsonString());
        Assert.Equal("""{"a":2}""", change.After!.ToJsonString());
    }

    [Fact]
    public void Masked_and_binary_fields_never_show_their_value_but_still_count_as_changed()
    {
        using var context = new WidgetContext();
        var widget = Saved(context);
        widget.Secret = "s2";
        widget.Hash = [9, 9];

        var changes = Assert.Single(AuditChangeSet.Collect(context, false)).Changes;

        Assert.Equal(2, changes.Count);
        Assert.All(changes, change =>
        {
            Assert.Equal(AuditRules.MaskedValue, change.Before!.GetValue<string>());
            Assert.Equal(AuditRules.MaskedValue, change.After!.GetValue<string>());
        });
    }

    [Fact]
    public void An_unchanged_masked_field_is_not_listed()
    {
        using var context = new WidgetContext();
        var widget = Saved(context);
        widget.Name = "CPU";

        Assert.Equal(["Name"], Assert.Single(AuditChangeSet.Collect(context, false)).Changes.Select(change => change.Field));
    }

    [Fact]
    public void A_deleted_row_logs_its_values_as_before_only()
    {
        using var context = new WidgetContext();
        var widget = Saved(context);
        context.Remove(widget);

        var deleted = Assert.Single(AuditChangeSet.Collect(context, false));

        Assert.Equal(AuditAction.Deleted, deleted.Action);
        Assert.Equal(widget.Id.ToString(), deleted.EntityId);
        Assert.All(deleted.Changes, change => Assert.Null(change.After));
        Assert.Equal("\"GPU\"", deleted.Changes.Single(change => change.Field == "Name").Before!.ToJsonString());
    }

    [Fact]
    public void Moving_to_the_trash_and_back_are_their_own_actions_without_a_field_list()
    {
        using var context = new WidgetContext();
        var draft = new Draft { Title = "A" };
        context.Attach(draft);

        TrashRules.MoveToTrash(draft, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var trashed = Assert.Single(AuditChangeSet.Collect(context, false));
        Assert.Equal(AuditAction.SoftDeleted, trashed.Action);
        Assert.Empty(trashed.Changes);

        context.ChangeTracker.Clear();
        draft.DeletedAtUtc = DateTimeOffset.UtcNow;
        context.Attach(draft);
        TrashRules.Restore(draft);
        Assert.Equal(AuditAction.Restored, Assert.Single(AuditChangeSet.Collect(context, false)).Action);
    }

    [Fact]
    public void An_edit_inside_a_restore_scope_is_logged_as_restored_values()
    {
        using var context = new WidgetContext();
        Saved(context).Price = 90m;

        Assert.Equal(AuditAction.ValuesRestored, Assert.Single(AuditChangeSet.Collect(context, true)).Action);
    }

    [Fact]
    public void An_excluded_kind_is_never_logged()
    {
        using var context = new WidgetContext();
        context.Add(new Counter { Value = 1 });

        Assert.Empty(AuditChangeSet.Collect(context, false));
    }

    [Fact]
    public void A_composite_key_is_joined_with_a_bar()
    {
        using var context = new WidgetContext();
        var orderId = Guid.NewGuid();
        context.Add(new Line { OrderId = orderId, Number = 3, Text = "x" });

        Assert.Equal($"{orderId}|3", Assert.Single(AuditChangeSet.Collect(context, false)).EntityId);
    }

    [Fact]
    public void The_ids_a_change_list_points_to_are_found_once_alone_or_in_a_list_on_either_side()
    {
        var brand = Guid.NewGuid();
        var category = Guid.NewGuid();
        var other = Guid.NewGuid();
        var changes = new[]
        {
            new AuditFieldChange("BrandId", JsonValue.Create(brand.ToString()), JsonValue.Create(category.ToString())),
            new AuditFieldChange("Related", null, new JsonArray(brand.ToString(), other.ToString(), "not an id")),
            new AuditFieldChange("Name", JsonValue.Create("Intel"), JsonValue.Create(12))
        };

        Assert.Equal([brand, category, other], AuditChanges.LinkedIds(changes).Order());
    }

    [Fact]
    public void Excluding_a_kind_that_is_not_in_the_model_fails_loudly()
    {
        var builder = new ModelBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.ExcludeFromAudit(typeof(Counter)));
    }
}

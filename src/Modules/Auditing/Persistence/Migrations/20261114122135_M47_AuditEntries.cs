using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Auditing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M47_AuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    actor_user_agent = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: false),
                    restored_from_entry_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.id);
                    table.CheckConstraint("ck_audit_entries_action", "action IN ('Created', 'Updated', 'Deleted', 'SoftDeleted', 'Restored', 'ValuesRestored')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_actor_user_id",
                table: "audit_entries",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_entity",
                table: "audit_entries",
                columns: new[] { "entity", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_module",
                table: "audit_entries",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_occurred_at_utc",
                table: "audit_entries",
                column: "occurred_at_utc",
                descending: new bool[0]);

            // Append-only in the database itself: no role (not even the schema owner) can change or empty the log.
            migrationBuilder.Sql("""
                CREATE FUNCTION audit_entries_append_only() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit_entries is append-only (% not allowed)', TG_OP;
                END;
                $$;

                CREATE TRIGGER trg_audit_entries_no_change BEFORE UPDATE OR DELETE ON audit_entries
                    FOR EACH ROW EXECUTE FUNCTION audit_entries_append_only();
                CREATE TRIGGER trg_audit_entries_no_truncate BEFORE TRUNCATE ON audit_entries
                    FOR EACH STATEMENT EXECUTE FUNCTION audit_entries_append_only();

                REVOKE UPDATE, DELETE, TRUNCATE ON TABLE public.audit_entries FROM PUBLIC, hardware_store_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.Sql("DROP FUNCTION audit_entries_append_only();");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Auditing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M3_2a_SecurityAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "security_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actor_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_key_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    safe_metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_audit_events", x => x.id);
                    table.CheckConstraint("ck_security_audit_actor_kind", "actor_kind IN ('Anonymous', 'User', 'System')");
                    table.CheckConstraint("ck_security_audit_event_type", "length(event_type) BETWEEN 1 AND 128");
                    table.CheckConstraint("ck_security_audit_outcome", "outcome IN ('Succeeded', 'Failed', 'Rejected', 'Recorded')");
                    table.CheckConstraint("ck_security_audit_session_hash_length", "session_key_hash IS NULL OR octet_length(session_key_hash) = 32");
                    table.ForeignKey(
                        name: "FK_security_audit_events_user_accounts_actor_user_account_id",
                        column: x => x.actor_user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_security_audit_events_user_accounts_subject_user_account_id",
                        column: x => x.subject_user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_events_actor_user_account_id",
                table: "security_audit_events",
                column: "actor_user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_events_occurred_at_utc",
                table: "security_audit_events",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_events_subject_user_account_id",
                table: "security_audit_events",
                column: "subject_user_account_id");

            migrationBuilder.Sql("""
                ALTER TABLE public.security_audit_events OWNER TO hardware_store_audit_owner;
                REVOKE ALL ON TABLE public.security_audit_events FROM PUBLIC, hardware_store_app;
                GRANT SELECT, INSERT ON TABLE public.security_audit_events TO hardware_store_audit_writer;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "security_audit_events");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Auditing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M46_BackupRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_security_audit_events_user_accounts_actor_user_account_id",
                table: "security_audit_events");

            migrationBuilder.DropForeignKey(
                name: "FK_security_audit_events_user_accounts_subject_user_account_id",
                table: "security_audit_events");

            migrationBuilder.CreateTable(
                name: "backup_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    file_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    destinations_written = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verify_passed = table.Column<bool>(type: "boolean", nullable: true),
                    verify_tables = table.Column<int>(type: "integer", nullable: true),
                    verify_rows = table.Column<long>(type: "bigint", nullable: true),
                    verify_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_runs", x => x.id);
                    table.CheckConstraint("ck_backup_runs_status", "status IN ('Succeeded', 'Failed')");
                    table.CheckConstraint("ck_backup_runs_trigger", "trigger_kind IN ('Scheduled', 'Manual', 'BeforeReset')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_backup_runs_started_at_utc",
                table: "backup_runs",
                column: "started_at_utc");

            migrationBuilder.AddForeignKey(
                name: "FK_security_audit_events_user_accounts_actor_user_account_id",
                table: "security_audit_events",
                column: "actor_user_account_id",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_security_audit_events_user_accounts_subject_user_account_id",
                table: "security_audit_events",
                column: "subject_user_account_id",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_security_audit_events_user_accounts_actor_user_account_id",
                table: "security_audit_events");

            migrationBuilder.DropForeignKey(
                name: "FK_security_audit_events_user_accounts_subject_user_account_id",
                table: "security_audit_events");

            migrationBuilder.DropTable(
                name: "backup_runs");

            migrationBuilder.AddForeignKey(
                name: "FK_security_audit_events_user_accounts_actor_user_account_id",
                table: "security_audit_events",
                column: "actor_user_account_id",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_security_audit_events_user_accounts_subject_user_account_id",
                table: "security_audit_events",
                column: "subject_user_account_id",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

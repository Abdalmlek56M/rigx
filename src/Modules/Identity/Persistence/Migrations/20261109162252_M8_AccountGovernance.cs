using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M8_AccountGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_restrictions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reinstatement_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_restrictions", x => x.id);
                    table.CheckConstraint("ck_account_restrictions_type", "type IN ('Suspension','Ban')");
                    table.CheckConstraint("ck_account_restrictions_window", "expires_at_utc IS NULL OR expires_at_utc > effective_at_utc");
                    table.ForeignKey(
                        name: "FK_account_restrictions_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_email_change_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    normalized_new_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_email_change_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_identity_email_change_requests_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    body_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    link_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_notifications_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_restrictions_account",
                table: "account_restrictions",
                columns: new[] { "user_account_id", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_email_change_requests_account",
                table: "identity_email_change_requests",
                columns: new[] { "user_account_id", "consumed_at_utc", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_email_change_requests_token",
                table: "identity_email_change_requests",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_recipient",
                table: "user_notifications",
                columns: new[] { "user_account_id", "is_read", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_restrictions");

            migrationBuilder.DropTable(
                name: "identity_email_change_requests");

            migrationBuilder.DropTable(
                name: "user_notifications");
        }
    }
}

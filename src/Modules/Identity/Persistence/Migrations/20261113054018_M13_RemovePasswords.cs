using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M13_RemovePasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_tokens");

            migrationBuilder.DropTable(
                name: "user_account_login_identifiers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_accounts_access_failed_count",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "access_failed_count",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "access_failed_window_started_utc",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "lockout_end_utc",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "user_accounts");

            // Password emails (verification links, resets, password notices) are never sent now.
            migrationBuilder.Sql("""
                UPDATE identity_email_outbox
                SET status = 'DeadLetter', protected_payload = ''::bytea, last_error_code = 'KindRetired'
                WHERE kind IN ('EmailVerification', 'PasswordReset', 'SecurityNotification')
                  AND status IN ('Pending', 'Failed', 'Processing');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "access_failed_count",
                table: "user_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "access_failed_window_started_utc",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end_utc",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "user_accounts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "identity_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    protected_code = table.Column<byte[]>(type: "bytea", nullable: true),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_tokens", x => x.id);
                    table.CheckConstraint("ck_identity_tokens_expiry_order", "expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_identity_tokens_hash_length", "octet_length(token_hash) = 32");
                    table.CheckConstraint("ck_identity_tokens_not_used_and_revoked", "used_at_utc IS NULL OR revoked_at_utc IS NULL");
                    table.CheckConstraint("ck_identity_tokens_purpose", "purpose IN ('EmailVerification', 'PasswordReset', 'EmailLoginCode', 'PasswordResetCode')");
                    table.ForeignKey(
                        name: "FK_identity_tokens_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_account_login_identifiers",
                columns: table => new
                {
                    normalized_identifier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_account_login_identifiers", x => x.normalized_identifier);
                    table.CheckConstraint("ck_user_account_login_identifier_length", "length(normalized_identifier) BETWEEN 1 AND 255");
                    table.ForeignKey(
                        name: "FK_user_account_login_identifiers_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_accounts_access_failed_count",
                table: "user_accounts",
                sql: "access_failed_count >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_identity_tokens_account_purpose_created",
                table: "identity_tokens",
                columns: new[] { "user_account_id", "purpose", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_identity_tokens_active_account_purpose",
                table: "identity_tokens",
                columns: new[] { "user_account_id", "purpose" },
                unique: true,
                filter: "used_at_utc IS NULL AND revoked_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_identity_tokens_token_hash",
                table: "identity_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_account_login_identifiers_user_account_id",
                table: "user_account_login_identifiers",
                column: "user_account_id");
        }
    }
}

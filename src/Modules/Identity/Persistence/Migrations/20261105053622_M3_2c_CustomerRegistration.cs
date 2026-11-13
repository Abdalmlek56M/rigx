using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M3_2c_CustomerRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $m3_2c_preflight$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM user_accounts
                        WHERE is_email_verified = TRUE AND password_hash IS NULL
                    ) THEN
                        RAISE EXCEPTION 'M3.2c preflight failed: a verified account has no password hash.';
                    END IF;

                    IF EXISTS (
                        SELECT normalized_identifier
                        FROM (
                            SELECT normalized_username AS normalized_identifier, id AS user_account_id FROM user_accounts
                            UNION ALL
                            SELECT normalized_email AS normalized_identifier, id AS user_account_id FROM user_accounts
                        ) AS identifiers
                        GROUP BY normalized_identifier
                        HAVING count(DISTINCT user_account_id) > 1
                    ) THEN
                        RAISE EXCEPTION 'M3.2c preflight failed: existing login identifiers collide across accounts; review records without automatic changes.';
                    END IF;
                END
                $m3_2c_preflight$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "user_accounts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.CreateTable(
                name: "identity_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_tokens", x => x.id);
                    table.CheckConstraint("ck_identity_tokens_expiry_order", "expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_identity_tokens_hash_length", "octet_length(token_hash) = 32");
                    table.CheckConstraint("ck_identity_tokens_not_used_and_revoked", "used_at_utc IS NULL OR revoked_at_utc IS NULL");
                    table.CheckConstraint("ck_identity_tokens_purpose", "purpose IN ('EmailVerification', 'PasswordReset')");
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

            migrationBuilder.Sql("""
                INSERT INTO user_account_login_identifiers (normalized_identifier, user_account_id)
                SELECT normalized_identifier, user_account_id
                FROM (
                    SELECT normalized_username AS normalized_identifier, id AS user_account_id FROM user_accounts
                    UNION
                    SELECT normalized_email AS normalized_identifier, id AS user_account_id FROM user_accounts
                ) AS existing_identifiers;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_accounts_verified_requires_password_hash",
                table: "user_accounts",
                sql: "NOT is_email_verified OR COALESCE(password_hash IS NOT NULL, FALSE)");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $m3_2c_rollback_check$
                BEGIN
                    IF EXISTS (SELECT 1 FROM identity_tokens) THEN
                        RAISE EXCEPTION 'M3.2c rollback refused: identity verification tokens exist and require explicit handling.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM user_accounts WHERE password_hash IS NULL) THEN
                        RAISE EXCEPTION 'M3.2c rollback refused: pending accounts without a password hash require an explicit recovery plan.';
                    END IF;
                END
                $m3_2c_rollback_check$;
                """);

            migrationBuilder.DropTable(
                name: "identity_tokens");

            migrationBuilder.DropTable(
                name: "user_account_login_identifiers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_accounts_verified_requires_password_hash",
                table: "user_accounts");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "user_accounts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}

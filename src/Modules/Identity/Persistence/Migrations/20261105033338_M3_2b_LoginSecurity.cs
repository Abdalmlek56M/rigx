using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M3_2b_LoginSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "normalized_email",
                table: "user_accounts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_username",
                table: "user_accounts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql("""
                DO $m3_2b$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM (
                            SELECT upper(normalize(btrim(username), NFC)) AS normalized_value, 'username' AS value_kind
                            FROM user_accounts
                            UNION ALL
                            SELECT upper(normalize(btrim(email), NFC)) AS normalized_value, 'email' AS value_kind
                            FROM user_accounts
                        ) AS values_to_check
                        GROUP BY normalized_value, value_kind
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'M3.2b normalization collision in username or email; resolve accounts before migration.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM user_accounts AS username_owner
                        JOIN user_accounts AS email_owner ON username_owner.id <> email_owner.id
                        WHERE upper(normalize(btrim(username_owner.username), NFC)) = upper(normalize(btrim(email_owner.email), NFC))
                           OR upper(normalize(btrim(username_owner.email), NFC)) = upper(normalize(btrim(email_owner.username), NFC))
                    ) THEN
                        RAISE EXCEPTION 'M3.2b cross-field login identifier collision; resolve accounts before migration.';
                    END IF;
                END
                $m3_2b$;

                UPDATE user_accounts
                SET normalized_username = upper(normalize(btrim(username), NFC)),
                    normalized_email = upper(normalize(btrim(email), NFC));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                table: "user_accounts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_username",
                table: "user_accounts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_accounts_normalized_email",
                table: "user_accounts",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_accounts_normalized_username",
                table: "user_accounts",
                column: "normalized_username",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_accounts_access_failed_count",
                table: "user_accounts",
                sql: "access_failed_count >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_user_accounts_normalized_email",
                table: "user_accounts");

            migrationBuilder.DropIndex(
                name: "ux_user_accounts_normalized_username",
                table: "user_accounts");

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
                name: "normalized_email",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "normalized_username",
                table: "user_accounts");
        }
    }
}

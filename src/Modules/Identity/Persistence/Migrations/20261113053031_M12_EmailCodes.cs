using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M12_EmailCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_user_accounts_verified_requires_password_hash",
                table: "user_accounts");

            migrationBuilder.CreateTable(
                name: "email_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    purpose = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    protected_code = table.Column<byte[]>(type: "bytea", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_codes", x => x.id);
                    table.CheckConstraint("ck_email_codes_attempts", "attempts >= 0");
                    table.CheckConstraint("ck_email_codes_expiry_order", "expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_email_codes_purpose", "purpose IN ('SignIn', 'StepUp')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_codes_email_purpose_created",
                table: "email_codes",
                columns: new[] { "normalized_email", "purpose", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_email_codes_active_email_purpose",
                table: "email_codes",
                columns: new[] { "normalized_email", "purpose" },
                unique: true,
                filter: "used_at_utc IS NULL AND revoked_at_utc IS NULL");

            // Codes of the retired one-time-code flow and their queued emails are no longer valid.
            migrationBuilder.Sql("""
                UPDATE identity_email_outbox
                SET status = 'DeadLetter', protected_payload = ''::bytea, last_error_code = 'KindRetired'
                WHERE kind IN ('EmailLoginCode', 'PasswordResetCode') AND status IN ('Pending', 'Failed', 'Processing');
                UPDATE identity_tokens SET revoked_at_utc = now()
                WHERE purpose IN ('EmailLoginCode', 'PasswordResetCode') AND used_at_utc IS NULL AND revoked_at_utc IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_codes");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_accounts_verified_requires_password_hash",
                table: "user_accounts",
                sql: "NOT is_email_verified OR COALESCE(password_hash IS NOT NULL, FALSE)");
        }
    }
}

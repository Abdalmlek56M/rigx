using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M11_EmailOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_identity_tokens_purpose",
                table: "identity_tokens");

            migrationBuilder.AddColumn<int>(
                name: "attempts",
                table: "identity_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "protected_code",
                table: "identity_tokens",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_identity_tokens_purpose",
                table: "identity_tokens",
                sql: "purpose IN ('EmailVerification', 'PasswordReset', 'EmailLoginCode', 'PasswordResetCode')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_identity_tokens_purpose",
                table: "identity_tokens");

            migrationBuilder.DropColumn(
                name: "attempts",
                table: "identity_tokens");

            migrationBuilder.DropColumn(
                name: "protected_code",
                table: "identity_tokens");

            migrationBuilder.AddCheckConstraint(
                name: "ck_identity_tokens_purpose",
                table: "identity_tokens",
                sql: "purpose IN ('EmailVerification', 'PasswordReset')");
        }
    }
}

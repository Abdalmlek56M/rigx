using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M3_2a_AuthenticationSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "authorization_version",
                table: "user_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "auth_sessions",
                columns: table => new
                {
                    session_key_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_ticket = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_activity_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idle_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    absolute_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idle_timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    authorization_version = table.Column<int>(type: "integer", nullable: false),
                    rbac_revision = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_sessions", x => x.session_key_hash);
                    table.CheckConstraint("ck_auth_sessions_expiry_order", "idle_expires_at_utc <= absolute_expires_at_utc");
                    table.CheckConstraint("ck_auth_sessions_idle_timeout", "idle_timeout_seconds > 0");
                    table.CheckConstraint("ck_auth_sessions_key_hash_length", "octet_length(session_key_hash) = 32");
                    table.ForeignKey(
                        name: "FK_auth_sessions_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "authorization_state",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rbac_revision = table.Column<long>(type: "bigint", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_state", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "authorization_state",
                columns: new[] { "id", "rbac_revision", "updated_at_utc" },
                values: new object[] { (short)1, 0L, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "ix_auth_sessions_user_account_id",
                table: "auth_sessions",
                column: "user_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_sessions");

            migrationBuilder.DropTable(
                name: "authorization_state");

            migrationBuilder.DropColumn(
                name: "authorization_version",
                table: "user_accounts");
        }
    }
}

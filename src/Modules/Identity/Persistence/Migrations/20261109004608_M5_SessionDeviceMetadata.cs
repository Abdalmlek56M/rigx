using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M5_SessionDeviceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "auth_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                table: "auth_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE auth_sessions SET public_id = gen_random_uuid() WHERE public_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                table: "auth_sessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "auth_sessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_auth_sessions_public_id",
                table: "auth_sessions",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_auth_sessions_public_id",
                table: "auth_sessions");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "auth_sessions");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "auth_sessions");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "auth_sessions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.PcBuilder.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M61_BuildVisibilityAndReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "saved_builds",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Link");

            migrationBuilder.CreateTable(
                name: "saved_build_moderations",
                columns: table => new
                {
                    saved_build_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_build_moderations", x => x.saved_build_id);
                    table.ForeignKey(
                        name: "FK_saved_build_moderations_saved_builds_saved_build_id",
                        column: x => x.saved_build_id,
                        principalTable: "saved_builds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_build_reactions",
                columns: table => new
                {
                    saved_build_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_build_reactions", x => new { x.saved_build_id, x.user_account_id, x.kind });
                    table.ForeignKey(
                        name: "FK_saved_build_reactions_saved_builds_saved_build_id",
                        column: x => x.saved_build_id,
                        principalTable: "saved_builds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saved_builds_visibility",
                table: "saved_builds",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "ix_saved_build_moderations_status",
                table: "saved_build_moderations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_saved_build_reactions_user_kind",
                table: "saved_build_reactions",
                columns: new[] { "user_account_id", "kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_build_moderations");

            migrationBuilder.DropTable(
                name: "saved_build_reactions");

            migrationBuilder.DropIndex(
                name: "ix_saved_builds_visibility",
                table: "saved_builds");

            migrationBuilder.DropColumn(
                name: "visibility",
                table: "saved_builds");
        }
    }
}

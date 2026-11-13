using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.PcBuilder.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M32_SavedBuilds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pc_build_slot_options");

            migrationBuilder.DropTable(
                name: "pc_build_slots");

            migrationBuilder.DropTable(
                name: "pc_build_templates");

            migrationBuilder.CreateTable(
                name: "saved_builds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    usage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    parts = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_builds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saved_builds_user_account_id",
                table: "saved_builds",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_saved_builds_token",
                table: "saved_builds",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_builds");

            migrationBuilder.CreateTable(
                name: "pc_build_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    summary_ar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    summary_en = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_build_templates", x => x.id);
                    table.CheckConstraint("ck_pc_build_templates_status", "status IN ('Draft','Published')");
                });

            migrationBuilder.CreateTable(
                name: "pc_build_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    label_ar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    label_en = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slot_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_build_slots", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_build_slots_pc_build_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "pc_build_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pc_build_slot_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    primary_image_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    product_slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    specifications_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_build_slot_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_build_slot_options_pc_build_slots_slot_id",
                        column: x => x.slot_id,
                        principalTable: "pc_build_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_pc_build_slot_options_product",
                table: "pc_build_slot_options",
                columns: new[] { "slot_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_pc_build_slots_template_key",
                table: "pc_build_slots",
                columns: new[] { "template_id", "slot_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pc_build_templates_status",
                table: "pc_build_templates",
                column: "status");
        }
    }
}

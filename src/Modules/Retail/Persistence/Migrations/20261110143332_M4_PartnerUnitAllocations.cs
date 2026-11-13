using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M4_PartnerUnitAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_partner_earnings_item_unit",
                table: "partner_earnings");

            migrationBuilder.CreateTable(
                name: "partner_unit_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    loss_sharing_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_unit_allocations", x => x.id);
                    table.CheckConstraint("ck_partner_unit_allocations_percent", "percent > 0 AND percent <= 100");
                });

            migrationBuilder.CreateIndex(
                name: "ux_partner_earnings_item_unit_partner",
                table: "partner_earnings",
                columns: new[] { "order_item_id", "unit_id", "partner_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_partner_unit_allocations_scope",
                table: "partner_unit_allocations",
                columns: new[] { "partner_name", "unit_id", "product_id", "category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partner_unit_allocations");

            migrationBuilder.DropIndex(
                name: "ux_partner_earnings_item_unit_partner",
                table: "partner_earnings");

            migrationBuilder.CreateIndex(
                name: "ux_partner_earnings_item_unit",
                table: "partner_earnings",
                columns: new[] { "order_item_id", "unit_id" },
                unique: true);
        }
    }
}

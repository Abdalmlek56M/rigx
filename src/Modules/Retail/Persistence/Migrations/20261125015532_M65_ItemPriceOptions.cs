using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M65_ItemPriceOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "offers_alone",
                table: "stock_items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "offers_in_build",
                table: "stock_items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "offers_with_board",
                table: "stock_items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_items_price_options",
                table: "stock_items",
                sql: "(offers_alone IS NULL AND offers_with_board IS NULL AND offers_in_build IS NULL) OR (offers_alone IS NOT NULL AND offers_with_board IS NOT NULL AND offers_in_build IS NOT NULL AND (offers_alone OR offers_with_board OR offers_in_build))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_items_price_options",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "offers_alone",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "offers_in_build",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "offers_with_board",
                table: "stock_items");
        }
    }
}

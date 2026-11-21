using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class M60_CategoryPriceOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "price_alone",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "price_in_build",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "price_with_board",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_categories_price_option",
                table: "categories",
                sql: "price_alone OR price_with_board OR price_in_build");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_categories_price_option",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "price_alone",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "price_in_build",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "price_with_board",
                table: "categories");
        }
    }
}

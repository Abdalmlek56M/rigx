using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class M18_CategorySpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "categories",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_in_builder",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_in_menu",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "spec_definitions",
                table: "categories",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateIndex(
                name: "ux_categories_code",
                table: "categories",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_categories_code",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "code",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "show_in_builder",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "show_in_menu",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "spec_definitions",
                table: "categories");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M5_ProductCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "condition",
                table: "products",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "New");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_condition",
                table: "products",
                sql: "condition IN ('New', 'Used')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_condition",
                table: "products");

            migrationBuilder.DropColumn(
                name: "condition",
                table: "products");
        }
    }
}

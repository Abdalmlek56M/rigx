using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Inventory.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M6_WarehouseOperationalMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                table: "warehouses",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manager_name",
                table: "warehouses",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "warehouses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contact_phone",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "manager_name",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "warehouses");
        }
    }
}

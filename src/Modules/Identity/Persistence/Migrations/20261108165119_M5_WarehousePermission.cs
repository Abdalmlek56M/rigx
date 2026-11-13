using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M5_WarehousePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                value: "Inventory.ManageWarehouses");

            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT id, 'Inventory.ManageWarehouses' FROM roles WHERE code = 'Owner'
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions
                WHERE permission_code = 'Inventory.ManageWarehouses';
                """);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Inventory.ManageWarehouses");
        }
    }
}

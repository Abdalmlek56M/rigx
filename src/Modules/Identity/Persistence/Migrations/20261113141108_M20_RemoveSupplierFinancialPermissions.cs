using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M20_RemoveSupplierFinancialPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions
                WHERE permission_code IN ('Purchasing.ViewSupplierFinancials', 'Purchasing.ManageSupplierFinancials');
                """);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Purchasing.ManageSupplierFinancials");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Purchasing.ViewSupplierFinancials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                values: new object[]
                {
                    "Purchasing.ManageSupplierFinancials",
                    "Purchasing.ViewSupplierFinancials"
                });

            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT id, permission FROM roles
                CROSS JOIN (VALUES ('Purchasing.ViewSupplierFinancials'), ('Purchasing.ManageSupplierFinancials')) AS granted(permission)
                WHERE code = 'Owner'
                ON CONFLICT DO NOTHING;
                """);
        }
    }
}

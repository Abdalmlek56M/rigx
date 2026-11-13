using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M3_SupplierFinancialPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                SELECT id, 'Purchasing.ViewSupplierFinancials' FROM roles WHERE code = 'Owner'
                ON CONFLICT DO NOTHING;
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT id, 'Purchasing.ManageSupplierFinancials' FROM roles WHERE code = 'Owner'
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions
                WHERE permission_code IN (
                    'Purchasing.ViewSupplierFinancials',
                    'Purchasing.ManageSupplierFinancials'
                );
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
    }
}

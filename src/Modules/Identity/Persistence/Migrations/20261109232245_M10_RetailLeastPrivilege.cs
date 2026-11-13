using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M10_RetailLeastPrivilege : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                values: new object[]
                {
                    "Finance.ManagePartnerAgreements",
                    "Finance.PostPartnerSettlements",
                    "Inventory.ViewCosts",
                    "Inventory.ViewSerials",
                    "Sales.ViewCustomerContact"
                });

            // Preserve the Owner's operational ability; every other role must be granted explicitly.
            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT r.id, p.code
                FROM roles r
                CROSS JOIN (VALUES
                    ('Finance.ManagePartnerAgreements'),
                    ('Finance.PostPartnerSettlements'),
                    ('Inventory.ViewCosts'),
                    ('Inventory.ViewSerials'),
                    ('Sales.ViewCustomerContact')
                ) AS p(code)
                WHERE r.code = 'Owner'
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Finance.ManagePartnerAgreements");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Finance.PostPartnerSettlements");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Inventory.ViewCosts");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Inventory.ViewSerials");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Sales.ViewCustomerContact");
        }
    }
}

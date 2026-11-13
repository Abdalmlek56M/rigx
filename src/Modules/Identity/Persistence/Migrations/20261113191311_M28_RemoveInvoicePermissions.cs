using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M28_RemoveInvoicePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions
                WHERE permission_code IN ('Invoices.IssueFinal', 'Invoices.ManageTemplates', 'Invoices.Reprint', 'Invoices.ReviewDraft');
                """);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Invoices.IssueFinal");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Invoices.ManageTemplates");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Invoices.Reprint");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Invoices.ReviewDraft");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                values: new object[]
                {
                    "Invoices.IssueFinal",
                    "Invoices.ManageTemplates",
                    "Invoices.Reprint",
                    "Invoices.ReviewDraft"
                });

            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT id, permission FROM roles
                CROSS JOIN (VALUES ('Invoices.IssueFinal'), ('Invoices.ManageTemplates'), ('Invoices.Reprint'), ('Invoices.ReviewDraft')) AS granted(permission)
                WHERE code = 'Owner'
                ON CONFLICT DO NOTHING;
                """);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M31_RequestPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                values: new object[]
                {
                    "Requests.Manage",
                    "Requests.View"
                });

            // Whoever already runs the store (settings) or creates purchases gets the inbox, so no one loses access to messages.
            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT DISTINCT rp.role_id, requested.code
                FROM role_permissions rp
                CROSS JOIN (VALUES ('Requests.View'), ('Requests.Manage')) AS requested(code)
                WHERE rp.permission_code IN ('System.StoreSettings', 'Purchasing.CreateOrder')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM role_permissions WHERE permission_code IN ('Requests.View', 'Requests.Manage');");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Requests.Manage");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Requests.View");
        }
    }
}

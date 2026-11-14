using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M45_TrashPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                values: new object[]
                {
                    "System.Purge",
                    "System.Trash"
                });

            // The owner role gets both; System.Purge goes to no one else. Whoever already runs the store settings can use the trash.
            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT r.id, requested.code
                FROM roles r
                CROSS JOIN (VALUES ('System.Trash'), ('System.Purge')) AS requested(code)
                WHERE r.code = 'Owner'
                ON CONFLICT DO NOTHING;

                INSERT INTO role_permissions (role_id, permission_code)
                SELECT DISTINCT rp.role_id, 'System.Trash'
                FROM role_permissions rp
                WHERE rp.permission_code = 'System.StoreSettings'
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM role_permissions WHERE permission_code IN ('System.Trash', 'System.Purge');");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "System.Purge");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "System.Trash");
        }
    }
}

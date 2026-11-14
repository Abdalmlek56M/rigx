using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M46_BackupPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                values: new object[]
                {
                    "System.BackupDownload",
                    "System.ResetData"
                });

            // The owner role gets both new permissions (and the existing System.ManageBackups); they go to no one else.
            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT r.id, requested.code
                FROM roles r
                CROSS JOIN (VALUES ('System.ManageBackups'), ('System.BackupDownload'), ('System.ResetData')) AS requested(code)
                WHERE r.code = 'Owner'
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM role_permissions WHERE permission_code IN ('System.BackupDownload', 'System.ResetData');");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "System.BackupDownload");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "System.ResetData");
        }
    }
}

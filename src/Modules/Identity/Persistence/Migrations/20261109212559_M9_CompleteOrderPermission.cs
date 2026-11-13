using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M9_CompleteOrderPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                value: "Sales.CompleteOrder");

            // Grant completion to every role that can already approve orders, so the
            // new server-side completion gate does not lock out existing administrators.
            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT role_id, 'Sales.CompleteOrder'
                FROM role_permissions
                WHERE permission_code = 'Sales.ApproveOrder'
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Sales.CompleteOrder");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M37_CustomerAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "merged_into_account_id",
                table: "user_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_note",
                table: "persons",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                values: new object[]
                {
                    "Customers.Manage",
                    "Customers.View"
                });

            // Whoever manages users (owner and admin roles) gets the customer pages, so no one loses access to customer data.
            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_code)
                SELECT DISTINCT rp.role_id, requested.code
                FROM role_permissions rp
                CROSS JOIN (VALUES ('Customers.View'), ('Customers.Manage')) AS requested(code)
                WHERE rp.permission_code = 'System.ManageUsers'
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM role_permissions WHERE permission_code IN ('Customers.View', 'Customers.Manage');");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Customers.Manage");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "code",
                keyValue: "Customers.View");

            migrationBuilder.DropColumn(
                name: "merged_into_account_id",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "customer_note",
                table: "persons");
        }
    }
}

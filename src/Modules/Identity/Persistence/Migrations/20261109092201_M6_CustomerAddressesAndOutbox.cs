using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M6_CustomerAddressesAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    country_calling_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    governorate = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    city = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    district = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    street = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    building = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    floor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    apartment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    landmark = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_addresses_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_account",
                table: "customer_addresses",
                columns: new[] { "user_account_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ux_customer_addresses_one_default",
                table: "customer_addresses",
                column: "user_account_id",
                unique: true,
                filter: "is_default AND NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_addresses");
        }
    }
}

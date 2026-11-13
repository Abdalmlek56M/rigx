using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M26_WarrantyRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "warranty_label",
                table: "order_item_components",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "warranty_months",
                table: "order_item_components",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "warranties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    condition = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    build_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    months = table.Column<int>(type: "integer", nullable: true),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    voided_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warranties", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_warranties_code",
                table: "warranties",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_warranties_order",
                table: "warranties",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_warranties_phone",
                table: "warranties",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_warranties_serial",
                table: "warranties",
                column: "serial_number");

            migrationBuilder.CreateIndex(
                name: "ix_warranties_user",
                table: "warranties",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_warranties_item_unit",
                table: "warranties",
                columns: new[] { "order_item_id", "unit_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "warranties");

            migrationBuilder.DropColumn(
                name: "warranty_label",
                table: "order_item_components");

            migrationBuilder.DropColumn(
                name: "warranty_months",
                table: "order_item_components");
        }
    }
}

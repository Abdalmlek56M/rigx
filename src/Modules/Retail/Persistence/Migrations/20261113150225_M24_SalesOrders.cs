using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M24_SalesOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "product_id",
                table: "order_items");

            migrationBuilder.RenameColumn(
                name: "sku",
                table: "order_items",
                newName: "code");

            migrationBuilder.AlterColumn<string>(
                name: "lead_source",
                table: "store_orders",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<string>(
                name: "area",
                table: "store_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "building",
                table: "store_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "channel",
                table: "store_orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "courier_name",
                table: "store_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_email",
                table: "store_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "governorate",
                table: "store_orders",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_blacklisted",
                table: "store_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "landmark",
                table: "store_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "paid_amount",
                table: "store_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "phone_has_whatsapp",
                table: "store_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "rated_at_utc",
                table: "store_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "reservation_auto_release",
                table: "store_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reservation_expired_at_utc",
                table: "store_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reserved_until_utc",
                table: "store_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "satisfaction_comment",
                table: "store_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "satisfaction_score",
                table: "store_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "street",
                table: "store_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tracking_token",
                table: "store_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "upfront_amount",
                table: "store_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "usage",
                table: "store_orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usage_note",
                table: "store_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "waybill_number",
                table: "store_orders",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "blacklist_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    address_key = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: true),
                    address_text = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blacklist_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_payments", x => x.id);
                    table.CheckConstraint("ck_order_payments_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_order_payments_store_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "store_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE store_orders SET
                    tracking_token = replace(gen_random_uuid()::text, '-', ''),
                    channel = CASE WHEN lead_source = 'Website' THEN 'Online' ELSE 'Store' END,
                    lead_source = NULL,
                    street = left(address_text, 200),
                    phone_number = regexp_replace(phone_number, '[^0-9]', '', 'g'),
                    status = CASE WHEN status = 'Ready' THEN 'Shipped' ELSE status END,
                    payment_method = CASE payment_method
                        WHEN 'MobileWalletTransfer' THEN 'Wallet'
                        WHEN 'ToBeArranged' THEN 'Cash'
                        WHEN 'CashOnHandover' THEN 'Cash'
                        ELSE payment_method END,
                    paid_amount = CASE WHEN payment_status = 'Paid' THEN total ELSE 0 END;
                """);

            migrationBuilder.DropColumn(
                name: "address_text",
                table: "store_orders");

            migrationBuilder.CreateIndex(
                name: "ix_store_orders_phone",
                table: "store_orders",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_store_orders_reserved_until",
                table: "store_orders",
                column: "reserved_until_utc",
                filter: "reserved_until_utc IS NOT NULL AND reservation_expired_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_store_orders_tracking_token",
                table: "store_orders",
                column: "tracking_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_blacklist_entries_address",
                table: "blacklist_entries",
                column: "address_key");

            migrationBuilder.CreateIndex(
                name: "ix_blacklist_entries_email",
                table: "blacklist_entries",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_blacklist_entries_phone",
                table: "blacklist_entries",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "IX_order_payments_order_id",
                table: "order_payments",
                column: "order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blacklist_entries");

            migrationBuilder.DropTable(
                name: "order_payments");

            migrationBuilder.DropIndex(
                name: "ix_store_orders_phone",
                table: "store_orders");

            migrationBuilder.DropIndex(
                name: "ix_store_orders_reserved_until",
                table: "store_orders");

            migrationBuilder.DropIndex(
                name: "ux_store_orders_tracking_token",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "area",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "building",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "channel",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "courier_name",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "customer_email",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "governorate",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "is_blacklisted",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "landmark",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "paid_amount",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "phone_has_whatsapp",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "rated_at_utc",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "reservation_auto_release",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "reservation_expired_at_utc",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "reserved_until_utc",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "satisfaction_comment",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "satisfaction_score",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "street",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "tracking_token",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "upfront_amount",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "usage",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "usage_note",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "waybill_number",
                table: "store_orders");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "order_items",
                newName: "sku");

            migrationBuilder.AlterColumn<string>(
                name: "lead_source",
                table: "store_orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_text",
                table: "store_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "order_items",
                type: "uuid",
                nullable: true);
        }
    }
}

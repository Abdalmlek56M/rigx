using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M1_RetailCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assemblies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    public_title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    public_title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sale_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assemblies", x => x.id);
                    table.CheckConstraint("ck_assemblies_price", "sale_price >= 0");
                });

            migrationBuilder.CreateTable(
                name: "inventory_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    brand = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    model = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    category = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    condition = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    warranty_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    acquisition_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    partner_percent_override = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    assembly_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_units", x => x.id);
                    table.CheckConstraint("ck_inventory_units_cost", "acquisition_cost >= 0");
                });

            migrationBuilder.CreateTable(
                name: "partner_agreements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    default_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_agreements", x => x.id);
                    table.CheckConstraint("ck_partner_agreements_percent", "default_percent BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "partner_earnings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    gross_profit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_earnings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "partner_settlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_settlements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "store_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    address_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fulfillment_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    lead_source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    delivery_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    internal_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "assembly_units",
                columns: table => new
                {
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assembly_units", x => new { x.AssemblyId, x.UnitId });
                    table.ForeignKey(
                        name: "FK_assembly_units_assemblies_AssemblyId",
                        column: x => x.AssemblyId,
                        principalTable: "assemblies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assembly_units_inventory_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "inventory_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_events_store_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "store_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assembly_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_cost_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_items_store_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "store_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_assembly_units_unit",
                table: "assembly_units",
                column: "UnitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_units_serial",
                table: "inventory_units",
                column: "serial_number");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_units_status_product",
                table: "inventory_units",
                columns: new[] { "status", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_events_order_id",
                table: "order_events",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ux_partner_earnings_item",
                table: "partner_earnings",
                column: "order_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_purchase_receipts_draft",
                table: "purchase_receipts",
                column: "purchase_draft_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_store_orders_status_created",
                table: "store_orders",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_store_orders_reference",
                table: "store_orders",
                column: "reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assembly_units");

            migrationBuilder.DropTable(
                name: "order_events");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "partner_agreements");

            migrationBuilder.DropTable(
                name: "partner_earnings");

            migrationBuilder.DropTable(
                name: "partner_settlements");

            migrationBuilder.DropTable(
                name: "purchase_receipts");

            migrationBuilder.DropTable(
                name: "assemblies");

            migrationBuilder.DropTable(
                name: "inventory_units");

            migrationBuilder.DropTable(
                name: "store_orders");
        }
    }
}

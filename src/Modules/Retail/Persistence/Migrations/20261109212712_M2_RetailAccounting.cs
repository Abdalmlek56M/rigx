using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M2_RetailAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_partner_earnings_item",
                table: "partner_earnings");

            migrationBuilder.AddColumn<bool>(
                name: "payment_required",
                table: "store_orders",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payment_waived_at_utc",
                table: "store_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_waived_by_user_account_id",
                table: "store_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_waiver_reason",
                table: "store_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                table: "store_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "offset_applied",
                table: "partner_settlements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "cost_amount",
                table: "partner_earnings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "direct_expense_amount",
                table: "partner_earnings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "partner_earnings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "loss_sharing_enabled",
                table: "partner_earnings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "offset_amount",
                table: "partner_earnings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "order_item_component_id",
                table: "partner_earnings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "revenue_amount",
                table: "partner_earnings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "reversal_reason",
                table: "partner_earnings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reversed_at_utc",
                table: "partner_earnings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "unit_id",
                table: "partner_earnings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "loss_sharing_enabled",
                table: "partner_agreements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "direct_expense_amount",
                table: "order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "direct_expense_note",
                table: "order_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "net_profit",
                table: "order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "inventory_units",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<string>(
                name: "inspection_condition",
                table: "inventory_units",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_name",
                table: "inventory_units",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "standalone_price",
                table: "inventory_units",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "allocation_percent",
                table: "assembly_units",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "order_item_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    revenue_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    direct_expense_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cost_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_profit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_components", x => x.id);
                    table.CheckConstraint("ck_order_item_components_allocation", "allocation_percent BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_order_item_components_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_returns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    inspection_condition = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    inspection_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    inspected_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inspected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_returns", x => x.id);
                    table.CheckConstraint("ck_order_returns_refund", "refund_amount >= 0");
                });

            migrationBuilder.CreateTable(
                name: "partner_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    partner_earning_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_return_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    applied_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_adjustments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_partner_earnings_item_unit",
                table: "partner_earnings",
                columns: new[] { "order_item_id", "unit_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_assembly_units_allocation",
                table: "assembly_units",
                sql: "allocation_percent BETWEEN 0 AND 100");

            migrationBuilder.CreateIndex(
                name: "ux_order_item_components_unit",
                table: "order_item_components",
                columns: new[] { "order_item_id", "unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_order_returns_item_unit",
                table: "order_returns",
                columns: new[] { "order_item_id", "unit_id" },
                unique: true);

            // Backfill legacy rows conservatively; new transactions snapshot per-unit components.
            migrationBuilder.Sql("""
                UPDATE partner_earnings AS pe
                SET revenue_amount = oi.line_total,
                    cost_amount = oi.unit_cost_snapshot,
                    gross_profit = oi.line_total - oi.unit_cost_snapshot
                FROM order_items AS oi
                WHERE oi.id = pe.order_item_id AND pe.revenue_amount = 0;

                UPDATE order_items
                SET net_profit = line_total - unit_cost_snapshot
                WHERE net_profit = 0;

                WITH ranked AS (
                    SELECT "AssemblyId" AS assembly_id, "UnitId" AS unit_id,
                           ROW_NUMBER() OVER (PARTITION BY "AssemblyId" ORDER BY "UnitId") AS rn,
                           COUNT(*) OVER (PARTITION BY "AssemblyId") AS cnt
                    FROM assembly_units
                )
                UPDATE assembly_units AS au
                SET allocation_percent = CASE
                    WHEN ranked.rn = ranked.cnt THEN 100 - ROUND(100.0 / ranked.cnt, 2) * (ranked.cnt - 1)
                    ELSE ROUND(100.0 / ranked.cnt, 2)
                END
                FROM ranked
                WHERE au."AssemblyId" = ranked.assembly_id AND au."UnitId" = ranked.unit_id
                  AND au.allocation_percent = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_components");

            migrationBuilder.DropTable(
                name: "order_returns");

            migrationBuilder.DropTable(
                name: "partner_adjustments");

            migrationBuilder.DropIndex(
                name: "ux_partner_earnings_item_unit",
                table: "partner_earnings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_assembly_units_allocation",
                table: "assembly_units");

            migrationBuilder.DropColumn(
                name: "payment_required",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "payment_waived_at_utc",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "payment_waived_by_user_account_id",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "payment_waiver_reason",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "offset_applied",
                table: "partner_settlements");

            migrationBuilder.DropColumn(
                name: "cost_amount",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "direct_expense_amount",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "loss_sharing_enabled",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "offset_amount",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "order_item_component_id",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "revenue_amount",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "reversal_reason",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "reversed_at_utc",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "unit_id",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "loss_sharing_enabled",
                table: "partner_agreements");

            migrationBuilder.DropColumn(
                name: "direct_expense_amount",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "direct_expense_note",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "net_profit",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "inspection_condition",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "source_name",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "standalone_price",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "allocation_percent",
                table: "assembly_units");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "inventory_units",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(24)",
                oldMaxLength: 24);

            migrationBuilder.CreateIndex(
                name: "ux_partner_earnings_item",
                table: "partner_earnings",
                column: "order_item_id",
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M53_Coupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "coupon_code",
                table: "store_orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "coupon_discount",
                table: "store_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "coupon_id",
                table: "store_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    discount_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    max_discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    min_order_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_uses = table.Column<int>(type: "integer", nullable: true),
                    once_per_customer = table.Column<bool>(type: "boolean", nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.id);
                    table.CheckConstraint("ck_coupons_kind", "discount_kind IN ('Percent','Amount')");
                    table.CheckConstraint("ck_coupons_value", "discount_value > 0 AND (discount_kind <> 'Percent' OR discount_value <= 90)");
                    table.CheckConstraint("ck_coupons_window", "ends_at_utc IS NULL OR starts_at_utc IS NULL OR ends_at_utc > starts_at_utc");
                });

            migrationBuilder.CreateIndex(
                name: "ix_store_orders_coupon",
                table: "store_orders",
                column: "coupon_id",
                filter: "coupon_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_coupons_code",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_store_orders_coupons_coupon_id",
                table: "store_orders",
                column: "coupon_id",
                principalTable: "coupons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_store_orders_coupons_coupon_id",
                table: "store_orders");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropIndex(
                name: "ix_store_orders_coupon",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "coupon_code",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "coupon_discount",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "coupon_id",
                table: "store_orders");
        }
    }
}

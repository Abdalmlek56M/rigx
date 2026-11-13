using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M7_OrderVariantsTermsThumbnails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "terms_accepted_at_utc",
                table: "store_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "terms_policy_version",
                table: "store_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_storage_key_snapshot",
                table: "order_items",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "variant_name",
                table: "order_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "terms_accepted_at_utc",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "terms_policy_version",
                table: "store_orders");

            migrationBuilder.DropColumn(
                name: "thumbnail_storage_key_snapshot",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "variant_name",
                table: "order_items");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M39_NotifyMe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "available_at_utc",
                table: "customer_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "available_item_code",
                table: "customer_requests",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "customer_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_key",
                table: "customer_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "notified_at_utc",
                table: "customer_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_requests_type_model_key",
                table: "customer_requests",
                columns: new[] { "type", "model_key" },
                filter: "model_key IS NOT NULL AND status IN ('New','Following')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customer_requests_type_model_key",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "available_at_utc",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "available_item_code",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "model_key",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "notified_at_utc",
                table: "customer_requests");
        }
    }
}

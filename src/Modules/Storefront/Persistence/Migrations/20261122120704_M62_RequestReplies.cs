using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M62_RequestReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_requests_type",
                table: "customer_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_notifications_kind",
                table: "customer_notifications");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "replied_at_utc",
                table: "customer_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reply",
                table: "customer_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_requests_type",
                table: "customer_requests",
                sql: "type IN ('Sell','TradeIn','Upgrade','NotifyMe','Support','Contact','Service','Complaint','Suggestion')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_notifications_kind",
                table: "customer_notifications",
                sql: "kind IN ('OrderStatus','BackInStock','RequestReply')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_requests_type",
                table: "customer_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_notifications_kind",
                table: "customer_notifications");

            migrationBuilder.DropColumn(
                name: "replied_at_utc",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "reply",
                table: "customer_requests");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_requests_type",
                table: "customer_requests",
                sql: "type IN ('Sell','TradeIn','Upgrade','NotifyMe','Support','Contact','Service')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_notifications_kind",
                table: "customer_notifications",
                sql: "kind IN ('OrderStatus','BackInStock')");
        }
    }
}

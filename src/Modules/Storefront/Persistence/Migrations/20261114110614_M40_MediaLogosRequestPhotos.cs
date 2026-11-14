using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M40_MediaLogosRequestPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "logos",
                table: "store_settings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "photo_token_expires_at_utc",
                table: "customer_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_token_hash",
                table: "customer_requests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "photos",
                table: "customer_requests",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.CreateIndex(
                name: "ix_customer_requests_photo_token_hash",
                table: "customer_requests",
                column: "photo_token_hash",
                filter: "photo_token_hash IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customer_requests_photo_token_hash",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "logos",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "photo_token_expires_at_utc",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "photo_token_hash",
                table: "customer_requests");

            migrationBuilder.DropColumn(
                name: "photos",
                table: "customer_requests");
        }
    }
}

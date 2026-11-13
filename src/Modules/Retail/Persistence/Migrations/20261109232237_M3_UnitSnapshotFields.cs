using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M3_UnitSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "inventory_units",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_code",
                table: "inventory_units",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_storage_keys_json",
                table: "inventory_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "purchase_date",
                table: "inventory_units",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purchase_kind",
                table: "inventory_units",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "inventory_units",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specifications_json",
                table: "inventory_units",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "external_code",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "image_storage_keys_json",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "purchase_date",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "purchase_kind",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "specifications_json",
                table: "inventory_units");
        }
    }
}

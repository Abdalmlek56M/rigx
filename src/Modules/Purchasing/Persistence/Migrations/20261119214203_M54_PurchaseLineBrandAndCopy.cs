using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Purchasing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M54_PurchaseLineBrandAndCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "brand_id",
                table: "purchase_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "copy_photos_from_item_id",
                table: "purchase_lines",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "brand_id",
                table: "purchase_lines");

            migrationBuilder.DropColumn(
                name: "copy_photos_from_item_id",
                table: "purchase_lines");
        }
    }
}

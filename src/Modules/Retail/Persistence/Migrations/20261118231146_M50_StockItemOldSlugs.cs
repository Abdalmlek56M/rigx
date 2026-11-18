using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M50_StockItemOldSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_item_old_slugs",
                columns: table => new
                {
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_item_old_slugs", x => x.slug);
                    table.ForeignKey(
                        name: "FK_stock_item_old_slugs_stock_items_stock_item_id",
                        column: x => x.stock_item_id,
                        principalTable: "stock_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_item_old_slugs_stock_item_id",
                table: "stock_item_old_slugs",
                column: "stock_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_item_old_slugs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M22_RemoveOrderVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE order_items SET name = left(name || ' - ' || variant_name, 255) WHERE variant_name IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "variant_name",
                table: "order_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}

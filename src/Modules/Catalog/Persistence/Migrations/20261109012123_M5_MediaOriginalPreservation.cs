using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M5_MediaOriginalPreservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "original_storage_key",
                table: "product_images",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_images_original_storage_key",
                table: "product_images",
                column: "original_storage_key",
                unique: true,
                filter: "original_storage_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_product_images_original_storage_key",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "original_storage_key",
                table: "product_images");
        }
    }
}

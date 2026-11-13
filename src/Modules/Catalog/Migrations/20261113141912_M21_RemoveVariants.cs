using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class M21_RemoveVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM product_specifications WHERE variant_id IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "fk_product_specifications_variant_product",
                table: "product_specifications");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_specifications_variant_id_product_id",
                table: "product_specifications");

            migrationBuilder.DropIndex(
                name: "ux_product_specifications_product_group_name",
                table: "product_specifications");

            migrationBuilder.DropIndex(
                name: "ux_product_specifications_variant_group_name",
                table: "product_specifications");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "product_specifications");

            migrationBuilder.CreateIndex(
                name: "ux_product_specifications_product_group_name",
                table: "product_specifications",
                columns: new[] { "product_id", "group_name", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_product_specifications_product_group_name",
                table: "product_specifications");

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "product_specifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.id);
                    table.UniqueConstraint("AK_product_variants_id_product_id", x => new { x.id, x.product_id });
                    table.CheckConstraint("ck_product_variants_barcode_not_blank", "barcode IS NULL OR length(btrim(barcode)) > 0");
                    table.CheckConstraint("ck_product_variants_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_product_variants_price", "price IS NULL OR price >= 0");
                    table.CheckConstraint("ck_product_variants_sku_not_blank", "length(btrim(sku)) > 0");
                    table.ForeignKey(
                        name: "FK_product_variants_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_specifications_variant_id_product_id",
                table: "product_specifications",
                columns: new[] { "variant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ux_product_specifications_product_group_name",
                table: "product_specifications",
                columns: new[] { "product_id", "group_name", "name" },
                unique: true,
                filter: "variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_product_specifications_variant_group_name",
                table: "product_specifications",
                columns: new[] { "variant_id", "group_name", "name" },
                unique: true,
                filter: "variant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product",
                table: "product_variants",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_variants_barcode",
                table: "product_variants",
                column: "barcode",
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_product_variants_id_product",
                table: "product_variants",
                columns: new[] { "id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_variants_sku",
                table: "product_variants",
                column: "sku",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_specifications_variant_product",
                table: "product_specifications",
                columns: new[] { "variant_id", "product_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "id", "product_id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Purchasing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M5_SupplierProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_products_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_products_supplier_profiles_supplier_profile_id",
                        column: x => x.supplier_profile_id,
                        principalTable: "supplier_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_products_is_active",
                table: "supplier_products",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_products_product_variant_id",
                table: "supplier_products",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ux_supplier_products_supplier_sku",
                table: "supplier_products",
                columns: new[] { "supplier_profile_id", "supplier_sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_supplier_products_supplier_variant",
                table: "supplier_products",
                columns: new[] { "supplier_profile_id", "product_variant_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_products");
        }
    }
}

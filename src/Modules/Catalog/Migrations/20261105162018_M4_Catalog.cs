using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class M4_Catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    logo_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    support_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.id);
                    table.CheckConstraint("ck_brands_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_brands_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    icon_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.CheckConstraint("ck_categories_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_categories_self_parent", "parent_id IS NULL OR parent_id <> id");
                    table.CheckConstraint("ck_categories_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("ck_categories_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "FK_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    short_description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    base_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Draft"),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("ck_products_barcode_not_blank", "barcode IS NULL OR length(btrim(barcode)) > 0");
                    table.CheckConstraint("ck_products_base_price", "base_price >= 0");
                    table.CheckConstraint("ck_products_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_products_sku_not_blank", "length(btrim(sku)) > 0");
                    table.CheckConstraint("ck_products_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("ck_products_status", "status IN ('Draft', 'Published')");
                    table.ForeignKey(
                        name: "FK_products_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    thumbnail_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    alt_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    thumbnail_width = table.Column<int>(type: "integer", nullable: false),
                    thumbnail_height = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.CheckConstraint("ck_product_images_alt_not_blank", "length(btrim(alt_text)) > 0");
                    table.CheckConstraint("ck_product_images_dimensions", "width > 0 AND height > 0 AND thumbnail_width > 0 AND thumbnail_height > 0");
                    table.CheckConstraint("ck_product_images_file_size", "file_size_bytes > 0 AND file_size_bytes <= 8388608");
                    table.CheckConstraint("ck_product_images_sort_order", "sort_order >= 0");
                    table.CheckConstraint("ck_product_images_storage_not_blank", "length(btrim(storage_key)) > 0");
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
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

            migrationBuilder.CreateTable(
                name: "product_specifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_specifications", x => x.id);
                    table.CheckConstraint("ck_product_specifications_group_not_blank", "length(btrim(group_name)) > 0");
                    table.CheckConstraint("ck_product_specifications_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_product_specifications_sort_order", "sort_order >= 0");
                    table.CheckConstraint("ck_product_specifications_value_not_blank", "length(btrim(value)) > 0");
                    table.ForeignKey(
                        name: "FK_product_specifications_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_specifications_variant_product",
                        columns: x => new { x.variant_id, x.product_id },
                        principalTable: "product_variants",
                        principalColumns: new[] { "id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_brands_slug",
                table: "brands",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_sort",
                table: "categories",
                columns: new[] { "parent_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_sort",
                table: "product_images",
                columns: new[] { "product_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_product_images_one_primary",
                table: "product_images",
                column: "product_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ux_product_images_storage_key",
                table: "product_images",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_images_thumbnail_storage_key",
                table: "product_images",
                column: "thumbnail_storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_specifications_product_sort",
                table: "product_specifications",
                columns: new[] { "product_id", "sort_order" });

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

            migrationBuilder.CreateIndex(
                name: "ix_products_brand_status",
                table: "products",
                columns: new[] { "brand_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_status_category",
                table: "products",
                columns: new[] { "status", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ux_products_barcode",
                table: "products",
                column: "barcode",
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_products_sku",
                table: "products",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_products_slug",
                table: "products",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "product_specifications");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}

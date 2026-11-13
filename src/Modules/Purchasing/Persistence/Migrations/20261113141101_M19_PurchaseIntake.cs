using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Purchasing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M19_PurchaseIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PurchaseIntakeData.CreateSequence);

            migrationBuilder.CreateTable(
                name: "sellers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    national_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    governorate = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    area = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sellers", x => x.id);
                    table.CheckConstraint("ck_sellers_kind", "kind IN ('Person','Trader','Company')");
                });

            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    keep_as_build = table.Column<bool>(type: "boolean", nullable: false),
                    build_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    trade_in_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    trade_in_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    remaining = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assembly_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.id);
                    table.CheckConstraint("ck_purchases_kind", "kind IN ('Parts','FullPc')");
                    table.CheckConstraint("ck_purchases_method", "payment_method IN ('Cash','InstaPay','Wallet','TradeIn','CashAndTradeIn')");
                    table.CheckConstraint("ck_purchases_money", "total >= 0 AND paid_amount >= 0 AND trade_in_value >= 0 AND remaining >= 0");
                    table.CheckConstraint("ck_purchases_status", "status IN ('Draft','Received','Cancelled')");
                    table.ForeignKey(
                        name: "FK_purchases_sellers_seller_id",
                        column: x => x.seller_id,
                        principalTable: "sellers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    condition = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    with_board_extra = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    alone_extra = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    partner_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    warranty_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    has_box = table.Column<bool>(type: "boolean", nullable: false),
                    internal_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    specs = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_lines", x => x.id);
                    table.CheckConstraint("ck_purchase_lines_condition", "condition IN ('New','Used')");
                    table.CheckConstraint("ck_purchase_lines_money", "unit_cost >= 0 AND line_total >= 0 AND price >= 0");
                    table.CheckConstraint("ck_purchase_lines_quantity", "quantity BETWEEN 1 AND 1000");
                    table.ForeignKey(
                        name: "FK_purchase_lines_purchases_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "purchases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_lines_purchase",
                table: "purchase_lines",
                column: "purchase_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_seller",
                table: "purchases",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_status_created",
                table: "purchases",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_purchases_code",
                table: "purchases",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_sellers_phone",
                table: "sellers",
                column: "phone",
                unique: true);

            migrationBuilder.Sql(PurchaseIntakeData.ConvertOldRows);

            migrationBuilder.DropTable(
                name: "purchase_draft_items");

            migrationBuilder.DropTable(
                name: "supplier_products");

            migrationBuilder.DropTable(
                name: "purchase_drafts");

            migrationBuilder.DropTable(
                name: "supplier_profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PurchaseIntakeData.DropSequence);

            migrationBuilder.DropTable(
                name: "purchase_lines");

            migrationBuilder.DropTable(
                name: "purchases");

            migrationBuilder.DropTable(
                name: "sellers");

            migrationBuilder.CreateTable(
                name: "supplier_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_account_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    bank_iban = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    commercial_registration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    contact_person_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contact_person_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    tax_id_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_profiles", x => x.id);
                    table.CheckConstraint("ck_supplier_profiles_payment_terms_days", "payment_terms_days >= 0 AND payment_terms_days <= 3650");
                    table.ForeignKey(
                        name: "FK_supplier_profiles_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_seller_contact = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    external_seller_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    listing_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    marketplace_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: true),
                    purchase_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_drafts", x => x.id);
                    table.CheckConstraint("ck_purchase_drafts_kind", "purchase_kind IN ('Component','Components','Prebuilt','Bundle','Other')");
                    table.CheckConstraint("ck_purchase_drafts_registered_supplier", "source_type <> 'RegisteredSupplier' OR supplier_id IS NOT NULL");
                    table.CheckConstraint("ck_purchase_drafts_source", "source_type IN ('RegisteredSupplier','Marketplace','IndividualSeller','Other')");
                    table.CheckConstraint("ck_purchase_drafts_status", "status IN ('Draft','Cancelled','Received')");
                    table.CheckConstraint("ck_purchase_drafts_total", "total_amount >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_drafts_supplier_profiles_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    supplier_sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "purchase_draft_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    category = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    item_condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    external_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    item_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    item_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    model = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    specifications_json = table.Column<string>(type: "jsonb", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_draft_items", x => x.id);
                    table.CheckConstraint("ck_purchase_draft_items_price", "unit_price > 0");
                    table.CheckConstraint("ck_purchase_draft_items_quantity", "quantity > 0 AND quantity <= 100000");
                    table.CheckConstraint("ck_purchase_draft_items_total", "line_total > 0");
                    table.ForeignKey(
                        name: "FK_purchase_draft_items_purchase_drafts_draft_id",
                        column: x => x.draft_id,
                        principalTable: "purchase_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_draft_items_draft",
                table: "purchase_draft_items",
                column: "draft_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_drafts_source",
                table: "purchase_drafts",
                column: "source_type");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_drafts_status_created",
                table: "purchase_drafts",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_drafts_supplier_id",
                table: "purchase_drafts",
                column: "supplier_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_supplier_profiles_is_active",
                table: "supplier_profiles",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_supplier_profiles_commercial_registration",
                table: "supplier_profiles",
                column: "commercial_registration",
                unique: true,
                filter: "commercial_registration IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_supplier_profiles_person_id",
                table: "supplier_profiles",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_supplier_profiles_tax_id_number",
                table: "supplier_profiles",
                column: "tax_id_number",
                unique: true,
                filter: "tax_id_number IS NOT NULL");
        }
    }
}

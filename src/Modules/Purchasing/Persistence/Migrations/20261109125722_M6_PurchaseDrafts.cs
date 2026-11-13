using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Purchasing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M6_PurchaseDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchase_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    marketplace_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    external_seller_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    external_seller_contact = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    listing_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_drafts", x => x.id);
                    table.CheckConstraint("ck_purchase_drafts_registered_supplier", "source_type <> 'RegisteredSupplier' OR supplier_id IS NOT NULL");
                    table.CheckConstraint("ck_purchase_drafts_source", "source_type IN ('RegisteredSupplier','Marketplace','IndividualSeller','Other')");
                    table.CheckConstraint("ck_purchase_drafts_status", "status IN ('Draft','Cancelled')");
                    table.CheckConstraint("ck_purchase_drafts_total", "total_amount >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_drafts_supplier_profiles_supplier_id",
                        column: x => x.supplier_id,
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
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_draft_items");

            migrationBuilder.DropTable(
                name: "purchase_drafts");
        }
    }
}

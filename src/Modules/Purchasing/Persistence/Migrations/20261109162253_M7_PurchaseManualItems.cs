using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Purchasing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M7_PurchaseManualItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "purchase_kind",
                table: "purchase_drafts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Component");

            migrationBuilder.AddColumn<string>(
                name: "brand",
                table: "purchase_draft_items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "purchase_draft_items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "purchase_draft_items",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_code",
                table: "purchase_draft_items",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_condition",
                table: "purchase_draft_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_kind",
                table: "purchase_draft_items",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Component");

            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "purchase_draft_items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specifications_json",
                table: "purchase_draft_items",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_purchase_drafts_kind",
                table: "purchase_drafts",
                sql: "purchase_kind IN ('Component','Components','Prebuilt','Bundle','Other')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_purchase_drafts_kind",
                table: "purchase_drafts");

            migrationBuilder.DropColumn(
                name: "purchase_kind",
                table: "purchase_drafts");

            migrationBuilder.DropColumn(
                name: "brand",
                table: "purchase_draft_items");

            migrationBuilder.DropColumn(
                name: "category",
                table: "purchase_draft_items");

            migrationBuilder.DropColumn(
                name: "description",
                table: "purchase_draft_items");

            migrationBuilder.DropColumn(
                name: "external_code",
                table: "purchase_draft_items");

            migrationBuilder.DropColumn(
                name: "item_condition",
                table: "purchase_draft_items");

            migrationBuilder.DropColumn(
                name: "item_kind",
                table: "purchase_draft_items");

            migrationBuilder.DropColumn(
                name: "model",
                table: "purchase_draft_items");

            migrationBuilder.DropColumn(
                name: "specifications_json",
                table: "purchase_draft_items");
        }
    }
}

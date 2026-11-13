using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Purchasing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M8_PurchaseReceiptStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_purchase_drafts_status",
                table: "purchase_drafts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_purchase_drafts_status",
                table: "purchase_drafts",
                sql: "status IN ('Draft','Cancelled','Received')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_purchase_drafts_status",
                table: "purchase_drafts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_purchase_drafts_status",
                table: "purchase_drafts",
                sql: "status IN ('Draft','Cancelled')");
        }
    }
}

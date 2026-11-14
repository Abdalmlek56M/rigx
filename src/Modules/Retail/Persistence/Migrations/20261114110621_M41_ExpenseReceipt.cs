using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M41_ExpenseReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "attachment_reference",
                table: "retail_expenses",
                newName: "receipt_storage_key");

            // Old values were free text, not storage keys.
            migrationBuilder.Sql("UPDATE retail_expenses SET receipt_storage_key = NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "receipt_storage_key",
                table: "retail_expenses",
                newName: "attachment_reference");
        }
    }
}

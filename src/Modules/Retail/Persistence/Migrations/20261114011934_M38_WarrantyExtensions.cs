using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M38_WarrantyExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "warranty_extensions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warranty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    months = table.Column<int>(type: "integer", nullable: false),
                    previous_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    new_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warranty_extensions", x => x.id);
                    table.CheckConstraint("ck_warranty_extensions_months", "months BETWEEN 1 AND 36");
                    table.ForeignKey(
                        name: "FK_warranty_extensions_warranties_warranty_id",
                        column: x => x.warranty_id,
                        principalTable: "warranties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_warranty_extensions_warranty",
                table: "warranty_extensions",
                column: "warranty_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "warranty_extensions");
        }
    }
}

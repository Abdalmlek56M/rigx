using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Purchasing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M5_SupplierDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tax_id_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    commercial_registration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    contact_person_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contact_person_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    bank_iban = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_profiles");
        }
    }
}

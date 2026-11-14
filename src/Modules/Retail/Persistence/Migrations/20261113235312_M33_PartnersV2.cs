using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M33_PartnersV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    join_date = table.Column<DateOnly>(type: "date", nullable: false),
                    default_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    basis = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    loss_sharing_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partners", x => x.id);
                    table.CheckConstraint("ck_partners_percent", "default_percent BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "partner_capital_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_capital_entries", x => x.id);
                    table.CheckConstraint("ck_partner_capital_entries_amount", "amount > 0");
                    table.CheckConstraint("ck_partner_capital_entries_kind", "kind IN ('Deposit','Withdrawal')");
                    table.ForeignKey(
                        name: "FK_partner_capital_entries_partners_partner_id",
                        column: x => x.partner_id,
                        principalTable: "partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"INSERT INTO partners (id, name, phone, join_date, default_percent, basis, loss_sharing_enabled, is_active, notes, created_at_utc, updated_at_utc, row_version)
SELECT gen_random_uuid(), a.partner_name, NULL, a.effective_from, a.default_percent, 'FinancedParts', a.loss_sharing_enabled, a.is_active, NULL, now(), now(), 1
FROM (SELECT DISTINCT ON (partner_name) * FROM partner_agreements ORDER BY partner_name, effective_from DESC, created_at_utc DESC) a;");

            migrationBuilder.Sql(@"INSERT INTO partners (id, name, phone, join_date, default_percent, basis, loss_sharing_enabled, is_active, notes, created_at_utc, updated_at_utc, row_version)
SELECT gen_random_uuid(), n.name, NULL, current_date, 0, 'FinancedParts', false, true, NULL, now(), now(), 1
FROM (SELECT partner_name AS name FROM partner_earnings UNION SELECT partner_name FROM partner_settlements
      UNION SELECT partner_name FROM partner_adjustments UNION SELECT partner_name FROM partner_unit_allocations) n
WHERE NOT EXISTS (SELECT 1 FROM partners p WHERE p.name = n.name);");

            migrationBuilder.AddColumn<Guid>(
                name: "partner_id",
                table: "partner_settlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "partner_id",
                table: "partner_earnings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "partner_id",
                table: "partner_adjustments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "partner_id",
                table: "inventory_units",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "period_month",
                table: "partner_earnings",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "order_item_id",
                table: "partner_earnings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "partner_earnings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.Sql(@"UPDATE partner_settlements t SET partner_id = p.id FROM partners p WHERE p.name = t.partner_name;");

            migrationBuilder.Sql(@"UPDATE partner_earnings t SET partner_id = p.id FROM partners p WHERE p.name = t.partner_name;");

            migrationBuilder.Sql(@"UPDATE partner_adjustments t SET partner_id = p.id FROM partners p WHERE p.name = t.partner_name;");

            migrationBuilder.Sql(@"UPDATE inventory_units u SET partner_id = x.partner_id, partner_percent_override = x.percent
FROM (SELECT DISTINCT ON (a.unit_id) a.unit_id, p.id AS partner_id, a.percent
      FROM partner_unit_allocations a JOIN partners p ON p.name = a.partner_name
      WHERE a.is_active AND a.unit_id IS NOT NULL ORDER BY a.unit_id, a.percent DESC) x
WHERE u.id = x.unit_id;");

            migrationBuilder.Sql(@"UPDATE inventory_units SET partner_id = (SELECT p.id FROM partner_agreements a JOIN partners p ON p.name = a.partner_name
                                              WHERE a.is_active ORDER BY a.effective_from DESC, a.created_at_utc DESC LIMIT 1)
WHERE partner_id IS NULL AND partner_percent_override IS NOT NULL;");

            migrationBuilder.Sql(@"UPDATE inventory_units SET partner_percent_override = NULL WHERE partner_id IS NULL AND partner_percent_override IS NOT NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "partner_id",
                table: "partner_settlements",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "partner_id",
                table: "partner_earnings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "partner_id",
                table: "partner_adjustments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "ux_partner_earnings_item_unit_partner",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "partner_name",
                table: "partner_settlements");

            migrationBuilder.DropColumn(
                name: "partner_name",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "partner_name",
                table: "partner_adjustments");

            migrationBuilder.DropTable(
                name: "partner_agreements");

            migrationBuilder.DropTable(
                name: "partner_allocation_events");

            migrationBuilder.DropTable(
                name: "partner_unit_allocations");

            migrationBuilder.CreateIndex(
                name: "ix_partner_settlements_partner",
                table: "partner_settlements",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_partner_earnings_partner",
                table: "partner_earnings",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ux_partner_earnings_item_unit_partner",
                table: "partner_earnings",
                columns: new[] { "order_item_id", "unit_id", "partner_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_partner_earnings_partner_month",
                table: "partner_earnings",
                columns: new[] { "partner_id", "period_month" },
                unique: true,
                filter: "period_month IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_partner_adjustments_partner",
                table: "partner_adjustments",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_units_partner",
                table: "inventory_units",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_partner_capital_entries_partner",
                table: "partner_capital_entries",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ux_partners_name",
                table: "partners",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_units_partners_partner_id",
                table: "inventory_units",
                column: "partner_id",
                principalTable: "partners",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_partner_adjustments_partners_partner_id",
                table: "partner_adjustments",
                column: "partner_id",
                principalTable: "partners",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_partner_earnings_partners_partner_id",
                table: "partner_earnings",
                column: "partner_id",
                principalTable: "partners",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_partner_settlements_partners_partner_id",
                table: "partner_settlements",
                column: "partner_id",
                principalTable: "partners",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_units_partners_partner_id",
                table: "inventory_units");

            migrationBuilder.DropForeignKey(
                name: "FK_partner_adjustments_partners_partner_id",
                table: "partner_adjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_partner_earnings_partners_partner_id",
                table: "partner_earnings");

            migrationBuilder.DropForeignKey(
                name: "FK_partner_settlements_partners_partner_id",
                table: "partner_settlements");

            migrationBuilder.DropTable(
                name: "partner_capital_entries");

            migrationBuilder.DropTable(
                name: "partners");

            migrationBuilder.DropIndex(
                name: "ix_partner_settlements_partner",
                table: "partner_settlements");

            migrationBuilder.DropIndex(
                name: "ix_partner_earnings_partner",
                table: "partner_earnings");

            migrationBuilder.DropIndex(
                name: "ux_partner_earnings_item_unit_partner",
                table: "partner_earnings");

            migrationBuilder.DropIndex(
                name: "ux_partner_earnings_partner_month",
                table: "partner_earnings");

            migrationBuilder.DropIndex(
                name: "ix_partner_adjustments_partner",
                table: "partner_adjustments");

            migrationBuilder.DropIndex(
                name: "ix_inventory_units_partner",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "partner_id",
                table: "partner_settlements");

            migrationBuilder.DropColumn(
                name: "partner_id",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "period_month",
                table: "partner_earnings");

            migrationBuilder.DropColumn(
                name: "partner_id",
                table: "partner_adjustments");

            migrationBuilder.DropColumn(
                name: "partner_id",
                table: "inventory_units");

            migrationBuilder.AddColumn<string>(
                name: "partner_name",
                table: "partner_settlements",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "order_item_id",
                table: "partner_earnings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "partner_earnings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "partner_name",
                table: "partner_earnings",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "partner_name",
                table: "partner_adjustments",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "partner_agreements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    default_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    loss_sharing_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    partner_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_agreements", x => x.id);
                    table.CheckConstraint("ck_partner_agreements_percent", "default_percent BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "partner_allocation_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    partner_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    snapshot_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_allocation_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "partner_unit_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    loss_sharing_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    partner_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_unit_allocations", x => x.id);
                    table.CheckConstraint("ck_partner_unit_allocations_percent", "percent > 0 AND percent <= 100");
                });

            migrationBuilder.CreateIndex(
                name: "ux_partner_earnings_item_unit_partner",
                table: "partner_earnings",
                columns: new[] { "order_item_id", "unit_id", "partner_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_partner_allocation_events_allocation",
                table: "partner_allocation_events",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ux_partner_unit_allocations_scope",
                table: "partner_unit_allocations",
                columns: new[] { "partner_name", "unit_id", "product_id", "category" },
                unique: true);
        }
    }
}

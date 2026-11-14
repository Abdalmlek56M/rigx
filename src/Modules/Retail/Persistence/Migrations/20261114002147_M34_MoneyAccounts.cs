using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M34_MoneyAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assembly_id",
                table: "retail_expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "money_account_id",
                table: "retail_expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "paid_by_partner_id",
                table: "retail_expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "recurring_expense_id",
                table: "retail_expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "expense_id",
                table: "partner_settlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "money_account_id",
                table: "order_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "expense_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "money_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    opening_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    opening_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_accounts", x => x.id);
                    table.CheckConstraint("ck_money_accounts_kind", "kind IN ('Cash','InstaPay','Wallet','BankTransfer')");
                    table.CheckConstraint("ck_money_accounts_opening", "opening_balance >= 0");
                });

            migrationBuilder.CreateTable(
                name: "money_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    source = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_movements", x => x.id);
                    table.CheckConstraint("ck_money_movements_amount", "amount <> 0");
                    table.ForeignKey(
                        name: "FK_money_movements_money_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "money_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recurring_expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    day_of_month = table.Column<int>(type: "integer", nullable: false),
                    money_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    start_month = table.Column<DateOnly>(type: "date", nullable: false),
                    last_posted_month = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_expenses", x => x.id);
                    table.CheckConstraint("ck_recurring_expenses_amount", "amount > 0");
                    table.CheckConstraint("ck_recurring_expenses_day", "day_of_month BETWEEN 1 AND 28");
                    table.ForeignKey(
                        name: "FK_recurring_expenses_money_accounts_money_account_id",
                        column: x => x.money_account_id,
                        principalTable: "money_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_retail_expenses_money_account_id",
                table: "retail_expenses",
                column: "money_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_retail_expenses_paid_by_partner_id",
                table: "retail_expenses",
                column: "paid_by_partner_id");

            migrationBuilder.CreateIndex(
                name: "ux_retail_expenses_recurring",
                table: "retail_expenses",
                columns: new[] { "recurring_expense_id", "expense_date" },
                unique: true,
                filter: "recurring_expense_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_partner_settlements_expense_id",
                table: "partner_settlements",
                column: "expense_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_payments_money_account_id",
                table: "order_payments",
                column: "money_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_expense_types_name",
                table: "expense_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_money_accounts_name",
                table: "money_accounts",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_money_movements_account_date",
                table: "money_movements",
                columns: new[] { "account_id", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "ux_money_movements_source",
                table: "money_movements",
                columns: new[] { "source", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_expenses_money_account_id",
                table: "recurring_expenses",
                column: "money_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_payments_money_accounts_money_account_id",
                table: "order_payments",
                column: "money_account_id",
                principalTable: "money_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_partner_settlements_retail_expenses_expense_id",
                table: "partner_settlements",
                column: "expense_id",
                principalTable: "retail_expenses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_retail_expenses_money_accounts_money_account_id",
                table: "retail_expenses",
                column: "money_account_id",
                principalTable: "money_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_retail_expenses_partners_paid_by_partner_id",
                table: "retail_expenses",
                column: "paid_by_partner_id",
                principalTable: "partners",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_retail_expenses_recurring_expenses_recurring_expense_id",
                table: "retail_expenses",
                column: "recurring_expense_id",
                principalTable: "recurring_expenses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"INSERT INTO money_accounts (id, name, kind, opening_balance, opening_date, is_active, sort_order, created_at_utc, row_version) VALUES
('0199a000-0000-7000-8000-000000000001', 'الخزنة', 'Cash', 0, DATE '2026-11-14', true, 1, now(), 1),
('0199a000-0000-7000-8000-000000000002', 'InstaPay', 'InstaPay', 0, DATE '2026-11-14', true, 2, now(), 1),
('0199a000-0000-7000-8000-000000000003', 'المحفظة', 'Wallet', 0, DATE '2026-11-14', true, 3, now(), 1),
('0199a000-0000-7000-8000-000000000004', 'حساب البنك', 'BankTransfer', 0, DATE '2026-11-14', true, 4, now(), 1);");

            migrationBuilder.Sql(@"INSERT INTO expense_types (id, name, is_active, sort_order, created_at_utc, row_version) VALUES
('0199a000-0000-7000-8000-000000000101', 'إيجار', true, 1, now(), 1),
('0199a000-0000-7000-8000-000000000102', 'كهرباء ومياه', true, 2, now(), 1),
('0199a000-0000-7000-8000-000000000103', 'إنترنت', true, 3, now(), 1),
('0199a000-0000-7000-8000-000000000104', 'مرتبات', true, 4, now(), 1),
('0199a000-0000-7000-8000-000000000105', 'شحن وتوصيل', true, 5, now(), 1),
('0199a000-0000-7000-8000-000000000106', 'تغليف', true, 6, now(), 1),
('0199a000-0000-7000-8000-000000000107', 'إعلانات', true, 7, now(), 1),
('0199a000-0000-7000-8000-000000000108', 'صيانة', true, 8, now(), 1),
('0199a000-0000-7000-8000-000000000109', 'أخرى', true, 9, now(), 1);");

            migrationBuilder.Sql(@"INSERT INTO expense_types (id, name, is_active, sort_order, created_at_utc, row_version)
SELECT gen_random_uuid(), c.category, true, 100, now(), 1
FROM (SELECT DISTINCT category FROM retail_expenses) c
WHERE NOT EXISTS (SELECT 1 FROM expense_types t WHERE t.name = c.category);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_payments_money_accounts_money_account_id",
                table: "order_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_partner_settlements_retail_expenses_expense_id",
                table: "partner_settlements");

            migrationBuilder.DropForeignKey(
                name: "FK_retail_expenses_money_accounts_money_account_id",
                table: "retail_expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_retail_expenses_partners_paid_by_partner_id",
                table: "retail_expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_retail_expenses_recurring_expenses_recurring_expense_id",
                table: "retail_expenses");

            migrationBuilder.DropTable(
                name: "expense_types");

            migrationBuilder.DropTable(
                name: "money_movements");

            migrationBuilder.DropTable(
                name: "recurring_expenses");

            migrationBuilder.DropTable(
                name: "money_accounts");

            migrationBuilder.DropIndex(
                name: "IX_retail_expenses_money_account_id",
                table: "retail_expenses");

            migrationBuilder.DropIndex(
                name: "IX_retail_expenses_paid_by_partner_id",
                table: "retail_expenses");

            migrationBuilder.DropIndex(
                name: "ux_retail_expenses_recurring",
                table: "retail_expenses");

            migrationBuilder.DropIndex(
                name: "IX_partner_settlements_expense_id",
                table: "partner_settlements");

            migrationBuilder.DropIndex(
                name: "IX_order_payments_money_account_id",
                table: "order_payments");

            migrationBuilder.DropColumn(
                name: "assembly_id",
                table: "retail_expenses");

            migrationBuilder.DropColumn(
                name: "money_account_id",
                table: "retail_expenses");

            migrationBuilder.DropColumn(
                name: "paid_by_partner_id",
                table: "retail_expenses");

            migrationBuilder.DropColumn(
                name: "recurring_expense_id",
                table: "retail_expenses");

            migrationBuilder.DropColumn(
                name: "expense_id",
                table: "partner_settlements");

            migrationBuilder.DropColumn(
                name: "money_account_id",
                table: "order_payments");
        }
    }
}

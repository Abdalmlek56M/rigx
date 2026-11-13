using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M3_1_IdentityPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "person_code_counters",
                columns: table => new
                {
                    year_month = table.Column<string>(type: "character(6)", fixedLength: true, maxLength: 6, nullable: false),
                    last_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_code_counters", x => x.year_month);
                    table.CheckConstraint("ck_person_code_counters_last_value", "last_value BETWEEN 1 AND 99999");
                    table.CheckConstraint("ck_person_code_counters_year_month", "year_month ~ '^[0-9]{6}$'");
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_customer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_supplier = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_employee = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persons", x => x.id);
                    table.CheckConstraint("ck_persons_person_code_format", "person_code ~ '^PER-[0-9]{6}-[0-9]{5}$'");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_accounts_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_code });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_code",
                        column: x => x.permission_code,
                        principalTable: "permissions",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_account_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                column: "code",
                values: new object[]
                {
                    "Finance.Disburse",
                    "Finance.ManageExpenses",
                    "Finance.VerifyPayment",
                    "Finance.ViewReports",
                    "Finance.ViewVaults",
                    "Inventory.ManageSerials",
                    "Inventory.StockAdjust",
                    "Inventory.StockCount",
                    "Inventory.Transfer",
                    "Inventory.View",
                    "Invoices.IssueFinal",
                    "Invoices.ManageTemplates",
                    "Invoices.Reprint",
                    "Invoices.ReviewDraft",
                    "Purchasing.CreateOrder",
                    "Purchasing.ManageSuppliers",
                    "Purchasing.PostBill",
                    "Purchasing.ReceiveGoods",
                    "Purchasing.View",
                    "Sales.ApproveOrder",
                    "Sales.CreatePOS",
                    "Sales.FulfillStorePickup",
                    "Sales.LogCommunication",
                    "Sales.OverrideMinimumPrice",
                    "Sales.ProcessReturn",
                    "Sales.ViewOrders",
                    "System.ExecuteAllowlistedAction",
                    "System.ManageBackups",
                    "System.ManageUsers",
                    "System.StoreSettings",
                    "System.ViewAuditLogs",
                    "System.ViewErrorLogs"
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "name" },
                values: new object[] { new Guid("11111111-1111-4111-8111-111111111111"), "Customer", "Customer" });

            migrationBuilder.CreateIndex(
                name: "ix_persons_email",
                table: "persons",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_persons_phone_number",
                table: "persons",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ux_persons_person_code",
                table: "persons",
                column: "person_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_code",
                table: "role_permissions",
                column: "permission_code");

            migrationBuilder.CreateIndex(
                name: "ux_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_accounts_email",
                table: "user_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_accounts_person_id",
                table: "user_accounts",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_accounts_username",
                table: "user_accounts",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_code_counters");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "persons");
        }
    }
}

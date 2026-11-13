using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M30_CustomerRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    governorate = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    part_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    model_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    owned_for = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    item_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    service_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    preferred_date = table.Column<DateOnly>(type: "date", nullable: true),
                    offer_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    expected_resale = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    internal_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchase_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_requests", x => x.id);
                    table.CheckConstraint("ck_customer_requests_service_kind", "service_kind IS NULL OR service_kind IN ('Consult','Build','Test','Maintenance','Sourcing','Upgrade')");
                    table.CheckConstraint("ck_customer_requests_status", "status IN ('New','Following','Offered','Accepted','Rejected','Converted','Closed')");
                    table.CheckConstraint("ck_customer_requests_type", "type IN ('Sell','TradeIn','Upgrade','NotifyMe','Support','Contact','Service')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_requests_phone_created",
                table: "customer_requests",
                columns: new[] { "phone", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_requests_status_created",
                table: "customer_requests",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_requests_type_status",
                table: "customer_requests",
                columns: new[] { "type", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_customer_requests_code",
                table: "customer_requests",
                column: "code",
                unique: true);

            migrationBuilder.Sql("CREATE SEQUENCE customer_request_code_seq;");

            // Old contact messages become Contact requests (Read -> Following), numbered oldest first.
            // Phones follow EgyptMobile (01xxxxxxxxx): +20 / 0020 removed, leading 0 added; anything else keeps its digits.
            migrationBuilder.Sql("""
                INSERT INTO customer_requests (id, code, type, status, name, phone, email, subject, message,
                                               created_at_utc, updated_at_utc, closed_at_utc, row_version)
                SELECT m.id, 'REQ-' || lpad(n.num::text, 4, '0'), 'Contact',
                       CASE m.status WHEN 'Read' THEN 'Following' ELSE m.status END,
                       m.full_name, CASE WHEN length(p.local) = 10 AND p.local LIKE '1%' THEN '0' || p.local ELSE p.local END,
                       m.email, m.subject, m.message,
                       m.created_at_utc, m.updated_at_utc, CASE WHEN m.status = 'Closed' THEN m.updated_at_utc END, 1
                FROM contact_messages m
                JOIN (SELECT id, row_number() OVER (ORDER BY created_at_utc, id) AS num FROM contact_messages) n ON n.id = m.id
                CROSS JOIN LATERAL (SELECT regexp_replace(coalesce(m.phone, ''), '\D', '', 'g') AS digits) d
                CROSS JOIN LATERAL (SELECT CASE
                    WHEN d.digits LIKE '0020%' THEN substr(d.digits, 5)
                    WHEN d.digits LIKE '20%' AND length(d.digits) = 12 THEN substr(d.digits, 3)
                    ELSE d.digits END AS local) p;
                """);
            migrationBuilder.Sql("SELECT setval('customer_request_code_seq', count(*)) FROM customer_requests HAVING count(*) > 0;");

            migrationBuilder.DropTable(
                name: "contact_messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_messages", x => x.id);
                    table.CheckConstraint("ck_contact_messages_status", "status IN ('New','Read','Closed')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_contact_messages_created",
                table: "contact_messages",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_contact_messages_email_created",
                table: "contact_messages",
                columns: new[] { "email", "created_at_utc" });

            migrationBuilder.Sql("""
                INSERT INTO contact_messages (id, full_name, email, phone, subject, message, status, created_at_utc, updated_at_utc, row_version)
                SELECT id, name, coalesce(email, ''), nullif(phone, ''), coalesce(subject, '-'), coalesce(message, ''),
                       CASE status WHEN 'New' THEN 'New' WHEN 'Closed' THEN 'Closed' ELSE 'Read' END,
                       created_at_utc, updated_at_utc, 1
                FROM customer_requests
                WHERE type = 'Contact';
                """);

            migrationBuilder.DropTable(
                name: "customer_requests");

            migrationBuilder.Sql("DROP SEQUENCE customer_request_code_seq;");
        }
    }
}

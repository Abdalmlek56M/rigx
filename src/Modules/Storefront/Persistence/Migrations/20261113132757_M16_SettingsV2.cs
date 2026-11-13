using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M16_SettingsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            string[] sections =
            [
                "store_info", "contact_links", "shipping", "payment", "watermark", "controls",
                "whatsapp_templates", "reviews", "language", "invoice", "lead_sources", "warranty_options"
            ];
            foreach (var section in sections)
                migrationBuilder.AddColumn<string>(
                    name: section,
                    table: "store_settings",
                    type: "jsonb",
                    nullable: false,
                    defaultValueSql: "'{}'::jsonb");

            // Carry the v1 values into the new sections; '{}' keeps the section defaults.
            migrationBuilder.Sql("""
                UPDATE store_settings SET
                  store_info = jsonb_strip_nulls(jsonb_build_object(
                    'pickupAddress', pickup_address, 'pickupHours', pickup_hours,
                    'pickupInstructions', pickup_instructions, 'warrantySummary', warranty_summary,
                    'serviceSummary', service_summary, 'consultationSummary', consultation_summary)),
                  whatsapp_templates = jsonb_strip_nulls(jsonb_build_object('general', whatsapp_message)),
                  contact_links = CASE
                    WHEN num_nonnulls(whatsapp_number, facebook_url, telegram_url, tiktok_url, youtube_url, instagram_url) = 0
                      THEN '{}'::jsonb
                    ELSE jsonb_build_object('links', (
                      SELECT coalesce(jsonb_agg(link ORDER BY position), '[]'::jsonb)
                      FROM (VALUES
                        (1, 'WhatsApp', whatsapp_number, true),
                        (2, 'Phone', whatsapp_number, true),
                        (3, 'Facebook', facebook_url, false),
                        (4, 'Instagram', instagram_url, false),
                        (5, 'TikTok', tiktok_url, false),
                        (6, 'YouTube', youtube_url, false),
                        (7, 'Telegram', telegram_url, false)
                      ) AS v1(position, platform, value, contact)
                      CROSS JOIN LATERAL (SELECT jsonb_build_object(
                        'platform', platform, 'value', value, 'showOnSite', true,
                        'showOnInvoice', contact, 'showInFloatingButton', contact) AS link) AS item
                      WHERE value IS NOT NULL))
                  END;
                """);

            foreach (var column in new[]
                     {
                         "whatsapp_number", "whatsapp_message", "facebook_url", "telegram_url", "tiktok_url",
                         "youtube_url", "instagram_url", "pickup_address", "pickup_hours", "pickup_instructions",
                         "warranty_summary", "service_summary", "consultation_summary"
                     })
                migrationBuilder.DropColumn(name: column, table: "store_settings");

            migrationBuilder.DropTable(name: "homepage_layouts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contact_links",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "controls",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "invoice",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "language",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "lead_sources",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "payment",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "reviews",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "shipping",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "store_info",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "warranty_options",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "watermark",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "whatsapp_templates",
                table: "store_settings");

            migrationBuilder.AddColumn<string>(
                name: "consultation_summary",
                table: "store_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "facebook_url",
                table: "store_settings",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instagram_url",
                table: "store_settings",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_address",
                table: "store_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_hours",
                table: "store_settings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_instructions",
                table: "store_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "service_summary",
                table: "store_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telegram_url",
                table: "store_settings",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tiktok_url",
                table: "store_settings",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "warranty_summary",
                table: "store_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp_message",
                table: "store_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp_number",
                table: "store_settings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "youtube_url",
                table: "store_settings",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "homepage_layouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    layout_json = table.Column<string>(type: "jsonb", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_layouts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_homepage_layouts_status",
                table: "homepage_layouts",
                column: "status",
                unique: true);
        }
    }
}

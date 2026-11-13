using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M5_StorefrontSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    whatsapp_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    whatsapp_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    facebook_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    telegram_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    tiktok_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    youtube_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    pickup_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pickup_hours = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    pickup_instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    warranty_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    service_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    consultation_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_settings", x => x.id);
                });

            migrationBuilder.Sql(
                "INSERT INTO store_settings (id, row_version, created_at_utc, updated_at_utc) VALUES (1, 1, now(), now()) ON CONFLICT (id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_settings");
        }
    }
}

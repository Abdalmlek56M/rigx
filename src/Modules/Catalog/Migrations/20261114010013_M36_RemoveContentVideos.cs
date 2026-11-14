using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class M36_RemoveContentVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_videos");

            migrationBuilder.DropIndex(
                name: "ux_content_media_asset_usages_reference",
                table: "content_media_asset_usages");

            migrationBuilder.CreateIndex(
                name: "ux_content_media_asset_usages_reference_asset",
                table: "content_media_asset_usages",
                columns: new[] { "reference_type", "reference_id", "asset_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_content_media_asset_usages_reference_asset",
                table: "content_media_asset_usages");

            migrationBuilder.CreateTable(
                name: "content_videos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description_ar = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    description_en = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    poster_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    provider = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Uploaded"),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    youtube_video_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_videos", x => x.id);
                    table.CheckConstraint("ck_content_videos_mime", "mime_type IN ('video/mp4','video/webm','video/youtube')");
                    table.CheckConstraint("ck_content_videos_provider", "provider IN ('Uploaded','YouTube')");
                    table.CheckConstraint("ck_content_videos_size", "file_size_bytes >= 0");
                    table.CheckConstraint("ck_content_videos_status", "status IN ('Draft','Published')");
                });

            migrationBuilder.CreateIndex(
                name: "ux_content_media_asset_usages_reference",
                table: "content_media_asset_usages",
                columns: new[] { "reference_type", "reference_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_videos_status",
                table: "content_videos",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_content_videos_storage_key",
                table: "content_videos",
                column: "storage_key",
                unique: true);
        }
    }
}

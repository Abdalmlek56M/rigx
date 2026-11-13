using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M8_ContentVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_videos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_ar = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    description_en = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    poster_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_videos", x => x.id);
                    table.CheckConstraint("ck_content_videos_mime", "mime_type IN ('video/mp4','video/webm')");
                    table.CheckConstraint("ck_content_videos_size", "file_size_bytes > 0");
                    table.CheckConstraint("ck_content_videos_status", "status IN ('Draft','Published')");
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_videos");
        }
    }
}

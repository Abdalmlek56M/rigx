using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M7_ContentVideoYouTube : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_content_videos_mime",
                table: "content_videos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_content_videos_size",
                table: "content_videos");

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "content_videos",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Uploaded");

            migrationBuilder.AddColumn<string>(
                name: "youtube_video_id",
                table: "content_videos",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_content_videos_mime",
                table: "content_videos",
                sql: "mime_type IN ('video/mp4','video/webm','video/youtube')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_content_videos_provider",
                table: "content_videos",
                sql: "provider IN ('Uploaded','YouTube')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_content_videos_size",
                table: "content_videos",
                sql: "file_size_bytes >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_content_videos_mime",
                table: "content_videos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_content_videos_provider",
                table: "content_videos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_content_videos_size",
                table: "content_videos");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "content_videos");

            migrationBuilder.DropColumn(
                name: "youtube_video_id",
                table: "content_videos");

            migrationBuilder.AddCheckConstraint(
                name: "ck_content_videos_mime",
                table: "content_videos",
                sql: "mime_type IN ('video/mp4','video/webm')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_content_videos_size",
                table: "content_videos",
                sql: "file_size_bytes > 0");
        }
    }
}

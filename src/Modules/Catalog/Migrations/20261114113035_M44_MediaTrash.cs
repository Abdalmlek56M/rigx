using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class M44_MediaTrash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_content_media_assets_active",
                table: "content_media_assets");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at_utc",
                table: "content_media_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "content_media_assets",
                type: "uuid",
                nullable: true);

            // Assets already deleted stay in the trash (their 30 days start from their last change).
            migrationBuilder.Sql("UPDATE content_media_assets SET deleted_at_utc = updated_at_utc WHERE is_deleted;");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "content_media_assets");

            migrationBuilder.CreateIndex(
                name: "ix_content_media_assets_active",
                table: "content_media_assets",
                columns: new[] { "deleted_at_utc", "updated_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_content_media_assets_active",
                table: "content_media_assets");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "content_media_assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE content_media_assets SET is_deleted = true WHERE deleted_at_utc IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "content_media_assets");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "content_media_assets");

            migrationBuilder.CreateIndex(
                name: "ix_content_media_assets_active",
                table: "content_media_assets",
                columns: new[] { "is_deleted", "updated_at_utc" });
        }
    }
}

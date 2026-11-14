using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M42_Trash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_content_articles_slug",
                table: "content_articles");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at_utc",
                table: "content_faqs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "content_faqs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at_utc",
                table: "content_articles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "content_articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_content_articles_slug",
                table: "content_articles",
                column: "slug",
                unique: true,
                filter: "deleted_at_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_content_articles_slug",
                table: "content_articles");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "content_articles");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "content_articles");

            migrationBuilder.CreateIndex(
                name: "ux_content_articles_slug",
                table: "content_articles",
                column: "slug",
                unique: true);
        }
    }
}

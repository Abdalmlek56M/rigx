using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M6_ContentMediaLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    alt_ar = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    alt_en = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    caption_ar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    caption_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tags_csv = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    thumbnail_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    original_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    thumbnail_width = table.Column<int>(type: "integer", nullable: false),
                    thumbnail_height = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    uploaded_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_media_assets", x => x.id);
                    table.CheckConstraint("ck_content_media_assets_dimensions", "width > 0 AND height > 0 AND thumbnail_width > 0 AND thumbnail_height > 0");
                    table.CheckConstraint("ck_content_media_assets_size", "file_size_bytes > 0");
                    table.CheckConstraint("ck_content_media_assets_storage_not_blank", "length(btrim(storage_key)) > 0 AND length(btrim(thumbnail_storage_key)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "content_media_asset_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_media_asset_usages", x => x.id);
                    table.ForeignKey(
                        name: "FK_content_media_asset_usages_content_media_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "content_media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_media_asset_usages_asset",
                table: "content_media_asset_usages",
                columns: new[] { "asset_id", "is_published" });

            migrationBuilder.CreateIndex(
                name: "ux_content_media_asset_usages_reference",
                table: "content_media_asset_usages",
                columns: new[] { "reference_type", "reference_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_media_assets_active",
                table: "content_media_assets",
                columns: new[] { "is_deleted", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_content_media_assets_storage_key",
                table: "content_media_assets",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_media_asset_usages");

            migrationBuilder.DropTable(
                name: "content_media_assets");
        }
    }
}

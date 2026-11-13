using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M7_ContentCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary_ar = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    summary_en = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    body_ar = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    body_en = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    main_image_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    category_slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    tags_csv = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    seo_title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    seo_title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    seo_description_ar = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    seo_description_en = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    canonical_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_articles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_faqs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_ar = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    question_en = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    answer_ar = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    answer_en = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_faqs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body_ar = table.Column<string>(type: "character varying(40000)", maxLength: 40000, nullable: false),
                    body_en = table.Column<string>(type: "character varying(40000)", maxLength: 40000, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "homepage_layouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    layout_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_layouts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_article_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_article_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_content_article_revisions_content_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "content_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_article_revisions_article",
                table: "content_article_revisions",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_articles_status_published",
                table: "content_articles",
                columns: new[] { "status", "published_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_content_articles_slug",
                table: "content_articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_faqs_status_sort",
                table: "content_faqs",
                columns: new[] { "status", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_content_policies_kind_version",
                table: "content_policies",
                columns: new[] { "kind", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_homepage_layouts_status",
                table: "homepage_layouts",
                column: "status",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_article_revisions");

            migrationBuilder.DropTable(
                name: "content_faqs");

            migrationBuilder.DropTable(
                name: "content_policies");

            migrationBuilder.DropTable(
                name: "homepage_layouts");

            migrationBuilder.DropTable(
                name: "content_articles");
        }
    }
}

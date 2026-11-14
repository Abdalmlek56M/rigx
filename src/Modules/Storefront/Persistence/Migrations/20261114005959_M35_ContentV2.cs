using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M35_ContentV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_video_references");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "content_faqs",
                newName: "topic");

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "content_faqs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "excluded_category_ids",
                table: "content_faqs",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<List<string>>(
                name: "excluded_item_codes",
                table: "content_faqs",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<bool>(
                name: "help_page_only",
                table: "content_faqs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "item_code",
                table: "content_faqs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "content_faqs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.AddColumn<string>(
                name: "youtube_id",
                table: "content_faqs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "blocks_ar",
                table: "content_articles",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "blocks_en",
                table: "content_articles",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            foreach (var language in new[] { "ar", "en" })
            {
                // Old plain-text bodies become paragraph blocks: one block per non-empty chunk between blank lines.
                migrationBuilder.Sql($@"
UPDATE content_articles article SET blocks_{language} = COALESCE((
    SELECT jsonb_agg(jsonb_build_object('type', 'paragraph',
        'spans', jsonb_build_array(jsonb_build_object('text', btrim(chunk, E' \t\r\n')))) ORDER BY ordinal)
    FROM regexp_split_to_table(replace(article.body_{language}, E'\r\n', E'\n'), E'\n[ \t]*\n')
        WITH ORDINALITY AS part(chunk, ordinal)
    WHERE btrim(chunk, E' \t\r\n') <> ''), '[]'::jsonb);");
            }

            migrationBuilder.DropColumn(
                name: "body_ar",
                table: "content_articles");

            migrationBuilder.DropColumn(
                name: "body_en",
                table: "content_articles");

            migrationBuilder.AddColumn<long>(
                name: "view_count",
                table: "content_articles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_content_faqs_scope_target",
                table: "content_faqs",
                columns: new[] { "scope", "category_id", "item_code" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_content_faqs_scope",
                table: "content_faqs",
                sql: "scope IN ('General','Category','Product')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_content_faqs_scope_target",
                table: "content_faqs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_content_faqs_scope",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "excluded_category_ids",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "excluded_item_codes",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "help_page_only",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "item_code",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "youtube_id",
                table: "content_faqs");

            migrationBuilder.DropColumn(
                name: "blocks_ar",
                table: "content_articles");

            migrationBuilder.DropColumn(
                name: "blocks_en",
                table: "content_articles");

            migrationBuilder.DropColumn(
                name: "view_count",
                table: "content_articles");

            migrationBuilder.RenameColumn(
                name: "topic",
                table: "content_faqs",
                newName: "category");

            migrationBuilder.AddColumn<string>(
                name: "body_ar",
                table: "content_articles",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "body_en",
                table: "content_articles",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "article_video_references",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_video_references", x => new { x.article_id, x.video_id });
                    table.ForeignKey(
                        name: "FK_article_video_references_content_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "content_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_video_references_video",
                table: "article_video_references",
                column: "video_id");
        }
    }
}

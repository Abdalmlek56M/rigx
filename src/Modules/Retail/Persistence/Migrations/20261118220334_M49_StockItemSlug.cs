using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M49_StockItemSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "stock_items",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            // Backfill with the StockItemSlug rule (oldest item keeps the plain slug, later ones get -2, -3, ...).
            migrationBuilder.Sql("""
                DO $m49$
                DECLARE
                    row record;
                    base text;
                    candidate text;
                    suffix int;
                BEGIN
                    FOR row IN SELECT id, model_name FROM stock_items ORDER BY created_at_utc, id LOOP
                        base := trim(both '-' from regexp_replace(lower(row.model_name), '[^a-z0-9]+', '-', 'g'));
                        base := trim(both '-' from left(base, 160));
                        IF base = '' THEN base := 'item'; END IF;
                        candidate := base;
                        suffix := 1;
                        WHILE EXISTS (SELECT 1 FROM stock_items WHERE slug = candidate) LOOP
                            suffix := suffix + 1;
                            candidate := base || '-' || suffix;
                        END LOOP;
                        UPDATE stock_items SET slug = candidate WHERE id = row.id;
                    END LOOP;
                END
                $m49$;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_stock_items_slug",
                table: "stock_items",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_stock_items_slug",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "stock_items");
        }
    }
}

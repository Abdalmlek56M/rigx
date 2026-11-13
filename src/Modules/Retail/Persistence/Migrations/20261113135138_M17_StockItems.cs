using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M17_StockItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SEQUENCE stock_item_code_seq;");
            migrationBuilder.Sql("CREATE SEQUENCE assembly_code_seq;");

            migrationBuilder.CreateTable(
                name: "stock_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    condition = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    model_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    with_board_extra = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    alone_extra = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    warranty_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    warranty_months = table.Column<int>(type: "integer", nullable: true),
                    has_box = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    internal_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    specs = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    photos = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    photo_count = table.Column<int>(type: "integer", nullable: false, computedColumnSql: "jsonb_array_length(photos)", stored: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_items", x => x.id);
                    table.CheckConstraint("ck_stock_items_condition", "condition IN ('New','Used')");
                    table.CheckConstraint("ck_stock_items_price", "price >= 0");
                });


            migrationBuilder.AddColumn<Guid>(name: "stock_item_id", table: "order_items", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "stock_item_id", table: "inventory_units", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<string>(name: "code", table: "assemblies", type: "character varying(16)", maxLength: 16, nullable: true);

            // One item per receipt line of new units (same receipt, product and name); every other unit is its own item.
            migrationBuilder.Sql(@"
                CREATE TEMP TABLE _unit_groups AS
                SELECT id AS unit_id,
                       CASE WHEN condition = 'New'
                            THEN 'lot:' || coalesce(receipt_id::text, '') || ':' || coalesce(product_id::text, '') || ':' || name
                            ELSE 'unit:' || id::text END AS group_key
                FROM inventory_units;

                CREATE TEMP TABLE _stock_groups AS
                SELECT d.*, gen_random_uuid() AS item_id,
                       row_number() OVER (ORDER BY d.received_at_utc, d.group_key) AS rn
                FROM (
                    SELECT DISTINCT ON (g.group_key)
                           g.group_key, u.product_id, coalesce(nullif(u.model, ''), u.name) AS model_name,
                           u.standalone_price, u.warranty_note, u.condition, u.received_at_utc, u.created_at_utc
                    FROM _unit_groups g JOIN inventory_units u ON u.id = g.unit_id
                    ORDER BY g.group_key, u.received_at_utc, u.id
                ) d;

                INSERT INTO stock_items (id, code, condition, product_id, model_name, model_key, price, warranty_label,
                                         has_box, is_hidden, specs, photos, created_at_utc, updated_at_utc, row_version)
                SELECT item_id,
                       'RX-' || CASE WHEN rn < 10000 THEN lpad(rn::text, 4, '0') ELSE rn::text END,
                       CASE WHEN condition = 'New' THEN 'New' ELSE 'Used' END,
                       product_id, left(model_name, 160),
                       lower(regexp_replace(left(model_name, 160), '[^[:alnum:]]', '', 'g')),
                       coalesce(standalone_price, 0), left(warranty_note, 64),
                       false, false, '{}'::jsonb, '[]'::jsonb, created_at_utc, created_at_utc, 1
                FROM _stock_groups;

                UPDATE inventory_units u SET stock_item_id = s.item_id
                FROM _unit_groups g JOIN _stock_groups s ON s.group_key = g.group_key
                WHERE g.unit_id = u.id;

                SELECT setval('stock_item_code_seq', coalesce((SELECT max(rn) FROM _stock_groups), 1),
                              (SELECT count(*) > 0 FROM _stock_groups));

                DROP TABLE _unit_groups;
                DROP TABLE _stock_groups;

                UPDATE assemblies a SET code = 'BLD-' || CASE WHEN r.rn < 100 THEN lpad(r.rn::text, 2, '0') ELSE r.rn::text END
                FROM (SELECT id, row_number() OVER (ORDER BY created_at_utc, id) AS rn FROM assemblies) r
                WHERE r.id = a.id;

                SELECT setval('assembly_code_seq', greatest((SELECT count(*) FROM assemblies), 1),
                              (SELECT count(*) > 0 FROM assemblies));
            ");

            migrationBuilder.AlterColumn<Guid>(name: "stock_item_id", table: "inventory_units", type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "code", table: "assemblies", type: "character varying(16)", maxLength: 16, nullable: false,
                oldClrType: typeof(string), oldType: "character varying(16)", oldMaxLength: 16, oldNullable: true);

            migrationBuilder.CreateIndex(name: "ix_inventory_units_stock_item", table: "inventory_units", column: "stock_item_id");
            migrationBuilder.CreateIndex(name: "ux_assemblies_code", table: "assemblies", column: "code", unique: true);
            migrationBuilder.CreateIndex(name: "ix_stock_items_listing", table: "stock_items", columns: new[] { "is_hidden", "condition", "category_id" });
            migrationBuilder.CreateIndex(name: "ix_stock_items_model_key", table: "stock_items", column: "model_key");
            migrationBuilder.CreateIndex(name: "ux_stock_items_code", table: "stock_items", column: "code", unique: true);
            migrationBuilder.AddForeignKey(name: "FK_inventory_units_stock_items_stock_item_id", table: "inventory_units",
                column: "stock_item_id", principalTable: "stock_items", principalColumn: "id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(name: "brand", table: "inventory_units");
            migrationBuilder.DropColumn(name: "category", table: "inventory_units");
            migrationBuilder.DropColumn(name: "description", table: "inventory_units");
            migrationBuilder.DropColumn(name: "external_code", table: "inventory_units");
            migrationBuilder.DropColumn(name: "image_storage_keys_json", table: "inventory_units");
            migrationBuilder.DropColumn(name: "model", table: "inventory_units");
            migrationBuilder.DropColumn(name: "sku", table: "inventory_units");
            migrationBuilder.DropColumn(name: "specifications_json", table: "inventory_units");
            migrationBuilder.DropColumn(name: "standalone_price", table: "inventory_units");
            migrationBuilder.DropColumn(name: "variant_id", table: "inventory_units");
            migrationBuilder.DropColumn(name: "warranty_note", table: "inventory_units");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_units_stock_items_stock_item_id",
                table: "inventory_units");

            migrationBuilder.DropTable(
                name: "stock_items");

            migrationBuilder.DropIndex(
                name: "ix_inventory_units_stock_item",
                table: "inventory_units");

            migrationBuilder.DropIndex(
                name: "ux_assemblies_code",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "stock_item_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "stock_item_id",
                table: "inventory_units");

            migrationBuilder.DropColumn(
                name: "code",
                table: "assemblies");

            migrationBuilder.Sql("DROP SEQUENCE stock_item_code_seq;");
            migrationBuilder.Sql("DROP SEQUENCE assembly_code_seq;");

            migrationBuilder.AddColumn<string>(
                name: "brand",
                table: "inventory_units",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "inventory_units",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "inventory_units",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_code",
                table: "inventory_units",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_storage_keys_json",
                table: "inventory_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "inventory_units",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sku",
                table: "inventory_units",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specifications_json",
                table: "inventory_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "standalone_price",
                table: "inventory_units",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "inventory_units",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "warranty_note",
                table: "inventory_units",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}

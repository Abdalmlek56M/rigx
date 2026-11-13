using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Retail.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M23_AssemblyCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "part_price",
                table: "assembly_units",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "warranty_label",
                table: "assembly_units",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "warranty_months",
                table: "assembly_units",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "copies",
                table: "assemblies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "photos",
                table: "assemblies",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "sell_mode",
                table: "assemblies",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AddColumn<string>(
                name: "suitable_for_ar",
                table: "assemblies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suitable_for_en",
                table: "assemblies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usage",
                table: "assemblies",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "warranty_label",
                table: "assemblies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "warranty_months",
                table: "assemblies",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE assembly_units AS link SET part_price = item.price
                FROM inventory_units AS unit JOIN stock_items AS item ON item.id = unit.stock_item_id
                WHERE unit.id = link."UnitId";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_assembly_units_part_price",
                table: "assembly_units",
                sql: "part_price >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_assemblies_listing",
                table: "assemblies",
                columns: new[] { "status", "usage" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_assemblies_copies",
                table: "assemblies",
                sql: "copies >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_assembly_units_part_price",
                table: "assembly_units");

            migrationBuilder.DropIndex(
                name: "ix_assemblies_listing",
                table: "assemblies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_assemblies_copies",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "part_price",
                table: "assembly_units");

            migrationBuilder.DropColumn(
                name: "warranty_label",
                table: "assembly_units");

            migrationBuilder.DropColumn(
                name: "warranty_months",
                table: "assembly_units");

            migrationBuilder.DropColumn(
                name: "copies",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "photos",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "sell_mode",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "suitable_for_ar",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "suitable_for_en",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "usage",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "warranty_label",
                table: "assemblies");

            migrationBuilder.DropColumn(
                name: "warranty_months",
                table: "assemblies");
        }
    }
}

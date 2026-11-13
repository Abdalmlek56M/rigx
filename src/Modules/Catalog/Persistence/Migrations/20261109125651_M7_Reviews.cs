using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M7_Reviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    show_avatar = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    moderation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    moderated_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    moderated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_reviews", x => x.id);
                    table.CheckConstraint("ck_product_reviews_body", "length(btrim(body)) BETWEEN 10 AND 4000");
                    table.CheckConstraint("ck_product_reviews_rating", "rating BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_product_reviews_status", "status IN ('Pending','Published','Rejected','Hidden')");
                    table.ForeignKey(
                        name: "FK_product_reviews_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    show_avatar = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    moderation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    moderated_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    moderated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_reviews", x => x.id);
                    table.CheckConstraint("ck_store_reviews_body", "length(btrim(body)) BETWEEN 10 AND 4000");
                    table.CheckConstraint("ck_store_reviews_rating", "rating BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_store_reviews_status", "status IN ('Pending','Published','Rejected','Hidden')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_product_status",
                table: "product_reviews",
                columns: new[] { "product_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_status_updated",
                table: "product_reviews",
                columns: new[] { "status", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_product_reviews_user_product",
                table: "product_reviews",
                columns: new[] { "user_account_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_store_reviews_status_updated",
                table: "store_reviews",
                columns: new[] { "status", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_store_reviews_user",
                table: "store_reviews",
                column: "user_account_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_reviews");

            migrationBuilder.DropTable(
                name: "store_reviews");
        }
    }
}

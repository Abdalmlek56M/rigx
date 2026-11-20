using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M54_FavouritesAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wishlist_items");

            migrationBuilder.CreateTable(
                name: "customer_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    target = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_notifications", x => x.id);
                    table.CheckConstraint("ck_customer_notifications_kind", "kind IN ('OrderStatus','BackInStock')");
                });

            migrationBuilder.CreateTable(
                name: "favourites",
                columns: table => new
                {
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favourites", x => new { x.user_account_id, x.item_code });
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_notifications_account_created",
                table: "customer_notifications",
                columns: new[] { "user_account_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_notifications");

            migrationBuilder.DropTable(
                name: "favourites");

            migrationBuilder.CreateTable(
                name: "wishlist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_items_account",
                table: "wishlist_items",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_wishlist_items_account_product",
                table: "wishlist_items",
                columns: new[] { "user_account_id", "product_id" },
                unique: true);
        }
    }
}

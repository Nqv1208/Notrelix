using Microsoft.EntityFrameworkCore.Migrations;
using Notrelix.Infrastructure.Data.WorkManagement;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillItemKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing Items get keys in creation order and boards their sequence
            // (idempotent), before the uniqueness guard is created.
            migrationBuilder.Sql(ItemKeyMigrationSql.Backfill);

            migrationBuilder.CreateIndex(
                name: "ux_work_board_items_board_item_key",
                schema: "work",
                table: "board_items",
                columns: new[] { "board_id", "item_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_work_board_items_board_item_key",
                schema: "work",
                table: "board_items");
        }
    }
}

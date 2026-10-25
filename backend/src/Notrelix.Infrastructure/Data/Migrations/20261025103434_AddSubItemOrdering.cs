#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubItemOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "archived_with_parent",
                schema: "work",
                table: "board_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ux_work_board_items_parent_position_active",
                schema: "work",
                table: "board_items",
                columns: new[] { "parent_item_id", "position" },
                unique: true,
                filter: "deleted_at IS NULL AND parent_item_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_work_board_items_parent_position_active",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "archived_with_parent",
                schema: "work",
                table: "board_items");
        }
    }
}

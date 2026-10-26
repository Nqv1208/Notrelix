#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemLifecycleAuthorship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                schema: "work",
                table: "board_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by",
                schema: "work",
                table: "board_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "completed_by",
                schema: "work",
                table: "board_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "status_before_completion_option_id",
                schema: "work",
                table: "board_items",
                type: "uuid",
                nullable: true);

            // An archived item accepts no further change, so its last update is the archive.
            migrationBuilder.Sql(
                "UPDATE work.board_items SET archived_at = updated_at, archived_by = updated_by WHERE is_archived AND archived_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archived_at",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "archived_by",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "completed_by",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "status_before_completion_option_id",
                schema: "work",
                table: "board_items");
        }
    }
}

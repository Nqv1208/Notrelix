#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceWorkItemPlacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workspace_work_item_placements",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    source_revision = table.Column<long>(type: "bigint", nullable: false),
                    last_occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_work_item_placements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_workspace_work_item_placements_workspace_item",
                schema: "reporting",
                table: "workspace_work_item_placements",
                columns: new[] { "workspace_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_resource_permissions_active_subject",
                schema: "governance",
                table: "resource_permissions",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "subject_type", "subject_id" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_resource_permissions_active_subject",
                schema: "governance",
                table: "resource_permissions");

            migrationBuilder.DropTable(
                name: "workspace_work_item_placements",
                schema: "reporting");
        }
    }
}

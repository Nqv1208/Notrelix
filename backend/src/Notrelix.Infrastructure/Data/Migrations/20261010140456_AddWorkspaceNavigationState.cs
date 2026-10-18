#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceNavigationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pinned_resources",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_kind = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pinned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pinned_resources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recent_resources",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_kind = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recent_resources", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workspace_pinned_resources_member_pinned",
                schema: "workspace",
                table: "pinned_resources",
                columns: new[] { "workspace_id", "user_id", "pinned_at" });

            migrationBuilder.CreateIndex(
                name: "ux_workspace_pinned_resources_member_resource",
                schema: "workspace",
                table: "pinned_resources",
                columns: new[] { "workspace_id", "user_id", "resource_kind", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workspace_recent_resources_member_last_opened",
                schema: "workspace",
                table: "recent_resources",
                columns: new[] { "workspace_id", "user_id", "last_opened_at" });

            migrationBuilder.CreateIndex(
                name: "ux_workspace_recent_resources_member_resource",
                schema: "workspace",
                table: "recent_resources",
                columns: new[] { "workspace_id", "user_id", "resource_kind", "resource_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pinned_resources",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "recent_resources",
                schema: "workspace");
        }
    }
}

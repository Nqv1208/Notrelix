#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_spaces_workspace_name_live",
                schema: "chat",
                table: "spaces");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "hidden_at",
                schema: "chat",
                table: "memberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "blocks",
                schema: "chat",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocked_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blocks", x => new { x.workspace_id, x.blocker_id, x.blocked_id });
                });

            migrationBuilder.CreateTable(
                name: "direct_pairs",
                schema: "chat",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_low = table.Column<Guid>(type: "uuid", nullable: false),
                    user_high = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_direct_pairs", x => new { x.workspace_id, x.user_low, x.user_high });
                    table.ForeignKey(
                        name: "fk_direct_pairs_chat_spaces_space_id",
                        column: x => x.space_id,
                        principalSchema: "chat",
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_spaces_workspace_name_live",
                schema: "chat",
                table: "spaces",
                columns: new[] { "workspace_id", "name" },
                unique: true,
                filter: "archived_at IS NULL AND deleted_at IS NULL AND kind IN ('channel_public', 'channel_private')");

            migrationBuilder.CreateIndex(
                name: "ix_blocks_workspace_blocked",
                schema: "chat",
                table: "blocks",
                columns: new[] { "workspace_id", "blocked_id" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_pairs_space_id",
                schema: "chat",
                table: "direct_pairs",
                column: "space_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocks",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "direct_pairs",
                schema: "chat");

            migrationBuilder.DropIndex(
                name: "ux_spaces_workspace_name_live",
                schema: "chat",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "hidden_at",
                schema: "chat",
                table: "memberships");

            migrationBuilder.CreateIndex(
                name: "ux_spaces_workspace_name_live",
                schema: "chat",
                table: "spaces",
                columns: new[] { "workspace_id", "name" },
                unique: true,
                filter: "archived_at IS NULL AND deleted_at IS NULL");
        }
    }
}

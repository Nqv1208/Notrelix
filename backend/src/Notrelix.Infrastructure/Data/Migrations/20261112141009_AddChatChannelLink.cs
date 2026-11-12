
#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatChannelLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "link_id",
                schema: "chat",
                table: "spaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "link_kind",
                schema: "chat",
                table: "spaces",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_spaces_workspace_link",
                schema: "chat",
                table: "spaces",
                columns: new[] { "workspace_id", "link_kind", "link_id" },
                filter: "link_id IS NOT NULL AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_spaces_workspace_link",
                schema: "chat",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "link_id",
                schema: "chat",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "link_kind",
                schema: "chat",
                table: "spaces");
        }
    }
}

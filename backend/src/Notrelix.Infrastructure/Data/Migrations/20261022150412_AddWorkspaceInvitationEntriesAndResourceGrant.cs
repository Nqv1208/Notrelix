#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceInvitationEntriesAndResourceGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "initial_entries",
                schema: "workspace",
                table: "workspace_invitations",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<Guid>(
                name: "resource_id",
                schema: "workspace",
                table: "workspace_invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resource_kind",
                schema: "workspace",
                table: "workspace_invitations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resource_level",
                schema: "workspace",
                table: "workspace_invitations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_workspace_invitations_resource_grant",
                schema: "workspace",
                table: "workspace_invitations",
                sql: "(resource_kind IS NULL AND resource_id IS NULL AND resource_level IS NULL) OR (resource_kind IN ('work-management.board', 'documents.page') AND resource_id IS NOT NULL AND resource_level IN ('Viewer', 'Commenter', 'Editor') AND role = 'Guest')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_workspace_invitations_resource_grant",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "initial_entries",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "resource_id",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "resource_kind",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "resource_level",
                schema: "workspace",
                table: "workspace_invitations");
        }
    }
}

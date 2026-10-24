#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceSecurityExtSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "settings_allow_guest_invites",
                schema: "workspace",
                table: "workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "settings_default_member_role",
                schema: "workspace",
                table: "workspaces",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Member");

            migrationBuilder.AddColumn<int>(
                name: "settings_guest_default_expiry_days",
                schema: "workspace",
                table: "workspaces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "settings_invitation_expiry_days",
                schema: "workspace",
                table: "workspaces",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "settings_public_sharing_changed_at",
                schema: "workspace",
                table: "workspaces",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "settings_allow_guest_invites",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "settings_default_member_role",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "settings_guest_default_expiry_days",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "settings_invitation_expiry_days",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "settings_public_sharing_changed_at",
                schema: "workspace",
                table: "workspaces");
        }
    }
}

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberJobTitleAndAccountPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "job_title",
                schema: "workspace",
                table: "workspace_members",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "phone_visible_to_workspace_members",
                schema: "identity",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "job_title",
                schema: "workspace",
                table: "workspace_members");

            migrationBuilder.DropColumn(
                name: "phone",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "phone_visible_to_workspace_members",
                schema: "identity",
                table: "user_profiles");
        }
    }
}

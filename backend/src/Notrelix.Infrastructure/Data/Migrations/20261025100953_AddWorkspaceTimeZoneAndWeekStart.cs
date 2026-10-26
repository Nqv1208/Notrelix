#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceTimeZoneAndWeekStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "settings_time_zone",
                schema: "workspace",
                table: "workspaces",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "settings_week_start",
                schema: "workspace",
                table: "workspaces",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "settings_time_zone",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "settings_week_start",
                schema: "workspace",
                table: "workspaces");
        }
    }
}

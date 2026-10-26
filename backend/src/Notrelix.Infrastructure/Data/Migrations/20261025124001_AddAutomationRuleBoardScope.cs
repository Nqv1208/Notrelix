#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationRuleBoardScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "board_id",
                schema: "automation",
                table: "automation_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_automation_rules_workspace_board",
                schema: "automation",
                table: "automation_rules",
                columns: new[] { "workspace_id", "board_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_automation_rules_workspace_board",
                schema: "automation",
                table: "automation_rules");

            migrationBuilder.DropColumn(
                name: "board_id",
                schema: "automation",
                table: "automation_rules");
        }
    }
}

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationExecutionRuleVersionSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "configuration_snapshot",
                schema: "automation",
                table: "automation_executions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "rule_version",
                schema: "automation",
                table: "automation_executions",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "configuration_snapshot",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropColumn(
                name: "rule_version",
                schema: "automation",
                table: "automation_executions");
        }
    }
}

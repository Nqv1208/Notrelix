
#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AutomationExecutionTriggerUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_automation_executions_rule_trigger",
                schema: "automation",
                table: "automation_executions",
                columns: new[] { "rule_id", "trigger_id" },
                unique: true,
                filter: "trigger_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_automation_executions_rule_trigger",
                schema: "automation",
                table: "automation_executions");
        }
    }
}

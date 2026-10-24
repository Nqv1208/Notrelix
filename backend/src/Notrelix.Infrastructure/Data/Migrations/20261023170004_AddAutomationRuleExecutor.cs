#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationRuleExecutor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "disabled_reason",
                schema: "automation",
                table: "automation_rules",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "executor_user_id",
                schema: "automation",
                table: "automation_rules",
                type: "uuid",
                nullable: true);

            // AUT-020 backfill: an existing rule runs as the user recorded as its
            // creator. A rule with no creator on record keeps a null executor and is
            // not executable (fail closed): it is disabled at its first evaluation.
            migrationBuilder.Sql(
                @"UPDATE automation.automation_rules
                  SET executor_user_id = created_by
                  WHERE executor_user_id IS NULL AND created_by IS NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "idx_automation_rules_workspace_executor",
                schema: "automation",
                table: "automation_rules",
                columns: new[] { "workspace_id", "executor_user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_automation_rules_workspace_executor",
                schema: "automation",
                table: "automation_rules");

            migrationBuilder.DropColumn(
                name: "disabled_reason",
                schema: "automation",
                table: "automation_rules");

            migrationBuilder.DropColumn(
                name: "executor_user_id",
                schema: "automation",
                table: "automation_rules");
        }
    }
}

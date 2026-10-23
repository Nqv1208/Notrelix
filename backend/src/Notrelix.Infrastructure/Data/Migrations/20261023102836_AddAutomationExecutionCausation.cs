#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationExecutionCausation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "causation_execution_id",
                schema: "automation",
                table: "automation_executions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "chain_depth",
                schema: "automation",
                table: "automation_executions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "causation_execution_id",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropColumn(
                name: "chain_depth",
                schema: "automation",
                table: "automation_executions");
        }
    }
}

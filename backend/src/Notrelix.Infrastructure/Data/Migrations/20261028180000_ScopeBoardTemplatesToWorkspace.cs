#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// A board template is workspace-scoped creation input, so every row carries its account and a workspace.
    /// Rows saved before this migration carried no tenant, so the backfill takes the account from the
    /// workspace and drops a row with no workspace. The model snapshot carries the new shape.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261028180000_ScopeBoardTemplatesToWorkspace")]
    public partial class ScopeBoardTemplatesToWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                schema: "work",
                table: "board_templates",
                type: "uuid",
                nullable: true);
            migrationBuilder.Sql(
                """
                UPDATE work.board_templates t SET account_id = w.account_id
                FROM workspace.workspaces w WHERE w.id = t.workspace_id;
                DELETE FROM work.board_templates WHERE account_id IS NULL OR workspace_id IS NULL;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "account_id", schema: "work", table: "board_templates", type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "workspace_id", schema: "work", table: "board_templates", type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "workspace_id", schema: "work", table: "board_templates", type: "uuid", nullable: true,
                oldClrType: typeof(Guid), oldType: "uuid");
            migrationBuilder.DropColumn(name: "account_id", schema: "work", table: "board_templates");
        }
    }
}

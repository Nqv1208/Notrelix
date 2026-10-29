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
            // Plain SET NOT NULL: AlterColumn also emits ALTER COLUMN ... TYPE, which Postgres refuses on a column
            // the table's RLS policies use (workspace_id), so an existing database could never apply this migration.
            migrationBuilder.Sql(
                """
                ALTER TABLE work.board_templates ALTER COLUMN account_id SET NOT NULL;
                ALTER TABLE work.board_templates ALTER COLUMN workspace_id SET NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE work.board_templates ALTER COLUMN workspace_id DROP NOT NULL;");
            migrationBuilder.DropColumn(name: "account_id", schema: "work", table: "board_templates");
        }
    }
}

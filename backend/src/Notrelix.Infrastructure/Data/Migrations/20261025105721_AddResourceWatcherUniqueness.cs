#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceWatcherUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A person follows a resource once; any earlier duplicate keeps its oldest row.
            migrationBuilder.Sql(
                """
                DELETE FROM collab.resource_watchers w
                USING collab.resource_watchers keep
                WHERE w.user_id = keep.user_id AND w.target_type = keep.target_type AND w.target_id = keep.target_id
                  AND (w.created_at, w.id) > (keep.created_at, keep.id);
                """);
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_resource_watchers_user_target ON collab.resource_watchers (user_id, target_type, target_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS collab.ux_resource_watchers_user_target;");
        }
    }
}

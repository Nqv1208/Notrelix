#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageCountAndSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Built CONCURRENTLY (outside the migration transaction) so chat sends are not blocked while chat.messages is indexed.
            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_messages_content_trgm " +
                "ON chat.messages USING gin (content gin_trgm_ops) " +
                "WHERE kind = 'User' AND deleted_at IS NULL;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_messages_unread_count " +
                "ON chat.messages (space_id, sequence DESC, author_id) INCLUDE (account_id, workspace_id) " +
                "WHERE kind = 'User' AND deleted_at IS NULL;",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS chat.ix_messages_content_trgm;", suppressTransaction: true);
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS chat.ix_messages_unread_count;", suppressTransaction: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageCountAndSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_messages_content_trgm",
                schema: "chat",
                table: "messages",
                column: "content",
                filter: "kind = 'User' AND deleted_at IS NULL")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_unread_count",
                schema: "chat",
                table: "messages",
                columns: new[] { "space_id", "sequence", "author_id" },
                descending: new[] { false, true, false },
                filter: "kind = 'User' AND deleted_at IS NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "account_id", "workspace_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_content_trgm",
                schema: "chat",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "ix_messages_unread_count",
                schema: "chat",
                table: "messages");
        }
    }
}

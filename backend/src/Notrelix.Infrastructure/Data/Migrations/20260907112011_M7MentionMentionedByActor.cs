#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class M7MentionMentionedByActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // M2F: mention creation now captures the trusted mentioner actor.
            // The mention pipeline produced no rows before this change (no
            // producer existed), so the column adds NOT NULL without a
            // persistent default — the Domain aggregate already guards
            // non-empty actors, and the database never fabricates a sentinel.
            migrationBuilder.AddColumn<Guid>(
                name: "mentioned_by_user_id",
                schema: "collab",
                table: "mentions",
                type: "uuid",
                nullable: false);

            migrationBuilder.Sql(
                """
                ALTER TABLE collab.mentions
                    ADD CONSTRAINT ck_mentions_mentioned_by_user_id_present
                    CHECK (mentioned_by_user_id <> '00000000-0000-0000-0000-000000000000');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE collab.mentions
                    DROP CONSTRAINT IF EXISTS ck_mentions_mentioned_by_user_id_present;
                """);

            migrationBuilder.DropColumn(
                name: "mentioned_by_user_id",
                schema: "collab",
                table: "mentions");
        }
    }
}

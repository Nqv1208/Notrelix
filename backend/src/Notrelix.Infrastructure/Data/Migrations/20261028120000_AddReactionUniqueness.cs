#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// A person holds one reaction per emoji and target. The index is raw SQL like the other
    /// collab uniqueness guards, so the EF model does not change and this migration carries no target model.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261028120000_AddReactionUniqueness")]
    public partial class AddReactionUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier duplicates keep their oldest row.
            migrationBuilder.Sql(
                """
                DELETE FROM collab.reactions r
                USING collab.reactions keep
                WHERE r.user_id = keep.user_id AND r.resource_type = keep.resource_type
                  AND r.resource_id = keep.resource_id AND r.emoji = keep.emoji
                  AND (r.created_at, r.id) > (keep.created_at, keep.id);
                """);
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_reactions_user_target_emoji ON collab.reactions (user_id, resource_type, resource_id, emoji);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS collab.ux_reactions_user_target_emoji;");
        }
    }
}

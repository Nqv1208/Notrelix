
#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillChatMentionCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data only: the read boundary (read_sequence) and the mention_count column already exist, and no index is added (the send and the
            // summary read memberships by their primary key and by idx_memberships_user_workspace_active). Until now mention_count was always 0;
            // from now on it is the stored count of unread mentions, so each active member starts from what the old per-read count would have said
            // and nobody sees a mention appear or vanish. The kind spellings are matched without case or dashes: rows hold the enum names today.
            migrationBuilder.Sql(@"
UPDATE chat.memberships AS m
SET mention_count = c.n
FROM (
    SELECT m2.space_id, m2.user_id, COUNT(DISTINCT msg.id)::int AS n
    FROM chat.memberships AS m2
    JOIN chat.messages AS msg ON msg.space_id = m2.space_id
    JOIN chat.message_mentions AS mm ON mm.message_id = msg.id AND mm.space_id = msg.space_id
    WHERE m2.left_at IS NULL
      AND msg.sequence > m2.read_sequence
      AND msg.created_at >= m2.joined_at
      AND msg.author_id <> m2.user_id
      AND lower(msg.kind) = 'user'
      AND msg.deleted_at IS NULL
      AND ((lower(mm.kind) = 'user' AND mm.target_id = m2.user_id)
           OR replace(lower(mm.kind), '-', '') IN ('broadcastchannel', 'broadcasthere'))
    GROUP BY m2.space_id, m2.user_id
) AS c
WHERE m.space_id = c.space_id AND m.user_id = c.user_id AND m.mention_count IS DISTINCT FROM c.n;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the stored counts stay and the column keeps its type.
        }
    }
}

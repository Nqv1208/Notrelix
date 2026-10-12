namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// Existing-data step of migration <c>BackfillItemKeys</c>: every Item without a
/// key gets the next number of its Board's sequence, in creation order
/// (<c>created_at</c>, then <c>id</c>), as <c>{prefix}-{n}</c> or plain <c>n</c>
/// when the Board has no prefix; numbering continues after the highest sequence
/// already used on the Board. Deleted and archived Items are numbered too, so a key
/// is never reused. Each Board's sequence is then raised to the highest number
/// used. Only Items still without a key are written, so a re-run changes nothing.
/// Migration history is append-only: a later rule change ships as a new migration.
/// </summary>
public static class ItemKeyMigrationSql
{
    public const string Backfill = """
        WITH starts AS (
            SELECT b.id AS board_id,
                   b.item_key_prefix AS prefix,
                   GREATEST(b.item_sequence,
                            COALESCE((SELECT max(i.item_sequence) FROM work.board_items i WHERE i.board_id = b.id), 0)) AS start
              FROM work.boards b
        ),
        numbered AS (
            SELECT i.id,
                   s.prefix,
                   s.start + row_number() OVER (PARTITION BY i.board_id ORDER BY i.created_at, i.id) AS seq
              FROM work.board_items i
              JOIN starts s ON s.board_id = i.board_id
             WHERE i.item_key IS NULL
        )
        UPDATE work.board_items i
           SET item_sequence = n.seq,
               item_key = CASE WHEN n.prefix IS NULL OR btrim(n.prefix) = '' THEN n.seq::text
                               ELSE n.prefix || '-' || n.seq::text END
          FROM numbered n
         WHERE i.id = n.id;

        UPDATE work.boards b
           SET item_sequence = m.max_sequence
          FROM (SELECT board_id, max(item_sequence) AS max_sequence
                  FROM work.board_items
                 GROUP BY board_id) m
         WHERE b.id = m.board_id
           AND b.item_sequence < m.max_sequence;
        """;
}

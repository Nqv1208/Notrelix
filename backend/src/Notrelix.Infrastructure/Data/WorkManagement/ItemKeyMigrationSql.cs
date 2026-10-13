namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// Existing-data step of migration <c>BackfillItemKeys</c>: every Item without a
/// key gets the next number of its Board's sequence, in creation order
/// (<c>created_at</c>, then <c>id</c>), as <c>{prefix}-{n}</c> or plain <c>n</c>
/// when the Board has no prefix; numbering continues after the highest sequence
/// already used on the Board. Deleted and archived Items are numbered too, so a key
/// is never reused. Each Board's sequence is then raised to the highest number
/// used. Only Items still without a key are written, so a re-run changes nothing.
/// Migration history is append-only once released: a later rule change ships as a
/// new migration.
/// <para>
/// Migration identity: the statements must see every tenant's rows, so the
/// migration runs as the owner of the <c>work</c> tables without
/// <c>FORCE ROW LEVEL SECURITY</c>, or as a <c>BYPASSRLS</c> role. Under any other
/// identity row-level security would silently hide rows, so the backfill first
/// fails with <see cref="BlockedPrefix"/>; it finally fails with
/// <see cref="IncompletePrefix"/> if any Item still has no key or a Board's
/// sequence is below a number already used.
/// </para>
/// <para>
/// One transaction: the backfill runs inside the migration transaction. It writes
/// two columns per existing Item once; at the current data volume (thousands of
/// Items, not millions) that is a short update, so no batching is needed. Items
/// created without a key by instances still running the previous release during a
/// rolling deploy are not covered; re-running this idempotent statement after the
/// deploy repairs them (see the follow-up in the delivery report).
/// </para>
/// </summary>
public static class ItemKeyMigrationSql
{
    public const string BlockedPrefix = "ITEM_KEY_BACKFILL_BLOCKED";
    public const string IncompletePrefix = "ITEM_KEY_BACKFILL_INCOMPLETE";

    public const string Backfill = $$"""
        DO $$
        BEGIN
            IF row_security_active('work.board_items') OR row_security_active('work.boards') THEN
                RAISE EXCEPTION '{{BlockedPrefix}}: row-level security applies to role %; run migrations as the work table owner without FORCE ROW LEVEL SECURITY or as a BYPASSRLS role', current_user;
            END IF;
        END $$;

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

        DO $$
        DECLARE
            v_keyless bigint;
            v_behind bigint;
        BEGIN
            SELECT count(*) INTO v_keyless FROM work.board_items WHERE item_key IS NULL;
            SELECT count(*) INTO v_behind
              FROM work.boards b
             WHERE b.item_sequence < (SELECT coalesce(max(i.item_sequence), 0) FROM work.board_items i WHERE i.board_id = b.id);
            IF v_keyless > 0 OR v_behind > 0 THEN
                RAISE EXCEPTION '{{IncompletePrefix}}: % item(s) without a key, % board(s) with a sequence below a used number', v_keyless, v_behind;
            END IF;
        END $$;
        """;
}

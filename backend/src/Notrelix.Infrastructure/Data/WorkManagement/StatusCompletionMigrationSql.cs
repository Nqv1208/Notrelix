namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// Existing-data step of migration <c>ReconcileStatusCompletion</c>, after Status
/// options received a category (<see cref="StatusOptionCategoryMigrationSql"/>):
/// <list type="number">
/// <item>every non-deleted Status field with at least two options and no done
/// option gets one: its highest-position option (ordinal, then id) that is not
/// a to-do option becomes done — legacy fields such as "Mở / Đang xử lý / Đóng"
/// had no done name the category backfill recognises;</item>
/// <item>every non-deleted Item whose value on its Board's Status field (the
/// first non-deleted Status field by position, then id — the read path's rule)
/// holds a done option, and that has no completion time, is completed at its last
/// update time (else its creation time), as a status write would have done.</item>
/// </list>
/// Only rows still missing a done option or a completion time are written, so a
/// re-run changes nothing; completion is only ever set here, never cleared.
/// Status fields with fewer than two options cannot hold both a to-do and a done
/// option; they are left as they are and reported with a NOTICE
/// (<see cref="NoDoneOptionPrefix"/>). Items are not versioned by this step.
/// <para>
/// Migration identity: as for the category backfill, the statements must see every
/// tenant's rows, so the step first fails with <see cref="BlockedPrefix"/> when
/// row-level security applies; it finally fails with <see cref="IncompletePrefix"/>
/// if a Status field with at least two options still has no done option.
/// </para>
/// </summary>
public static class StatusCompletionMigrationSql
{
    public const string BlockedPrefix = "STATUS_COMPLETION_BACKFILL_BLOCKED";
    public const string IncompletePrefix = "STATUS_COMPLETION_BACKFILL_INCOMPLETE";
    public const string NoDoneOptionPrefix = "STATUS_COMPLETION_BACKFILL_NO_DONE_OPTION";

    public const string Backfill = $$"""
        DO $$
        BEGIN
            IF row_security_active('work.field_options') OR row_security_active('work.board_fields')
               OR row_security_active('work.board_items') OR row_security_active('work.board_item_values') THEN
                RAISE EXCEPTION '{{BlockedPrefix}}: row-level security applies to role %; run migrations as the work table owner without FORCE ROW LEVEL SECURITY or as a BYPASSRLS role', current_user;
            END IF;
        END $$;

        UPDATE work.field_options o
           SET category = 'Done'
          FROM (
              SELECT DISTINCT ON (p.field_id) p.id
                FROM work.field_options p
                JOIN work.board_fields f ON f.id = p.field_id
               WHERE f.type = 'Status'
                 AND f.deleted_at IS NULL
                 AND p.category IS DISTINCT FROM 'Todo'
                 AND NOT EXISTS (SELECT 1 FROM work.field_options d WHERE d.field_id = p.field_id AND d.category = 'Done')
                 AND (SELECT count(*) FROM work.field_options c WHERE c.field_id = p.field_id) >= 2
               ORDER BY p.field_id, p.position COLLATE "C" DESC, p.id DESC
          ) highest
         WHERE o.id = highest.id;

        WITH status_field AS (
            SELECT DISTINCT ON (f.board_id) f.id, f.board_id
              FROM work.board_fields f
             WHERE f.type = 'Status'
               AND f.deleted_at IS NULL
             ORDER BY f.board_id, f.position COLLATE "C", f.id
        )
        UPDATE work.board_items i
           SET completed_at = COALESCE(i.updated_at, i.created_at)
          FROM status_field sf
          JOIN work.board_item_values v ON v.field_id = sf.id
          JOIN work.field_options o ON o.field_id = sf.id AND o.category = 'Done'
         WHERE i.id = v.item_id
           AND i.board_id = sf.board_id
           AND jsonb_typeof(v.value) = 'string'
           AND o.id::text = lower(v.value #>> '{}')
           AND i.completed_at IS NULL
           AND i.deleted_at IS NULL;

        DO $$
        DECLARE
            v_missing bigint;
            v_small bigint;
        BEGIN
            SELECT count(*) INTO v_missing
              FROM work.board_fields f
             WHERE f.type = 'Status'
               AND f.deleted_at IS NULL
               AND (SELECT count(*) FROM work.field_options c WHERE c.field_id = f.id) >= 2
               AND NOT EXISTS (SELECT 1 FROM work.field_options d WHERE d.field_id = f.id AND d.category = 'Done');
            IF v_missing > 0 THEN
                RAISE EXCEPTION '{{IncompletePrefix}}: % Status field(s) with at least two options still have no done option', v_missing;
            END IF;

            SELECT count(*) INTO v_small
              FROM work.board_fields f
             WHERE f.type = 'Status'
               AND f.deleted_at IS NULL
               AND NOT EXISTS (SELECT 1 FROM work.field_options d WHERE d.field_id = f.id AND d.category = 'Done');
            IF v_small > 0 THEN
                RAISE NOTICE '{{NoDoneOptionPrefix}}: % Status field(s) with fewer than two options have no done option; a board manager must add one', v_small;
            END IF;
        END $$;
        """;
}

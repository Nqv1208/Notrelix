namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// Existing-data step of migration <c>BackfillDefaultBoardGroups</c>: a Board that has no live
/// group (created before a new Board started with its default groups) gets one group per option
/// of its system Status field, in option order, with the option's name and colour; a Board whose
/// Status field has no option gets a single "Tasks" group. A Board that already has a live group
/// is left alone, so a re-run changes nothing. Positions are single-digit fractional keys
/// (<c>a0</c>..<c>az</c>), enough for the 62 options the first groups are taken from.
/// <para>
/// Migration identity: the statements must see every tenant's rows, so the migration runs as the
/// owner of the <c>work</c> tables without <c>FORCE ROW LEVEL SECURITY</c>, or as a
/// <c>BYPASSRLS</c> role; otherwise it fails with <see cref="BlockedPrefix"/> instead of silently
/// skipping boards.
/// </para>
/// </summary>
public static class BoardGroupMigrationSql
{
    public const string BlockedPrefix = "BOARD_GROUP_BACKFILL_BLOCKED";

    public const string Backfill = $$"""
        DO $$
        BEGIN
            IF row_security_active('work.board_groups') OR row_security_active('work.boards') THEN
                RAISE EXCEPTION '{{BlockedPrefix}}: row-level security applies to role %; run migrations as the work table owner without FORCE ROW LEVEL SECURITY or as a BYPASSRLS role', current_user;
            END IF;
        END $$;

        INSERT INTO work.board_groups
            (id, account_id, workspace_id, board_id, title, color, position,
             is_collapsed, is_archived, created_at, created_by, version)
        SELECT gen_random_uuid(), b.account_id, b.workspace_id, b.id, o.name, o.color,
               'a' || substr('0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz', o.rn::int, 1),
               false, false, now(), b.created_by, 1
          FROM work.boards b
          JOIN (
                SELECT f.board_id, fo.name, fo.color,
                       row_number() OVER (PARTITION BY f.board_id ORDER BY fo.position COLLATE "C", fo.id) AS rn
                  FROM work.board_fields f
                  JOIN work.field_options fo ON fo.field_id = f.id
                 WHERE f.type = 'Status' AND f.is_system AND f.deleted_at IS NULL
               ) o ON o.board_id = b.id AND o.rn <= 62
         WHERE b.deleted_at IS NULL
           AND NOT EXISTS (SELECT 1 FROM work.board_groups g WHERE g.board_id = b.id AND g.deleted_at IS NULL);

        INSERT INTO work.board_groups
            (id, account_id, workspace_id, board_id, title, color, position,
             is_collapsed, is_archived, created_at, created_by, version)
        SELECT gen_random_uuid(), b.account_id, b.workspace_id, b.id, 'Tasks', '#7A8391', 'a0',
               false, false, now(), b.created_by, 1
          FROM work.boards b
         WHERE b.deleted_at IS NULL
           AND NOT EXISTS (SELECT 1 FROM work.board_groups g WHERE g.board_id = b.id AND g.deleted_at IS NULL);
        """;
}

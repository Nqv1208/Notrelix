namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// Existing-data step of migration <c>BackfillBoardOwners</c>: a board always has an Owner, and boards created
/// before that rule have none (the creator was not added as a member). A backfill must never re-grant access
/// that was taken away, so the creator becomes Owner only when all of these hold:
/// <list type="number">
/// <item>the board is live (not deleted) and has no Owner member;</item>
/// <item>the creator is an Active workspace member whose workspace role is Owner, Admin or Member (a Guest is
/// never given a board role by a backfill);</item>
/// <item>the creator holds no board role now, and the board's access-change log
/// (<c>governance.resource_access_changes</c>) records no removal or role change of that person on that board.</item>
/// </list>
/// Any other board stays ownerless: a workspace Owner assigns one (the share dialog says so). The log is kept 90
/// days, so a removal older than that cannot be seen; that is the known limit of this check. Only missing rows are
/// written (<c>ON CONFLICT DO NOTHING</c>), so a re-run changes nothing.
/// <para>
/// Migration identity: the statement must see every tenant's rows, so it runs as the owner of the <c>work</c>
/// tables without <c>FORCE ROW LEVEL SECURITY</c>, or as a <c>BYPASSRLS</c> role. Under any other identity
/// row-level security would silently hide rows, so it first fails with <see cref="BlockedPrefix"/> (same rule as the
/// item key backfill). One transaction, one insert per affected board.
/// </para>
/// </summary>
public static class BoardOwnerMigrationSql
{
    public const string BlockedPrefix = "BOARD_OWNER_BACKFILL_BLOCKED";

    public const string Backfill = $$"""
        DO $$
        BEGIN
            IF row_security_active('work.boards') OR row_security_active('work.board_members')
               OR row_security_active('workspace.workspace_members')
               OR row_security_active('governance.resource_access_changes') THEN
                RAISE EXCEPTION '{{BlockedPrefix}}: row-level security applies to role %; run migrations as the work table owner without FORCE ROW LEVEL SECURITY or as a BYPASSRLS role', current_user;
            END IF;
        END $$;

        INSERT INTO work.board_members (id, board_id, user_id, role, joined_at)
        SELECT gen_random_uuid(), b.id, b.created_by, 'Owner', b.created_at
          FROM work.boards b
          JOIN workspace.workspace_members wm
            ON wm.workspace_id = b.workspace_id AND wm.user_id = b.created_by
           AND wm.status = 'Active' AND wm.role IN ('Owner', 'Admin', 'Member')
         WHERE b.deleted_at IS NULL
           AND b.created_by IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM work.board_members m WHERE m.board_id = b.id AND m.role = 'Owner')
           AND NOT EXISTS (
               SELECT 1 FROM governance.resource_access_changes c
                WHERE c.resource_type = 'work-management.board'
                  AND c.resource_id = b.id
                  AND c.subject_type = 'User'
                  AND c.subject_id = b.created_by
                  AND c.change_kind IN ('Removed', 'Changed'))
        ON CONFLICT (board_id, user_id) DO NOTHING;
        """;
}

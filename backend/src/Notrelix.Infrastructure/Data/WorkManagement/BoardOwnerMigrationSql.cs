namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// Existing-data step of migration <c>BackfillBoardOwners</c>: a board always has an Owner, and boards created
/// before that rule have none (the creator was not added as a member). Each live (not deleted) board with no
/// Owner member gets its creator as Owner, when the creator is still an active member of the board's
/// workspace and holds no board role yet. A creator who already holds another role keeps it (a role is
/// never raised by a backfill), and a creator who left the workspace is skipped, so such a board stays
/// ownerless until a manager promotes someone. Only missing rows are written (<c>ON CONFLICT DO NOTHING</c>
/// on the board and user), so a re-run changes nothing.
/// <para>
/// Migration identity: the statement must see every tenant's rows, so it runs as the owner of the
/// <c>work</c> tables without <c>FORCE ROW LEVEL SECURITY</c>, or as a <c>BYPASSRLS</c> role. Under any
/// other identity row-level security would silently hide rows, so it first fails with
/// <see cref="BlockedPrefix"/> (same rule as the item key backfill). One transaction, one insert per
/// affected board: at the current volume (boards, not items) no batching is needed.
/// </para>
/// </summary>
public static class BoardOwnerMigrationSql
{
    public const string BlockedPrefix = "BOARD_OWNER_BACKFILL_BLOCKED";

    public const string Backfill = $$"""
        DO $$
        BEGIN
            IF row_security_active('work.boards') OR row_security_active('work.board_members')
               OR row_security_active('workspace.workspace_members') THEN
                RAISE EXCEPTION '{{BlockedPrefix}}: row-level security applies to role %; run migrations as the work table owner without FORCE ROW LEVEL SECURITY or as a BYPASSRLS role', current_user;
            END IF;
        END $$;

        INSERT INTO work.board_members (id, board_id, user_id, role, joined_at)
        SELECT gen_random_uuid(), b.id, b.created_by, 'Owner', b.created_at
          FROM work.boards b
          JOIN workspace.workspace_members wm
            ON wm.workspace_id = b.workspace_id AND wm.user_id = b.created_by AND wm.status = 'Active'
         WHERE b.deleted_at IS NULL
           AND b.created_by IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM work.board_members m WHERE m.board_id = b.id AND m.role = 'Owner')
        ON CONFLICT (board_id, user_id) DO NOTHING;
        """;
}

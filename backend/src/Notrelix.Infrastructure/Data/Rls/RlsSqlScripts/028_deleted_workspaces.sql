-- =============================================================================
-- 028_deleted_workspaces.sql — A deleted workspace is visible only to its Owner
-- =============================================================================
-- Deleting a workspace cuts access at once (owner decision 2026-11-01): every request on it, from a member or a
-- stranger, answers the identical NotFound. The application login now cannot even read the row unless the
-- person is an active Owner of that workspace, because the Owner alone may restore it within 30 days (the
-- "Recently deleted" list and the restore command read it; nothing else does). This only NARROWS the access
-- 007 gave: every predicate below is the 007 scope AND a further condition, never an OR that admits more.
-- The worker keeps its unconditional policy, so the purge job can find and remove an expired workspace.
-- Child content rows (boards, pages, ...) keep their own policies; the Application refuses them (see
-- AccessPolicyEngine) because the workspace is gone.
-- Runs after 007 and 026, whose helpers it calls; apply is idempotent.
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.is_workspace_owner(p_account_id uuid, p_workspace_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, workspace, ops, pg_temp
AS $$
    SELECT CASE
        WHEN p_account_id IS NULL OR p_workspace_id IS NULL THEN false
        WHEN ops.is_worker_scope() THEN true
        WHEN ops.current_user_id() IS NULL THEN false
        ELSE COALESCE((
            SELECT true
            FROM workspace.workspace_members m
            WHERE m.account_id = p_account_id
              AND m.workspace_id = p_workspace_id
              AND m.user_id = ops.current_user_id()
              AND m.role = 'Owner'
              AND m.status = 'Active'
            LIMIT 1), false)
    END;
$$;

REVOKE ALL ON FUNCTION ops.is_workspace_owner(uuid, uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.is_workspace_owner(uuid, uuid) TO notrelix_app, notrelix_worker;

-- An active workspace follows the Account scope as before; a deleted one also needs an Owner of it.
SELECT ops.create_policy('workspace', 'workspaces', 'p_app_select', 'SELECT', 'notrelix_app',
    'ops.has_account_access(account_id) AND (deleted_at IS NULL OR ops.is_workspace_owner(account_id, id))', NULL);
SELECT ops.create_policy('workspace', 'workspaces', 'p_app_update', 'UPDATE', 'notrelix_app',
    'ops.has_account_access(account_id) AND (deleted_at IS NULL OR ops.is_workspace_owner(account_id, id))',
    'ops.has_account_access(account_id) AND (deleted_at IS NULL OR ops.is_workspace_owner(account_id, id))');
SELECT ops.create_policy('workspace', 'workspaces', 'p_app_delete', 'DELETE', 'notrelix_app',
    'ops.has_account_access(account_id) AND (deleted_at IS NULL OR ops.is_workspace_owner(account_id, id))', NULL);

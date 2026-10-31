-- =============================================================================
-- 023_custom_role_assignments.sql — Custom-role assignments are an administrator write
-- =============================================================================
-- Replaces the generic tenant write policies of member_role_assignments.
-- =============================================================================

-- 6. A custom-role assignment attaches a role to an ACTIVE member of the workspace (member_id is the person's
-- user id: the Governance event that carries it is mapped to the integration event's UserId). Reading follows workspace access; writing needs a workspace
-- administrator. Removing or reactivating a member clears their assignments through ops.clear_member_custom_roles (024).
DO $$
DECLARE
    v_admin text := 'ops.is_workspace_admin(account_id, workspace_id)';
    v_target text := 'EXISTS (SELECT 1 FROM workspace.workspace_members m WHERE m.user_id = member_role_assignments.member_id '
        || 'AND m.account_id = member_role_assignments.account_id AND m.workspace_id = member_role_assignments.workspace_id '
        || 'AND m.status = ''Active'')';
BEGIN
    IF ops.table_exists('governance', 'member_role_assignments') THEN
        EXECUTE 'DROP POLICY IF EXISTS p_app_insert ON governance.member_role_assignments';
        EXECUTE 'DROP POLICY IF EXISTS p_app_update ON governance.member_role_assignments';
        EXECUTE 'DROP POLICY IF EXISTS p_app_delete ON governance.member_role_assignments';
        EXECUTE 'CREATE POLICY p_app_insert ON governance.member_role_assignments FOR INSERT TO notrelix_app WITH CHECK ('
            || v_admin || ' AND ' || v_target || ')';
        EXECUTE 'CREATE POLICY p_app_update ON governance.member_role_assignments FOR UPDATE TO notrelix_app USING ('
            || v_admin || ') WITH CHECK (' || v_admin || ' AND ' || v_target || ')';
        EXECUTE 'CREATE POLICY p_app_delete ON governance.member_role_assignments FOR DELETE TO notrelix_app USING ('
            || v_admin || ')';
    END IF;
END $$;

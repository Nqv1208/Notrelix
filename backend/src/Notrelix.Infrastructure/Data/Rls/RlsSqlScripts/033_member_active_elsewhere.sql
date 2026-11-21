-- =============================================================================
-- 033_member_active_elsewhere.sql — Is a member of this workspace also active in another Account?
-- =============================================================================
-- A second factor belongs to the person, not to a workspace, so an Owner may reset it only for a person whose
-- active memberships all sit inside the Owner's own Account. The application role cannot see another Account's
-- membership rows, so this bounded definer function answers one boolean and nothing else. It is valid only in
-- the `app` scope of the request's own account and workspace, for an active Owner of that workspace, about an
-- active member of that workspace.
-- =============================================================================
CREATE OR REPLACE FUNCTION ops.member_active_elsewhere(p_account uuid, p_workspace uuid, p_user uuid)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, workspace, ops, pg_temp
AS $$
BEGIN
    IF p_account IS NULL OR p_workspace IS NULL OR p_user IS NULL
       OR ops.current_request_scope() <> 'app'
       OR ops.current_user_id() IS NULL
       OR p_account IS DISTINCT FROM ops.current_account_id()
       OR p_workspace IS DISTINCT FROM ops.current_workspace_id()
       OR NOT EXISTS (
           SELECT 1 FROM workspace.workspace_members o
            WHERE o.workspace_id = p_workspace AND o.account_id = p_account
              AND o.user_id = ops.current_user_id() AND o.role = 'Owner' AND o.status = 'Active')
       OR NOT EXISTS (
           SELECT 1 FROM workspace.workspace_members t
            WHERE t.workspace_id = p_workspace AND t.account_id = p_account
              AND t.user_id = p_user AND t.status = 'Active') THEN
        RAISE EXCEPTION 'Membership lookup denied for the current scope' USING ERRCODE = '42501';
    END IF;

    RETURN EXISTS (
        SELECT 1 FROM workspace.workspace_members m
         WHERE m.user_id = p_user AND m.status = 'Active' AND m.account_id <> p_account);
END;
$$;

REVOKE ALL ON FUNCTION ops.member_active_elsewhere(uuid, uuid, uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.member_active_elsewhere(uuid, uuid, uuid) TO notrelix_app;

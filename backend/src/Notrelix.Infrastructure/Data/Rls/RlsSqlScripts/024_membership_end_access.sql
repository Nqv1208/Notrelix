-- =============================================================================
-- 024_membership_end_access.sql — Derived access ends with the membership
-- =============================================================================
-- Two bounded functions the application role calls in the request transaction that removes, or reactivates, a
-- member. Both are valid only in the `app` scope of the request's own account and workspace, for the person
-- themself or a workspace administrator. Application authorization stays the authority on who may remove.
-- =============================================================================

-- 1. Ends the custom-role assignments of one member of the request's own workspace. Valid for the person
-- themself (leaving) or a workspace administrator (removing or reactivating).
CREATE OR REPLACE FUNCTION ops.clear_member_custom_roles(p_account uuid, p_workspace uuid, p_user uuid)
RETURNS void
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
BEGIN
    IF p_account IS NULL OR p_workspace IS NULL OR p_user IS NULL
       OR ops.current_request_scope() <> 'app'
       OR ops.current_user_id() IS NULL
       OR p_account IS DISTINCT FROM ops.current_account_id()
       OR p_workspace IS DISTINCT FROM ops.current_workspace_id()
       OR NOT (p_user = ops.current_user_id() OR ops.is_workspace_admin(p_account, p_workspace)) THEN
        RAISE EXCEPTION 'Custom role clearing denied for the current scope' USING ERRCODE = '42501';
    END IF;

    -- member_id is the person's user id (see 023).
    DELETE FROM governance.member_role_assignments a
     WHERE a.account_id = p_account AND a.workspace_id = p_workspace AND a.member_id = p_user;
END;
$$;

-- 2. A person who holds no active grant in any OTHER workspace of the account loses their account-level
-- Member grant in the same write that ends the grant of p_workspace (the workspace being left, which is the
-- request's own). An account administrator's grant is never touched (it is an Account role, not a consequence
-- of workspace membership).
CREATE OR REPLACE FUNCTION ops.revoke_account_grant_if_last_workspace(
    p_account uuid, p_workspace uuid, p_user uuid, p_revoked_at timestamptz)
RETURNS void
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
BEGIN
    IF p_account IS NULL OR p_workspace IS NULL OR p_user IS NULL
       OR ops.current_request_scope() <> 'app'
       OR ops.current_user_id() IS NULL
       OR p_account IS DISTINCT FROM ops.current_account_id()
       OR p_workspace IS DISTINCT FROM ops.current_workspace_id()
       OR NOT (p_user = ops.current_user_id() OR ops.is_workspace_admin(p_account, p_workspace)) THEN
        RAISE EXCEPTION 'Account grant revocation denied for the current scope' USING ERRCODE = '42501';
    END IF;

    UPDATE authz.access_grants g
       SET revoked_at = p_revoked_at, updated_at = p_revoked_at
     WHERE g.account_id = p_account AND g.user_id = p_user AND g.workspace_id IS NULL
       AND g.revoked_at IS NULL AND g.is_account_admin = false
       AND NOT EXISTS (
           SELECT 1 FROM authz.access_grants w
            WHERE w.account_id = p_account AND w.user_id = p_user
              AND w.workspace_id IS NOT NULL AND w.workspace_id <> p_workspace
              AND w.revoked_at IS NULL AND w.membership_status = 'Active');
END;
$$;

REVOKE ALL ON FUNCTION ops.clear_member_custom_roles(uuid, uuid, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.revoke_account_grant_if_last_workspace(uuid, uuid, uuid, timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.clear_member_custom_roles(uuid, uuid, uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.revoke_account_grant_if_last_workspace(uuid, uuid, uuid, timestamptz) TO notrelix_app;

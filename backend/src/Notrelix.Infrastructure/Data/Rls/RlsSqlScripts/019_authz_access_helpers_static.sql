-- =============================================================================
-- 019_authz_access_helpers_static.sql — Static-SQL access helpers
-- =============================================================================
-- Supersedes the definitions of the same five signatures in 003_authz_access_helpers.sql (003 stays as the
-- history of the schema-aware version; this file runs after it, so the static body is the one left installed).
--
-- Why: the 003 bodies built their query with format() and EXECUTE for every call, and each call rebuilt the
-- active-grant predicate through ops.authz_grant_active_predicate_sql(), which probes information_schema three
-- times. A row-level policy calls these functions once per row, so a read of N rows paid about 0.18 ms per row
-- (a 5,000-item board read took about 0.9 s inside the database).
--
-- Semantics are identical to 003 on the installed schema: same inputs, same NULL handling, same early returns in
-- the same order, same session settings read (through ops.current_user_id(), ops.is_worker_scope() and
-- ops.has_invitee_scope()), same SECURITY DEFINER, search_path and grants. The only thing that changed is that
-- the schema variants 003 tolerated are resolved to the one this schema has: authz.access_grants carries
-- revoked_at, membership_status, is_account_admin, is_workspace_admin and permission_codes, and has no
-- expires_at, so an active grant is `revoked_at IS NULL AND membership_status = 'Active'`. A column that goes
-- missing now fails when this file is applied instead of silently widening or narrowing access at run time.
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.has_account_access(p_account_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
    SELECT CASE
        WHEN p_account_id IS NULL THEN false
        WHEN ops.is_worker_scope() THEN true
        WHEN ops.has_invitee_scope(p_account_id, NULL) THEN true
        WHEN ops.current_user_id() IS NULL THEN false
        ELSE COALESCE((
            SELECT true
            FROM authz.access_grants g
            WHERE g.account_id = p_account_id
              AND g.user_id = ops.current_user_id()
              AND g.revoked_at IS NULL
              AND g.membership_status = 'Active'
            LIMIT 1), false)
    END;
$$;

CREATE OR REPLACE FUNCTION ops.has_workspace_access(p_account_id uuid, p_workspace_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
    SELECT CASE
        WHEN p_workspace_id IS NULL THEN ops.has_account_access(p_account_id)
        WHEN ops.is_worker_scope() THEN true
        WHEN ops.has_invitee_scope(p_account_id, p_workspace_id) THEN true
        WHEN ops.current_user_id() IS NULL THEN false
        WHEN p_account_id IS NULL THEN COALESCE((
            SELECT true
            FROM authz.access_grants g
            WHERE g.workspace_id = p_workspace_id
              AND g.user_id = ops.current_user_id()
              AND g.revoked_at IS NULL
              AND g.membership_status = 'Active'
            LIMIT 1), false)
        ELSE COALESCE((
            SELECT true
            FROM authz.access_grants g
            WHERE g.account_id = p_account_id
              AND g.workspace_id = p_workspace_id
              AND g.user_id = ops.current_user_id()
              AND g.revoked_at IS NULL
              AND g.membership_status = 'Active'
            LIMIT 1), false)
    END;
$$;

CREATE OR REPLACE FUNCTION ops.is_account_admin(p_account_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
    SELECT CASE
        WHEN p_account_id IS NULL THEN false
        WHEN ops.is_worker_scope() THEN true
        WHEN ops.current_user_id() IS NULL THEN false
        ELSE COALESCE((
            SELECT true
            FROM authz.access_grants g
            WHERE g.account_id = p_account_id
              AND g.user_id = ops.current_user_id()
              AND g.is_account_admin = true
              AND g.revoked_at IS NULL
              AND g.membership_status = 'Active'
            LIMIT 1), false)
    END;
$$;

CREATE OR REPLACE FUNCTION ops.is_workspace_admin(p_account_id uuid, p_workspace_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
    SELECT CASE
        WHEN p_workspace_id IS NULL THEN ops.is_account_admin(p_account_id)
        WHEN ops.is_worker_scope() THEN true
        WHEN ops.current_user_id() IS NULL THEN false
        WHEN p_account_id IS NULL THEN COALESCE((
            SELECT true
            FROM authz.access_grants g
            WHERE g.workspace_id = p_workspace_id
              AND g.user_id = ops.current_user_id()
              AND (g.is_workspace_admin = true OR g.is_account_admin = true)
              AND g.revoked_at IS NULL
              AND g.membership_status = 'Active'
            LIMIT 1), false)
        ELSE COALESCE((
            SELECT true
            FROM authz.access_grants g
            WHERE g.account_id = p_account_id
              AND g.workspace_id = p_workspace_id
              AND g.user_id = ops.current_user_id()
              AND (g.is_workspace_admin = true OR g.is_account_admin = true)
              AND g.revoked_at IS NULL
              AND g.membership_status = 'Active'
            LIMIT 1), false)
    END;
$$;

CREATE OR REPLACE FUNCTION ops.has_permission(p_account_id uuid, p_workspace_id uuid, p_permission_code text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
    SELECT CASE
        WHEN p_permission_code IS NULL THEN false
        WHEN ops.is_worker_scope() THEN true
        WHEN ops.current_user_id() IS NULL THEN false
        ELSE COALESCE((
            SELECT true
            FROM authz.access_grants g
            WHERE g.user_id = ops.current_user_id()
              AND (p_account_id IS NULL OR g.account_id = p_account_id)
              AND (p_workspace_id IS NULL OR g.workspace_id = p_workspace_id)
              AND (
                  COALESCE(g.is_account_admin, false) = true
                  OR COALESCE(g.is_workspace_admin, false) = true
                  OR p_permission_code = ANY(g.permission_codes)
              )
              AND g.revoked_at IS NULL
              AND g.membership_status = 'Active'
            LIMIT 1), false)
    END;
$$;

GRANT EXECUTE ON FUNCTION ops.has_account_access(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.has_workspace_access(uuid, uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.is_account_admin(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.is_workspace_admin(uuid, uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.has_permission(uuid, uuid, text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;

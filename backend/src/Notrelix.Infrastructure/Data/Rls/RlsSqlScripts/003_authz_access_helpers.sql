-- =============================================================================
-- 003_authz_access_helpers.sql — Schema-aware authz helpers
-- =============================================================================
-- This file intentionally uses dynamic SQL for authz.access_grants so it supports
-- both known Schema V2 variants:
--   A) access_grants has expires_at but no membership_status
--   B) access_grants has membership_status but no expires_at
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS authz;

CREATE OR REPLACE FUNCTION ops.authz_grant_active_predicate_sql(p_alias text)
RETURNS text
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, information_schema, pg_temp
AS $$
DECLARE
    clauses text[] := ARRAY[]::text[];
BEGIN
    IF ops.column_exists('authz', 'access_grants', 'revoked_at') THEN
        clauses := clauses || format('%I.revoked_at IS NULL', p_alias);
    END IF;

    IF ops.column_exists('authz', 'access_grants', 'expires_at') THEN
        clauses := clauses || format('(%I.expires_at IS NULL OR %I.expires_at > now())', p_alias, p_alias);
    END IF;

    IF ops.column_exists('authz', 'access_grants', 'membership_status') THEN
        clauses := clauses || format('%I.membership_status = ''Active''', p_alias);
    END IF;

    IF array_length(clauses, 1) IS NULL THEN
        RETURN 'true';
    END IF;

    RETURN array_to_string(clauses, ' AND ');
END;
$$;

CREATE OR REPLACE FUNCTION ops.has_account_access(p_account_id uuid)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
DECLARE
    v_user_id uuid := ops.current_user_id();
    v_sql text;
    v_has boolean := false;
BEGIN
    IF p_account_id IS NULL THEN
        RETURN false;
    END IF;

    IF ops.is_worker_scope() THEN
        RETURN true;
    END IF;

    IF v_user_id IS NULL THEN
        RETURN false;
    END IF;

    IF NOT ops.table_exists('authz', 'access_grants') THEN
        RETURN false;
    END IF;

    v_sql := format(
        'SELECT EXISTS (
            SELECT 1
            FROM authz.access_grants g
            WHERE g.account_id = $1
              AND g.user_id = $2
              AND %s
        )',
        ops.authz_grant_active_predicate_sql('g')
    );

    EXECUTE v_sql USING p_account_id, v_user_id INTO v_has;
    RETURN COALESCE(v_has, false);
END;
$$;

CREATE OR REPLACE FUNCTION ops.has_workspace_access(p_account_id uuid, p_workspace_id uuid)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
DECLARE
    v_user_id uuid := ops.current_user_id();
    v_sql text;
    v_has boolean := false;
BEGIN
    IF p_workspace_id IS NULL THEN
        RETURN ops.has_account_access(p_account_id);
    END IF;

    IF ops.is_worker_scope() THEN
        RETURN true;
    END IF;

    IF v_user_id IS NULL THEN
        RETURN false;
    END IF;

    IF NOT ops.table_exists('authz', 'access_grants') THEN
        RETURN false;
    END IF;

    IF p_account_id IS NULL THEN
        v_sql := format(
            'SELECT EXISTS (
                SELECT 1
                FROM authz.access_grants g
                WHERE g.workspace_id = $1
                  AND g.user_id = $2
                  AND %s
            )',
            ops.authz_grant_active_predicate_sql('g')
        );
        EXECUTE v_sql USING p_workspace_id, v_user_id INTO v_has;
    ELSE
        v_sql := format(
            'SELECT EXISTS (
                SELECT 1
                FROM authz.access_grants g
                WHERE g.account_id = $1
                  AND g.workspace_id = $2
                  AND g.user_id = $3
                  AND %s
            )',
            ops.authz_grant_active_predicate_sql('g')
        );
        EXECUTE v_sql USING p_account_id, p_workspace_id, v_user_id INTO v_has;
    END IF;

    RETURN COALESCE(v_has, false);
END;
$$;

CREATE OR REPLACE FUNCTION ops.is_account_admin(p_account_id uuid)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
DECLARE
    v_user_id uuid := ops.current_user_id();
    v_sql text;
    v_has boolean := false;
BEGIN
    IF p_account_id IS NULL THEN
        RETURN false;
    END IF;

    IF ops.is_worker_scope() THEN
        RETURN true;
    END IF;

    IF v_user_id IS NULL OR NOT ops.table_exists('authz', 'access_grants') THEN
        RETURN false;
    END IF;

    IF NOT ops.column_exists('authz', 'access_grants', 'is_account_admin') THEN
        RETURN false;
    END IF;

    v_sql := format(
        'SELECT EXISTS (
            SELECT 1
            FROM authz.access_grants g
            WHERE g.account_id = $1
              AND g.user_id = $2
              AND g.is_account_admin = true
              AND %s
        )',
        ops.authz_grant_active_predicate_sql('g')
    );

    EXECUTE v_sql USING p_account_id, v_user_id INTO v_has;
    RETURN COALESCE(v_has, false);
END;
$$;

CREATE OR REPLACE FUNCTION ops.is_workspace_admin(p_account_id uuid, p_workspace_id uuid)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
DECLARE
    v_user_id uuid := ops.current_user_id();
    v_sql text;
    v_has boolean := false;
BEGIN
    IF p_workspace_id IS NULL THEN
        RETURN ops.is_account_admin(p_account_id);
    END IF;

    IF ops.is_worker_scope() THEN
        RETURN true;
    END IF;

    IF v_user_id IS NULL OR NOT ops.table_exists('authz', 'access_grants') THEN
        RETURN false;
    END IF;

    IF NOT ops.column_exists('authz', 'access_grants', 'is_workspace_admin') THEN
        RETURN ops.is_account_admin(p_account_id);
    END IF;

    IF p_account_id IS NULL THEN
        v_sql := format(
            'SELECT EXISTS (
                SELECT 1
                FROM authz.access_grants g
                WHERE g.workspace_id = $1
                  AND g.user_id = $2
                  AND (g.is_workspace_admin = true OR g.is_account_admin = true)
                  AND %s
            )',
            ops.authz_grant_active_predicate_sql('g')
        );
        EXECUTE v_sql USING p_workspace_id, v_user_id INTO v_has;
    ELSE
        v_sql := format(
            'SELECT EXISTS (
                SELECT 1
                FROM authz.access_grants g
                WHERE g.account_id = $1
                  AND g.workspace_id = $2
                  AND g.user_id = $3
                  AND (g.is_workspace_admin = true OR g.is_account_admin = true)
                  AND %s
            )',
            ops.authz_grant_active_predicate_sql('g')
        );
        EXECUTE v_sql USING p_account_id, p_workspace_id, v_user_id INTO v_has;
    END IF;

    RETURN COALESCE(v_has, false);
END;
$$;

CREATE OR REPLACE FUNCTION ops.has_permission(p_account_id uuid, p_workspace_id uuid, p_permission_code text)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
DECLARE
    v_user_id uuid := ops.current_user_id();
    v_sql text;
    v_has boolean := false;
BEGIN
    IF p_permission_code IS NULL THEN
        RETURN false;
    END IF;

    IF ops.is_worker_scope() THEN
        RETURN true;
    END IF;

    IF v_user_id IS NULL OR NOT ops.table_exists('authz', 'access_grants') THEN
        RETURN false;
    END IF;

    IF NOT ops.column_exists('authz', 'access_grants', 'permission_codes') THEN
        RETURN ops.is_workspace_admin(p_account_id, p_workspace_id);
    END IF;

    v_sql := format(
        'SELECT EXISTS (
            SELECT 1
            FROM authz.access_grants g
            WHERE g.user_id = $1
              AND ($2::uuid IS NULL OR g.account_id = $2)
              AND ($3::uuid IS NULL OR g.workspace_id = $3)
              AND (
                  COALESCE(g.is_account_admin, false) = true
                  OR COALESCE(g.is_workspace_admin, false) = true
                  OR $4 = ANY(g.permission_codes)
              )
              AND %s
        )',
        ops.authz_grant_active_predicate_sql('g')
    );

    EXECUTE v_sql USING v_user_id, p_account_id, p_workspace_id, p_permission_code INTO v_has;
    RETURN COALESCE(v_has, false);
END;
$$;

GRANT EXECUTE ON FUNCTION ops.authz_grant_active_predicate_sql(text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.has_account_access(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.has_workspace_access(uuid, uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.is_account_admin(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.is_workspace_admin(uuid, uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.has_permission(uuid, uuid, text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;

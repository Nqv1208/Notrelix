-- =============================================================================
-- 001_roles.sql — Runtime roles for Notrelix Schema V2 RLS
-- =============================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_app') THEN
        CREATE ROLE notrelix_app NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_auth') THEN
        CREATE ROLE notrelix_auth NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_worker') THEN
        CREATE ROLE notrelix_worker NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_support_readonly') THEN
        CREATE ROLE notrelix_support_readonly NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_migrator') THEN
        CREATE ROLE notrelix_migrator NOLOGIN;
    END IF;
END $$;


-- =============================================================================
-- 002_context_helpers.sql — Session context helpers
-- =============================================================================
CREATE SCHEMA IF NOT EXISTS ops;

CREATE OR REPLACE FUNCTION ops.current_setting_text(p_name text)
RETURNS text
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v text;
BEGIN
    v := NULLIF(current_setting(p_name, true), '');
    RETURN v;
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION ops.current_setting_uuid(p_name text)
RETURNS uuid
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v text;
BEGIN
    v := NULLIF(current_setting(p_name, true), '');
    IF v IS NULL THEN
        RETURN NULL;
    END IF;
    RETURN v::uuid;
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION ops.current_user_id()
RETURNS uuid
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_setting_uuid('app.current_user_id');
$$;

CREATE OR REPLACE FUNCTION ops.current_account_id()
RETURNS uuid
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_setting_uuid('app.current_account_id');
$$;

CREATE OR REPLACE FUNCTION ops.current_workspace_id()
RETURNS uuid
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_setting_uuid('app.current_workspace_id');
$$;

CREATE OR REPLACE FUNCTION ops.current_request_scope()
RETURNS text
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT COALESCE(ops.current_setting_text('app.request_scope'), 'app');
$$;

CREATE OR REPLACE FUNCTION ops.is_worker_scope()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_request_scope() IN ('worker', 'system', 'migration', 'migrator');
$$;

CREATE OR REPLACE FUNCTION ops.is_support_scope()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_request_scope() = 'support';
$$;

CREATE OR REPLACE FUNCTION ops.table_exists(p_schema text, p_table text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, information_schema, ops, pg_temp
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = p_schema
          AND table_name = p_table
          AND table_type = 'BASE TABLE'
    );
$$;

CREATE OR REPLACE FUNCTION ops.column_exists(p_schema text, p_table text, p_column text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, information_schema, ops, pg_temp
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = p_schema
          AND table_name = p_table
          AND column_name = p_column
    );
$$;

GRANT EXECUTE ON FUNCTION ops.current_setting_text(text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_setting_uuid(text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_user_id() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_account_id() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_workspace_id() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_request_scope() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.is_worker_scope() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.is_support_scope() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.table_exists(text, text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.column_exists(text, text, text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;


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


-- =============================================================================
-- 004_policy_runtime.sql — Dynamic policy applier helpers
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.drop_all_policies_for_table(p_schema text, p_table text)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    r record;
BEGIN
    IF NOT ops.table_exists(p_schema, p_table) THEN
        RETURN;
    END IF;

    FOR r IN
        SELECT policyname
        FROM pg_policies
        WHERE schemaname = p_schema
          AND tablename = p_table
    LOOP
        EXECUTE format('DROP POLICY IF EXISTS %I ON %I.%I', r.policyname, p_schema, p_table);
    END LOOP;
END;
$$;

CREATE OR REPLACE FUNCTION ops.enable_rls_for_table(p_schema text, p_table text)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
BEGIN
    IF NOT ops.table_exists(p_schema, p_table) THEN
        RETURN;
    END IF;

    EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY', p_schema, p_table);
    EXECUTE format('ALTER TABLE %I.%I FORCE ROW LEVEL SECURITY', p_schema, p_table);
END;
$$;

CREATE OR REPLACE FUNCTION ops.create_policy(
    p_schema text,
    p_table text,
    p_policy text,
    p_command text,
    p_roles text,
    p_using text DEFAULT NULL,
    p_check text DEFAULT NULL
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_sql text;
BEGIN
    IF NOT ops.table_exists(p_schema, p_table) THEN
        RETURN;
    END IF;

    EXECUTE format('DROP POLICY IF EXISTS %I ON %I.%I', p_policy, p_schema, p_table);

    v_sql := format(
        'CREATE POLICY %I ON %I.%I FOR %s TO %s',
        p_policy,
        p_schema,
        p_table,
        p_command,
        p_roles
    );

    IF p_using IS NOT NULL THEN
        v_sql := v_sql || ' USING (' || p_using || ')';
    END IF;

    IF p_check IS NOT NULL THEN
        v_sql := v_sql || ' WITH CHECK (' || p_check || ')';
    END IF;

    EXECUTE v_sql;
END;
$$;

CREATE OR REPLACE FUNCTION ops.scope_expression_for_table(p_schema text, p_table text)
RETURNS text
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, information_schema, pg_temp
AS $$
BEGIN
    IF ops.column_exists(p_schema, p_table, 'account_id')
       AND ops.column_exists(p_schema, p_table, 'workspace_id') THEN
        RETURN 'ops.has_workspace_access(account_id, workspace_id)';
    END IF;

    IF ops.column_exists(p_schema, p_table, 'account_id') THEN
        RETURN 'ops.has_account_access(account_id)';
    END IF;

    IF ops.column_exists(p_schema, p_table, 'workspace_id') THEN
        RETURN 'ops.has_workspace_access(NULL, workspace_id)';
    END IF;

    RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION ops.apply_scoped_business_policies(
    p_schema text,
    p_table text,
    p_allow_app_write boolean DEFAULT true
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_scope text;
BEGIN
    IF NOT ops.table_exists(p_schema, p_table) THEN
        RAISE NOTICE 'RLS skipped missing table %.%', p_schema, p_table;
        RETURN;
    END IF;

    v_scope := ops.scope_expression_for_table(p_schema, p_table);

    IF v_scope IS NULL THEN
        RAISE NOTICE 'RLS skipped %.% because no account_id/workspace_id column exists', p_schema, p_table;
        RETURN;
    END IF;

    PERFORM ops.drop_all_policies_for_table(p_schema, p_table);
    PERFORM ops.enable_rls_for_table(p_schema, p_table);

    PERFORM ops.create_policy(p_schema, p_table, 'p_app_select', 'SELECT', 'notrelix_app', v_scope, NULL);

    IF p_allow_app_write THEN
        PERFORM ops.create_policy(p_schema, p_table, 'p_app_insert', 'INSERT', 'notrelix_app', NULL, v_scope);
        PERFORM ops.create_policy(p_schema, p_table, 'p_app_update', 'UPDATE', 'notrelix_app', v_scope, v_scope);
        PERFORM ops.create_policy(p_schema, p_table, 'p_app_delete', 'DELETE', 'notrelix_app', v_scope, NULL);
    END IF;

    PERFORM ops.create_policy(p_schema, p_table, 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
    PERFORM ops.create_policy(p_schema, p_table, 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
END;
$$;

CREATE OR REPLACE FUNCTION ops.apply_readonly_projection_policies(p_schema text, p_table text)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_scope text;
BEGIN
    IF NOT ops.table_exists(p_schema, p_table) THEN
        RAISE NOTICE 'RLS skipped missing table %.%', p_schema, p_table;
        RETURN;
    END IF;

    v_scope := COALESCE(ops.scope_expression_for_table(p_schema, p_table), 'false');

    PERFORM ops.drop_all_policies_for_table(p_schema, p_table);
    PERFORM ops.enable_rls_for_table(p_schema, p_table);
    PERFORM ops.create_policy(p_schema, p_table, 'p_app_select', 'SELECT', 'notrelix_app', v_scope, NULL);
    PERFORM ops.create_policy(p_schema, p_table, 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
    PERFORM ops.create_policy(p_schema, p_table, 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
END;
$$;

CREATE OR REPLACE FUNCTION ops.apply_worker_internal_policies(
    p_schema text,
    p_table text,
    p_allow_app_insert boolean DEFAULT false
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_scope text;
BEGIN
    IF NOT ops.table_exists(p_schema, p_table) THEN
        RAISE NOTICE 'RLS skipped missing table %.%', p_schema, p_table;
        RETURN;
    END IF;

    v_scope := COALESCE(ops.scope_expression_for_table(p_schema, p_table), 'true');

    PERFORM ops.drop_all_policies_for_table(p_schema, p_table);
    PERFORM ops.enable_rls_for_table(p_schema, p_table);
    PERFORM ops.create_policy(p_schema, p_table, 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
    PERFORM ops.create_policy(p_schema, p_table, 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);

    IF p_allow_app_insert THEN
        PERFORM ops.create_policy(p_schema, p_table, 'p_app_insert', 'INSERT', 'notrelix_app', NULL, v_scope);
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION ops.apply_catalog_policies(p_schema text, p_table text)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
BEGIN
    IF NOT ops.table_exists(p_schema, p_table) THEN
        RAISE NOTICE 'RLS skipped missing table %.%', p_schema, p_table;
        RETURN;
    END IF;

    PERFORM ops.drop_all_policies_for_table(p_schema, p_table);
    PERFORM ops.enable_rls_for_table(p_schema, p_table);
    PERFORM ops.create_policy(p_schema, p_table, 'p_app_select', 'SELECT', 'notrelix_app', 'true', NULL);
    PERFORM ops.create_policy(p_schema, p_table, 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
    PERFORM ops.create_policy(p_schema, p_table, 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
END;
$$;

CREATE OR REPLACE FUNCTION ops.apply_user_owned_policies(
    p_schema text,
    p_table text,
    p_user_column text,
    p_auth_all boolean DEFAULT false,
    p_app_write boolean DEFAULT true
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_own text;
BEGIN
    IF NOT ops.table_exists(p_schema, p_table) THEN
        RAISE NOTICE 'RLS skipped missing table %.%', p_schema, p_table;
        RETURN;
    END IF;

    IF NOT ops.column_exists(p_schema, p_table, p_user_column) THEN
        RAISE NOTICE 'RLS skipped %.% because user column % does not exist', p_schema, p_table, p_user_column;
        RETURN;
    END IF;

    v_own := format('%I = ops.current_user_id()', p_user_column);

    PERFORM ops.drop_all_policies_for_table(p_schema, p_table);
    PERFORM ops.enable_rls_for_table(p_schema, p_table);
    PERFORM ops.create_policy(p_schema, p_table, 'p_app_select_own', 'SELECT', 'notrelix_app', v_own, NULL);

    IF p_app_write THEN
        PERFORM ops.create_policy(p_schema, p_table, 'p_app_insert_own', 'INSERT', 'notrelix_app', NULL, v_own);
        PERFORM ops.create_policy(p_schema, p_table, 'p_app_update_own', 'UPDATE', 'notrelix_app', v_own, v_own);
        PERFORM ops.create_policy(p_schema, p_table, 'p_app_delete_own', 'DELETE', 'notrelix_app', v_own, NULL);
    END IF;

    IF p_auth_all THEN
        PERFORM ops.create_policy(p_schema, p_table, 'p_auth_all', 'ALL', 'notrelix_auth', 'true', 'true');
    END IF;

    PERFORM ops.create_policy(p_schema, p_table, 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
    PERFORM ops.create_policy(p_schema, p_table, 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
END;
$$;

GRANT EXECUTE ON FUNCTION ops.drop_all_policies_for_table(text, text) TO notrelix_migrator;
GRANT EXECUTE ON FUNCTION ops.enable_rls_for_table(text, text) TO notrelix_migrator;
GRANT EXECUTE ON FUNCTION ops.create_policy(text, text, text, text, text, text, text) TO notrelix_migrator;
GRANT EXECUTE ON FUNCTION ops.scope_expression_for_table(text, text) TO notrelix_migrator, notrelix_app, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.apply_scoped_business_policies(text, text, boolean) TO notrelix_migrator;
GRANT EXECUTE ON FUNCTION ops.apply_readonly_projection_policies(text, text) TO notrelix_migrator;
GRANT EXECUTE ON FUNCTION ops.apply_worker_internal_policies(text, text, boolean) TO notrelix_migrator;
GRANT EXECUTE ON FUNCTION ops.apply_catalog_policies(text, text) TO notrelix_migrator;
GRANT EXECUTE ON FUNCTION ops.apply_user_owned_policies(text, text, text, boolean, boolean) TO notrelix_migrator;


-- =============================================================================
-- 005_grants.sql — Schema/table/function privileges
-- =============================================================================

DO $$
DECLARE
    s text;
BEGIN
    FOREACH s IN ARRAY ARRAY['account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops']
    LOOP
        IF EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = s) THEN
            EXECUTE format('GRANT USAGE ON SCHEMA %I TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly', s);
            EXECUTE format('GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I TO notrelix_migrator', s);
            EXECUTE format('GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I TO notrelix_migrator', s);
            EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO notrelix_app', s);
            EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO notrelix_auth', s);
            EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO notrelix_worker', s);
            EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO notrelix_support_readonly', s);
        END IF;
    END LOOP;
END $$;


-- =============================================================================
-- 006_policies_identity.sql — Identity/Auth policies
-- =============================================================================

DO $$
BEGIN
    IF ops.table_exists('identity', 'users') THEN
        PERFORM ops.drop_all_policies_for_table('identity', 'users');
        PERFORM ops.enable_rls_for_table('identity', 'users');
        PERFORM ops.create_policy('identity', 'users', 'p_app_select_self', 'SELECT', 'notrelix_app', 'id = ops.current_user_id()', NULL);
        PERFORM ops.create_policy('identity', 'users', 'p_app_update_self', 'UPDATE', 'notrelix_app', 'id = ops.current_user_id()', 'id = ops.current_user_id()');
        PERFORM ops.create_policy('identity', 'users', 'p_auth_all', 'ALL', 'notrelix_auth', 'true', 'true');
        PERFORM ops.create_policy('identity', 'users', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('identity', 'users', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;

SELECT ops.apply_user_owned_policies('identity', 'user_profiles', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'user_sessions', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'oauth_accounts', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'user_security_settings', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'user_mfa_methods', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'user_api_tokens', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'user_login_attempts', 'user_id', true, false);
SELECT ops.apply_user_owned_policies('identity', 'email_verification_tokens', 'user_id', true, false);
SELECT ops.apply_user_owned_policies('identity', 'password_reset_tokens', 'user_id', true, false);


-- =============================================================================
-- 007_policies_platform.sql — Account/Workspace/Governance/Authz policies
-- =============================================================================

DO $$
BEGIN
    IF ops.table_exists('account', 'accounts') THEN
        PERFORM ops.drop_all_policies_for_table('account', 'accounts');
        PERFORM ops.enable_rls_for_table('account', 'accounts');
        PERFORM ops.create_policy('account', 'accounts', 'p_app_select', 'SELECT', 'notrelix_app', 'ops.has_account_access(id)', NULL);
        PERFORM ops.create_policy('account', 'accounts', 'p_app_update_admin', 'UPDATE', 'notrelix_app', 'ops.is_account_admin(id)', 'ops.is_account_admin(id)');
        PERFORM ops.create_policy('account', 'accounts', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('account', 'accounts', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;

SELECT ops.apply_scoped_business_policies('account', 'account_members', true);
SELECT ops.apply_scoped_business_policies('account', 'account_invitations', true);
SELECT ops.apply_scoped_business_policies('account', 'account_domains', true);
SELECT ops.apply_scoped_business_policies('account', 'account_settings', true);
SELECT ops.apply_scoped_business_policies('account', 'account_regions', true);
SELECT ops.apply_scoped_business_policies('account', 'account_identity_providers', true);
SELECT ops.apply_scoped_business_policies('account', 'scim_directories', true);
SELECT ops.apply_scoped_business_policies('account', 'scim_sync_runs', true);
SELECT ops.apply_scoped_business_policies('account', 'workspace_routes', true);
SELECT ops.apply_scoped_business_policies('workspace', 'workspaces', true);
SELECT ops.apply_scoped_business_policies('workspace', 'workspace_members', true);
SELECT ops.apply_scoped_business_policies('workspace', 'workspace_invitations', true);
SELECT ops.apply_scoped_business_policies('workspace', 'spaces', true);
SELECT ops.apply_scoped_business_policies('workspace', 'teams', true);
SELECT ops.apply_scoped_business_policies('workspace', 'team_members', true);
SELECT ops.apply_scoped_business_policies('governance', 'custom_roles', true);
SELECT ops.apply_scoped_business_policies('governance', 'custom_role_permissions', true);
SELECT ops.apply_scoped_business_policies('governance', 'workspace_member_role_assignments', true);
SELECT ops.apply_scoped_business_policies('governance', 'resource_permissions', true);
SELECT ops.apply_scoped_business_policies('governance', 'field_permissions', true);
SELECT ops.apply_scoped_business_policies('governance', 'permission_rules', true);
SELECT ops.apply_scoped_business_policies('governance', 'permission_templates', true);
SELECT ops.apply_scoped_business_policies('governance', 'workspace_policies', true);
SELECT ops.apply_scoped_business_policies('governance', 'share_links', true);
SELECT ops.apply_scoped_business_policies('governance', 'resource_permission_inheritance_cache', true);

DO $$
DECLARE
    v_own text;
BEGIN
    IF ops.table_exists('authz', 'access_grants') THEN
        PERFORM ops.drop_all_policies_for_table('authz', 'access_grants');
        PERFORM ops.enable_rls_for_table('authz', 'access_grants');

        IF ops.column_exists('authz', 'access_grants', 'user_id') THEN
            v_own := 'user_id = ops.current_user_id()';
        ELSE
            v_own := 'false';
        END IF;

        PERFORM ops.create_policy('authz', 'access_grants', 'p_app_select_own', 'SELECT', 'notrelix_app', v_own, NULL);
        PERFORM ops.create_policy('authz', 'access_grants', 'p_auth_select_own', 'SELECT', 'notrelix_auth', v_own, NULL);
        PERFORM ops.create_policy('authz', 'access_grants', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('authz', 'access_grants', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;


-- =============================================================================
-- 008_policies_workspace_scoped_domain.sql — Product/domain schema policies
-- =============================================================================
SELECT ops.apply_scoped_business_policies('work', 'boards', true);
SELECT ops.apply_scoped_business_policies('work', 'board_groups', true);
SELECT ops.apply_scoped_business_policies('work', 'board_fields', true);
SELECT ops.apply_scoped_business_policies('work', 'field_options', true);
SELECT ops.apply_scoped_business_policies('work', 'board_items', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_values', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_members', true);
SELECT ops.apply_scoped_business_policies('work', 'labels', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_labels', true);
SELECT ops.apply_scoped_business_policies('work', 'board_views', true);
SELECT ops.apply_scoped_business_policies('work', 'board_view_user_preferences', true);
SELECT ops.apply_scoped_business_policies('work', 'saved_filters', true);
SELECT ops.apply_scoped_business_policies('work', 'board_view_pins', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_links', true);
SELECT ops.apply_scoped_business_policies('work', 'checklists', true);
SELECT ops.apply_scoped_business_policies('work', 'checklist_items', true);
SELECT ops.apply_scoped_business_policies('work', 'relation_field_configs', true);
SELECT ops.apply_scoped_business_policies('work', 'board_relations', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_connections', true);
SELECT ops.apply_scoped_business_policies('work', 'formula_dependencies', true);
SELECT ops.apply_scoped_business_policies('work', 'mirror_value_snapshots', true);
SELECT ops.apply_scoped_business_policies('work', 'rollup_snapshots', true);
SELECT ops.apply_scoped_business_policies('work', 'approval_requests', true);
SELECT ops.apply_scoped_business_policies('work', 'approval_steps', true);
SELECT ops.apply_scoped_business_policies('work', 'workload_allocations', true);
SELECT ops.apply_scoped_business_policies('work', 'board_templates', true);
SELECT ops.apply_scoped_business_policies('work', 'item_templates', true);
SELECT ops.apply_scoped_business_policies('work', 'board_subscribers', true);
SELECT ops.apply_scoped_business_policies('work', 'item_dependencies', true);
SELECT ops.apply_scoped_business_policies('work', 'time_tracking_entries', true);
SELECT ops.apply_scoped_business_policies('work', 'forms', true);
SELECT ops.apply_scoped_business_policies('work', 'form_questions', true);
SELECT ops.apply_scoped_business_policies('work', 'form_submissions', true);
SELECT ops.apply_scoped_business_policies('docs', 'pages', true);
SELECT ops.apply_scoped_business_policies('docs', 'blocks', true);
SELECT ops.apply_scoped_business_policies('docs', 'document_versions', true);
SELECT ops.apply_scoped_business_policies('docs', 'resource_links', true);
SELECT ops.apply_scoped_business_policies('docs', 'page_templates', true);
SELECT ops.apply_scoped_business_policies('collab', 'comments', true);
SELECT ops.apply_scoped_business_policies('collab', 'reactions', true);
SELECT ops.apply_scoped_business_policies('collab', 'mentions', true);
SELECT ops.apply_scoped_business_policies('collab', 'attachments', true);
SELECT ops.apply_scoped_business_policies('collab', 'resource_watchers', true);
SELECT ops.apply_scoped_business_policies('collab', 'presence_sessions', true);
SELECT ops.apply_scoped_business_policies('collab', 'resource_read_states', true);
SELECT ops.apply_scoped_business_policies('automation', 'automation_rules', true);
SELECT ops.apply_scoped_business_policies('automation', 'automation_executions', true);
SELECT ops.apply_scoped_business_policies('automation', 'scheduled_jobs', true);
SELECT ops.apply_scoped_business_policies('automation', 'automation_templates', true);
SELECT ops.apply_scoped_business_policies('automation', 'ai_agents', true);
SELECT ops.apply_scoped_business_policies('automation', 'ai_agent_runs', true);
SELECT ops.apply_scoped_business_policies('integration', 'integration_connections', true);
SELECT ops.apply_scoped_business_policies('integration', 'integration_scopes', true);
SELECT ops.apply_scoped_business_policies('integration', 'integration_secret_versions', true);
SELECT ops.apply_scoped_business_policies('integration', 'webhook_subscriptions', true);
SELECT ops.apply_scoped_business_policies('integration', 'webhook_deliveries', true);
SELECT ops.apply_scoped_business_policies('integration', 'inbound_webhook_events', true);
SELECT ops.apply_scoped_business_policies('integration', 'calendar_integrations', true);
SELECT ops.apply_scoped_business_policies('integration', 'calendar_event_links', true);
SELECT ops.apply_scoped_business_policies('integration', 'integration_sync_cursors', true);
SELECT ops.apply_scoped_business_policies('reporting', 'dashboards', true);
SELECT ops.apply_scoped_business_policies('reporting', 'dashboard_widgets', true);
SELECT ops.apply_scoped_business_policies('reporting', 'dashboard_sources', true);
SELECT ops.apply_scoped_business_policies('reporting', 'reporting_snapshots', true);
SELECT ops.apply_scoped_business_policies('billing', 'billing_customers', true);
SELECT ops.apply_catalog_policies('billing', 'plans');
SELECT ops.apply_catalog_policies('billing', 'plan_prices');
SELECT ops.apply_catalog_policies('billing', 'plan_limits');
SELECT ops.apply_scoped_business_policies('billing', 'subscriptions', true);
SELECT ops.apply_scoped_business_policies('billing', 'subscription_items', true);
SELECT ops.apply_scoped_business_policies('billing', 'payment_methods', true);
SELECT ops.apply_scoped_business_policies('billing', 'invoices', true);
SELECT ops.apply_scoped_business_policies('billing', 'invoice_line_items', true);
SELECT ops.apply_scoped_business_policies('billing', 'entitlements', true);
SELECT ops.apply_scoped_business_policies('billing', 'usage_metrics', true);
SELECT ops.apply_scoped_business_policies('billing', 'usage_metric_history', true);
SELECT ops.apply_scoped_business_policies('billing', 'feature_usage_ledger', true);
SELECT ops.apply_worker_internal_policies('billing', 'billing_events', true);


-- =============================================================================
-- 009_policies_notifications_activity_search.sql — Projection/read-model policies
-- =============================================================================
SELECT ops.apply_readonly_projection_policies('search', 'search_documents');
SELECT ops.apply_worker_internal_policies('search', 'search_index_jobs', true);

DO $$
BEGIN
    IF ops.table_exists('notifications', 'notification_items') THEN
        PERFORM ops.drop_all_policies_for_table('notifications', 'notification_items');
        PERFORM ops.enable_rls_for_table('notifications', 'notification_items');
        IF ops.table_exists('notifications', 'notification_recipients')
           AND ops.column_exists('notifications', 'notification_recipients', 'notification_id')
           AND ops.column_exists('notifications', 'notification_recipients', 'user_id') THEN
            PERFORM ops.create_policy(
                'notifications',
                'notification_items',
                'p_app_select_recipient',
                'SELECT',
                'notrelix_app',
                'EXISTS (
                    SELECT 1
                    FROM notifications.notification_recipients nr
                    WHERE nr.notification_id = id
                      AND nr.user_id = ops.current_user_id()
                )',
                NULL
            );
        ELSE
            PERFORM ops.create_policy('notifications', 'notification_items', 'p_app_select_workspace', 'SELECT', 'notrelix_app', 'ops.has_workspace_access(account_id, workspace_id)', NULL);
        END IF;
        PERFORM ops.create_policy('notifications', 'notification_items', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('notifications', 'notification_items', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;

SELECT ops.apply_user_owned_policies('notifications', 'notification_recipients', 'user_id', false, true);
SELECT ops.apply_user_owned_policies('notifications', 'notification_preferences', 'user_id', false, true);
SELECT ops.apply_user_owned_policies('notifications', 'notification_counters', 'user_id', false, true);
SELECT ops.apply_worker_internal_policies('notifications', 'notification_deliveries', true);
SELECT ops.apply_worker_internal_policies('notifications', 'email_outbox', true);
SELECT ops.apply_worker_internal_policies('notifications', 'email_delivery_attempts', false);
SELECT ops.apply_readonly_projection_policies('activity', 'workspace_activity_logs');
SELECT ops.apply_user_owned_policies('activity', 'activity_read_states', 'user_id', false, true);
SELECT ops.apply_readonly_projection_policies('analytics', 'workspace_usage_daily');
SELECT ops.apply_readonly_projection_policies('analytics', 'feature_usage_daily');


-- =============================================================================
-- 010_policies_events_messaging_audit_ops.sql — Runtime/internal policies
-- =============================================================================
SELECT ops.apply_worker_internal_policies('events', 'domain_event_logs', true);
SELECT ops.apply_worker_internal_policies('messaging', 'outbox_messages', true);
SELECT ops.apply_worker_internal_policies('messaging', 'outbox_delivery_attempts', false);
SELECT ops.apply_worker_internal_policies('messaging', 'processed_events', false);

DO $$
DECLARE
    v_scope text;
    v_admin_read text;
BEGIN
    IF ops.table_exists('audit', 'audit_logs') THEN
        v_scope := COALESCE(ops.scope_expression_for_table('audit', 'audit_logs'), 'false');
        v_admin_read := CASE
            WHEN ops.column_exists('audit', 'audit_logs', 'account_id') AND ops.column_exists('audit', 'audit_logs', 'workspace_id')
                THEN '(ops.is_account_admin(account_id) OR ops.is_workspace_admin(account_id, workspace_id) OR ops.has_permission(account_id, workspace_id, ''audit.read''))'
            WHEN ops.column_exists('audit', 'audit_logs', 'account_id')
                THEN '(ops.is_account_admin(account_id) OR ops.has_permission(account_id, NULL, ''audit.read''))'
            ELSE 'false'
        END;

        PERFORM ops.drop_all_policies_for_table('audit', 'audit_logs');
        PERFORM ops.enable_rls_for_table('audit', 'audit_logs');
        PERFORM ops.create_policy('audit', 'audit_logs', 'p_app_insert', 'INSERT', 'notrelix_app', NULL, v_scope);
        PERFORM ops.create_policy('audit', 'audit_logs', 'p_app_select_admin', 'SELECT', 'notrelix_app', v_admin_read, NULL);
        PERFORM ops.create_policy('audit', 'audit_logs', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('audit', 'audit_logs', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;


DO $$
DECLARE
    v_scope text;
    v_admin_read text;
BEGIN
    IF ops.table_exists('audit', 'security_events') THEN
        v_scope := COALESCE(ops.scope_expression_for_table('audit', 'security_events'), 'false');
        v_admin_read := CASE
            WHEN ops.column_exists('audit', 'security_events', 'account_id') AND ops.column_exists('audit', 'security_events', 'workspace_id')
                THEN '(ops.is_account_admin(account_id) OR ops.is_workspace_admin(account_id, workspace_id) OR ops.has_permission(account_id, workspace_id, ''audit.read''))'
            WHEN ops.column_exists('audit', 'security_events', 'account_id')
                THEN '(ops.is_account_admin(account_id) OR ops.has_permission(account_id, NULL, ''audit.read''))'
            ELSE 'false'
        END;

        PERFORM ops.drop_all_policies_for_table('audit', 'security_events');
        PERFORM ops.enable_rls_for_table('audit', 'security_events');
        PERFORM ops.create_policy('audit', 'security_events', 'p_app_insert', 'INSERT', 'notrelix_app', NULL, v_scope);
        PERFORM ops.create_policy('audit', 'security_events', 'p_app_select_admin', 'SELECT', 'notrelix_app', v_admin_read, NULL);
        PERFORM ops.create_policy('audit', 'security_events', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('audit', 'security_events', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;


DO $$
DECLARE
    v_scope text;
    v_own text;
BEGIN
    IF ops.table_exists('ops', 'idempotency_keys') THEN
        v_scope := COALESCE(ops.scope_expression_for_table('ops', 'idempotency_keys'), 'false');
        IF ops.column_exists('ops', 'idempotency_keys', 'user_id') THEN
            v_own := '(' || v_scope || ' AND user_id = ops.current_user_id())';
        ELSE
            v_own := v_scope;
        END IF;
        PERFORM ops.drop_all_policies_for_table('ops', 'idempotency_keys');
        PERFORM ops.enable_rls_for_table('ops', 'idempotency_keys');
        PERFORM ops.create_policy('ops', 'idempotency_keys', 'p_app_select', 'SELECT', 'notrelix_app', v_own, NULL);
        PERFORM ops.create_policy('ops', 'idempotency_keys', 'p_app_insert', 'INSERT', 'notrelix_app', NULL, v_own);
        PERFORM ops.create_policy('ops', 'idempotency_keys', 'p_app_update', 'UPDATE', 'notrelix_app', v_own, v_own);
        PERFORM ops.create_policy('ops', 'idempotency_keys', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('ops', 'idempotency_keys', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;

SELECT ops.apply_worker_internal_policies('ops', 'job_locks', false);
SELECT ops.apply_scoped_business_policies('ops', 'import_jobs', true);
SELECT ops.apply_scoped_business_policies('ops', 'export_jobs', true);
SELECT ops.apply_worker_internal_policies('ops', 'cleanup_runs', false);


-- =============================================================================
-- 011_verification.sql — RLS verification queries
-- =============================================================================

-- 1. Legacy/stale tables must not exist in Schema V2.
WITH forbidden(schema_name, table_name) AS (
    VALUES
        ('collab','notifications'),
        ('collab','notification_preferences'),
        ('collab','notification_deliveries'),
        ('collab','unread_counters'),
        ('collab','activity_logs'),
        ('audit','activity_logs'),
        ('governance','audit_logs'),
        ('governance','security_events'),
        ('governance','audit_retention_policies'),
        ('governance','member_role_assignments'),
        ('automation','outbox_messages'),
        ('ops','processed_events'),
        ('billing','workspace_feature_usages'),
        ('identity','api_tokens')
)
SELECT f.schema_name, f.table_name, 'FORBIDDEN_TABLE_EXISTS' AS issue
FROM forbidden f
JOIN information_schema.tables t
  ON t.table_schema = f.schema_name
 AND t.table_name = f.table_name
WHERE t.table_type = 'BASE TABLE'
ORDER BY f.schema_name, f.table_name;

-- 2. Tables in target schemas with RLS disabled.
SELECT n.nspname AS schema_name, c.relname AS table_name, 'RLS_DISABLED' AS issue
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE c.relkind = 'r'
  AND n.nspname IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
  AND c.relrowsecurity = false
ORDER BY n.nspname, c.relname;

-- 3. RLS-enabled tables without policies.
SELECT n.nspname AS schema_name, c.relname AS table_name, 'NO_POLICY' AS issue
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
LEFT JOIN pg_policies p
  ON p.schemaname = n.nspname
 AND p.tablename = c.relname
WHERE c.relkind = 'r'
  AND n.nspname IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
  AND c.relrowsecurity = true
GROUP BY n.nspname, c.relname
HAVING count(p.policyname) = 0
ORDER BY n.nspname, c.relname;

-- 4. Policy count by schema.
SELECT schemaname, count(*) AS policy_count
FROM pg_policies
WHERE schemaname IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
GROUP BY schemaname
ORDER BY schemaname;

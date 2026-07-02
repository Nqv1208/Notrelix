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

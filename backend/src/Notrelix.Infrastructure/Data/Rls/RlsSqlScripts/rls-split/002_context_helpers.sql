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

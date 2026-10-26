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

-- The unbounded background principal. Two things must hold: the request scope names it AND the login of
-- the connection is a member of notrelix_worker. The scope value is a session setting that any
-- connection can write, so it alone proves nothing; the login is chosen by the connection string and
-- cannot be changed from inside a session. session_user (not current_user) is read because inside this
-- SECURITY DEFINER function current_user is the function owner. The application login is never a member
-- of notrelix_worker, so setting the scope on an application connection grants nothing.
CREATE OR REPLACE FUNCTION ops.is_worker_scope()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_request_scope() = 'worker'
       AND pg_has_role(session_user, 'notrelix_worker', 'MEMBER');
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

-- =============================================================================
-- 027_operator_console.sql — The platform operator login: its own role, a closed allow-list, no tenant content
-- =============================================================================
-- The operator console runs on its own database login (notrelix_operator). The role holds privileges ONLY on the
-- tables below and on one counts-only function; it has no privilege on any tenant content table, identity table
-- or credential, so a bug or a stolen operator session cannot read a board, an item, a page or a comment.
-- Row-level security is forced on every table the role can touch and the policies are bound to the role (not to a
-- session setting an application connection could write). Grants are explicit and never inherited: the role is a
-- member of no other role. Runs after 005, which does not know this role; apply is idempotent.
-- =============================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_operator') THEN
        CREATE ROLE notrelix_operator NOLOGIN;
    END IF;
END $$;

-- 1. The role is a member of nothing, and nothing but the migrator and the owner is a member of it.
DO $$
DECLARE
    m record;
BEGIN
    FOR m IN
        SELECT g.rolname AS granted
          FROM pg_auth_members am
          JOIN pg_roles g ON g.oid = am.roleid
          JOIN pg_roles r ON r.oid = am.member
         WHERE r.rolname = 'notrelix_operator'
    LOOP
        EXECUTE format('REVOKE %I FROM notrelix_operator', m.granted);
    END LOOP;
END $$;

-- 2. Start from nothing: whatever a previous apply granted is removed, then the allow-list is granted again.
DO $$
DECLARE
    s text;
BEGIN
    FOR s IN SELECT nspname FROM pg_namespace WHERE nspname NOT LIKE 'pg\_%' AND nspname NOT IN ('information_schema', 'public')
    LOOP
        EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA %I FROM notrelix_operator', s);
        EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA %I FROM notrelix_operator', s);
        EXECUTE format('REVOKE ALL ON ALL FUNCTIONS IN SCHEMA %I FROM notrelix_operator', s);
        EXECUTE format('REVOKE ALL ON SCHEMA %I FROM notrelix_operator', s);
    END LOOP;
END $$;

-- 3. The allow-list. Account identity and lifecycle status (never its members, workspaces or content), the
-- commercial billing tables the operator edits, the operator's own accounts and audit trail, and insert-only
-- access to the two internal tables a domain event is written to, so an operator change publishes its event
-- in the same transaction like every other change. Each grant is made only for a table that exists, so the pack
-- still applies to a database that has not yet received the operator migration (it is applied again after it).
DO $$
DECLARE
    g record;
BEGIN
    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'operators') THEN
        GRANT USAGE ON SCHEMA operators TO notrelix_operator;
    END IF;
    GRANT USAGE ON SCHEMA account, billing, events, messaging TO notrelix_operator;

    FOR g IN
        SELECT * FROM (VALUES
            ('account', 'accounts', 'SELECT'),
            ('account', 'accounts', 'UPDATE (status, updated_at, updated_by, version)'),
            ('billing', 'plans', 'SELECT, INSERT, UPDATE'),
            ('billing', 'plan_limits', 'SELECT, INSERT, UPDATE'),
            ('billing', 'plan_prices', 'SELECT, INSERT, UPDATE'),
            ('billing', 'subscriptions', 'SELECT, INSERT, UPDATE'),
            ('billing', 'entitlements', 'SELECT, INSERT, UPDATE'),
            ('operators', 'operator_accounts', 'SELECT'),
            ('operators', 'operator_accounts', 'UPDATE (failed_sign_in_count, locked_until, last_signed_in_at, updated_at, updated_by, version)'),
            ('operators', 'operator_audit', 'SELECT, INSERT'),
            ('events', 'domain_event_logs', 'INSERT'),
            ('messaging', 'outbox_messages', 'INSERT')
        ) AS t(schema_name, table_name, privileges)
    LOOP
        IF ops.table_exists(g.schema_name, g.table_name) THEN
            EXECUTE format('GRANT %s ON %I.%I TO notrelix_operator', g.privileges, g.schema_name, g.table_name);
        END IF;
    END LOOP;
END $$;

-- 4. The impact preview: two counts, never a row. Owner-defined, search_path pinned, no dynamic SQL in the body.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'operators') THEN
        EXECUTE $fn$
            CREATE OR REPLACE FUNCTION operators.account_summary(p_account uuid)
            RETURNS TABLE (active_members integer, workspaces integer)
            LANGUAGE sql
            STABLE
            SECURITY DEFINER
            SET search_path = pg_catalog, pg_temp
            AS $body$
                SELECT
                    (SELECT COUNT(*)::integer FROM account.account_members m
                      WHERE m.account_id = p_account AND m.status = 'Active' AND m.deleted_at IS NULL),
                    (SELECT COUNT(*)::integer FROM workspace.workspaces w
                      WHERE w.account_id = p_account AND w.deleted_at IS NULL)
            $body$
        $fn$;
        REVOKE ALL ON FUNCTION operators.account_summary(uuid) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION operators.account_summary(uuid) TO notrelix_operator;
    END IF;
END $$;

-- 5. Row-level security, forced, with policies bound to the operator role. The operator tables are reachable
-- by that role only (no other runtime login holds a grant and no policy admits another role).
DO $$
BEGIN
    IF ops.table_exists('operators', 'operator_accounts') THEN
        PERFORM ops.enable_rls_for_table('operators', 'operator_accounts');
        PERFORM ops.create_policy('operators', 'operator_accounts', 'p_operator_select', 'SELECT', 'notrelix_operator', 'true', NULL);
        PERFORM ops.create_policy('operators', 'operator_accounts', 'p_operator_update', 'UPDATE', 'notrelix_operator', 'true', 'true');
    END IF;
    IF ops.table_exists('operators', 'operator_audit') THEN
        PERFORM ops.enable_rls_for_table('operators', 'operator_audit');
        PERFORM ops.create_policy('operators', 'operator_audit', 'p_operator_select', 'SELECT', 'notrelix_operator', 'true', NULL);
        PERFORM ops.create_policy('operators', 'operator_audit', 'p_operator_insert', 'INSERT', 'notrelix_operator', NULL, 'true');
    END IF;
END $$;

-- Existing tables: add the operator's policies next to the tenant ones (the earlier scripts drop and recreate
-- every policy of these tables, so this script, applied last, restores them).
SELECT ops.create_policy('account', 'accounts', 'p_operator_select', 'SELECT', 'notrelix_operator', 'true', NULL);
SELECT ops.create_policy('account', 'accounts', 'p_operator_update', 'UPDATE', 'notrelix_operator', 'true', 'true');

DO $$
DECLARE
    t text;
BEGIN
    FOREACH t IN ARRAY ARRAY['plans', 'plan_limits', 'plan_prices', 'subscriptions', 'entitlements']
    LOOP
        PERFORM ops.create_policy('billing', t, 'p_operator_select', 'SELECT', 'notrelix_operator', 'true', NULL);
        PERFORM ops.create_policy('billing', t, 'p_operator_insert', 'INSERT', 'notrelix_operator', NULL, 'true');
        PERFORM ops.create_policy('billing', t, 'p_operator_update', 'UPDATE', 'notrelix_operator', 'true', 'true');
    END LOOP;
END $$;

SELECT ops.create_policy('events', 'domain_event_logs', 'p_operator_insert', 'INSERT', 'notrelix_operator', NULL, 'true');
SELECT ops.create_policy('messaging', 'outbox_messages', 'p_operator_insert', 'INSERT', 'notrelix_operator', NULL, 'true');

-- 6. The operator's records are never touched by the tenant logins: no schema usage, no grant, no policy.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'operators') THEN
        REVOKE ALL ON SCHEMA operators FROM PUBLIC, notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
    END IF;
END $$;

-- The tenant logins also lose write access to subscriptions: the plan of an account is the operator's to change
-- (the development plan switch that needed it is gone). Reads stay: a tenant sees its own plan.
DO $$
DECLARE
    p text;
BEGIN
    IF ops.table_exists('billing', 'subscriptions') THEN
        EXECUTE 'REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON billing.subscriptions FROM notrelix_app, notrelix_auth';
        FOREACH p IN ARRAY ARRAY['p_app_insert', 'p_app_update', 'p_app_delete']
        LOOP
            EXECUTE format('DROP POLICY IF EXISTS %I ON billing.subscriptions', p);
        END LOOP;
    END IF;
END $$;

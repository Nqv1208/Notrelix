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

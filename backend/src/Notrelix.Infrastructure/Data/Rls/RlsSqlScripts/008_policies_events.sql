-- =============================================================================
-- 008_policies_events.sql — Events schema RLS policies
-- =============================================================================
-- events.domain_event_logs is internal business fact log.
-- App/auth/worker may INSERT. App must NOT SELECT. Worker/support read.
-- =============================================================================

ALTER TABLE events.domain_event_logs ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_domain_event_logs_insert_app ON events.domain_event_logs;
CREATE POLICY p_domain_event_logs_insert_app ON events.domain_event_logs
    FOR INSERT TO notrelix_app, notrelix_auth, notrelix_worker
    WITH CHECK (
        workspace_id IS NULL
        OR (workspace_id = ops.current_workspace_id()
            AND authz.current_user_has_workspace_access(workspace_id))
        OR ops.current_request_scope() IN ('worker', 'system')
    );

DROP POLICY IF EXISTS p_domain_event_logs_worker_read ON events.domain_event_logs;
CREATE POLICY p_domain_event_logs_worker_read ON events.domain_event_logs
    FOR SELECT TO notrelix_worker
    USING (true);

DROP POLICY IF EXISTS p_domain_event_logs_support_read ON events.domain_event_logs;
CREATE POLICY p_domain_event_logs_support_read ON events.domain_event_logs
    FOR SELECT TO notrelix_support_readonly
    USING (true);

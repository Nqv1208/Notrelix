-- =============================================================================
-- 012_policies_audit.sql — Audit schema RLS policies
-- =============================================================================
-- audit.audit_logs, audit.security_events — compliance/security.
-- App can INSERT (via interceptor) but NOT SELECT. Support reads. Worker ALL.
-- =============================================================================

ALTER TABLE audit.audit_logs ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_audit_logs_insert_app ON audit.audit_logs;
CREATE POLICY p_audit_logs_insert_app ON audit.audit_logs
    FOR INSERT TO notrelix_app, notrelix_auth, notrelix_worker
    WITH CHECK (
        workspace_id IS NULL
        OR workspace_id = ops.current_workspace_id()
        OR ops.current_request_scope() IN ('worker', 'system', 'auth')
    );

DROP POLICY IF EXISTS p_audit_logs_worker_all ON audit.audit_logs;
CREATE POLICY p_audit_logs_worker_all ON audit.audit_logs
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_audit_logs_support_read ON audit.audit_logs;
CREATE POLICY p_audit_logs_support_read ON audit.audit_logs
    FOR SELECT TO notrelix_support_readonly
    USING (true);

ALTER TABLE audit.security_events ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_security_events_insert_app ON audit.security_events;
CREATE POLICY p_security_events_insert_app ON audit.security_events
    FOR INSERT TO notrelix_app, notrelix_auth, notrelix_worker
    WITH CHECK (
        workspace_id IS NULL
        OR workspace_id = ops.current_workspace_id()
        OR ops.current_request_scope() IN ('worker', 'system', 'auth')
    );

DROP POLICY IF EXISTS p_security_events_worker_all ON audit.security_events;
CREATE POLICY p_security_events_worker_all ON audit.security_events
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_security_events_support_read ON audit.security_events;
CREATE POLICY p_security_events_support_read ON audit.security_events
    FOR SELECT TO notrelix_support_readonly
    USING (true);

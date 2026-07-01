-- =============================================================================
-- 009_policies_messaging.sql — Messaging schema RLS policies
-- =============================================================================
-- Outbox/inbox are internal queues. App must NOT SELECT/UPDATE.
-- Worker has ALL. Support reads.
-- =============================================================================

-- messaging.outbox_messages
ALTER TABLE messaging.outbox_messages ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_outbox_insert_app ON messaging.outbox_messages;
CREATE POLICY p_outbox_insert_app ON messaging.outbox_messages
    FOR INSERT TO notrelix_app, notrelix_auth, notrelix_worker
    WITH CHECK (
        workspace_id IS NULL
        OR (workspace_id = ops.current_workspace_id()
            AND authz.current_user_has_workspace_access(workspace_id))
        OR ops.current_request_scope() IN ('worker', 'system')
    );

DROP POLICY IF EXISTS p_outbox_worker_all ON messaging.outbox_messages;
CREATE POLICY p_outbox_worker_all ON messaging.outbox_messages
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_outbox_support_read ON messaging.outbox_messages;
CREATE POLICY p_outbox_support_read ON messaging.outbox_messages
    FOR SELECT TO notrelix_support_readonly
    USING (true);

-- messaging.outbox_delivery_attempts
ALTER TABLE messaging.outbox_delivery_attempts ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_outbox_attempts_worker_all ON messaging.outbox_delivery_attempts;
CREATE POLICY p_outbox_attempts_worker_all ON messaging.outbox_delivery_attempts
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_outbox_attempts_support_read ON messaging.outbox_delivery_attempts;
CREATE POLICY p_outbox_attempts_support_read ON messaging.outbox_delivery_attempts
    FOR SELECT TO notrelix_support_readonly
    USING (true);

-- messaging.processed_events
ALTER TABLE messaging.processed_events ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_processed_events_worker_all ON messaging.processed_events;
CREATE POLICY p_processed_events_worker_all ON messaging.processed_events
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_processed_events_support_read ON messaging.processed_events;
CREATE POLICY p_processed_events_support_read ON messaging.processed_events
    FOR SELECT TO notrelix_support_readonly
    USING (true);

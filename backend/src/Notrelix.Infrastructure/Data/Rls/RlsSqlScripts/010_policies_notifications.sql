-- =============================================================================
-- 010_policies_notifications.sql — Notifications schema RLS policies
-- =============================================================================
-- User-facing: notification_items, notification_recipients, preferences, counters
-- Internal: email_outbox, email_delivery_attempts, notification_deliveries
-- =============================================================================

-- Helper: check if current user is a recipient of a notification
CREATE OR REPLACE FUNCTION notifications.current_user_is_notification_recipient(p_notification_id uuid)
RETURNS boolean AS $$
BEGIN
    IF p_notification_id IS NULL OR ops.current_user_id() IS NULL THEN
        RETURN false;
    END IF;
    RETURN EXISTS (
        SELECT 1 FROM notifications.notification_recipients r
        WHERE r.notification_id = p_notification_id
          AND r.recipient_user_id = ops.current_user_id()
    );
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

GRANT EXECUTE ON FUNCTION notifications.current_user_is_notification_recipient(uuid) TO notrelix_app;

-- notifications.notification_items
ALTER TABLE notifications.notification_items ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_notification_items_select_recipient_app ON notifications.notification_items;
CREATE POLICY p_notification_items_select_recipient_app ON notifications.notification_items
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
        AND notifications.current_user_is_notification_recipient(id)
        AND deleted_at IS NULL
    );

DROP POLICY IF EXISTS p_notification_items_worker_all ON notifications.notification_items;
CREATE POLICY p_notification_items_worker_all ON notifications.notification_items
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_notification_items_support_read ON notifications.notification_items;
CREATE POLICY p_notification_items_support_read ON notifications.notification_items
    FOR SELECT TO notrelix_support_readonly
    USING (true);

-- notifications.notification_recipients
ALTER TABLE notifications.notification_recipients ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_notification_recipients_select_self_app ON notifications.notification_recipients;
CREATE POLICY p_notification_recipients_select_self_app ON notifications.notification_recipients
    FOR SELECT TO notrelix_app
    USING (
        recipient_user_id = ops.current_user_id()
        AND workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_notification_recipients_update_self_app ON notifications.notification_recipients;
CREATE POLICY p_notification_recipients_update_self_app ON notifications.notification_recipients
    FOR UPDATE TO notrelix_app
    USING (
        recipient_user_id = ops.current_user_id()
        AND workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    )
    WITH CHECK (
        recipient_user_id = ops.current_user_id()
        AND workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_notification_recipients_worker_all ON notifications.notification_recipients;
CREATE POLICY p_notification_recipients_worker_all ON notifications.notification_recipients
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- notifications.notification_preferences
ALTER TABLE notifications.notification_preferences ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_notification_preferences_select_self_app ON notifications.notification_preferences;
CREATE POLICY p_notification_preferences_select_self_app ON notifications.notification_preferences
    FOR SELECT TO notrelix_app
    USING (
        user_id = ops.current_user_id()
        AND (workspace_id IS NULL OR workspace_id = ops.current_workspace_id())
    );

DROP POLICY IF EXISTS p_notification_preferences_worker_all ON notifications.notification_preferences;
CREATE POLICY p_notification_preferences_worker_all ON notifications.notification_preferences
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- notifications.notification_counters
ALTER TABLE notifications.notification_counters ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_notification_counters_select_self_app ON notifications.notification_counters;
CREATE POLICY p_notification_counters_select_self_app ON notifications.notification_counters
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_notification_counters_insert_app ON notifications.notification_counters;
CREATE POLICY p_notification_counters_insert_app ON notifications.notification_counters
    FOR INSERT TO notrelix_app
    WITH CHECK (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_notification_counters_update_app ON notifications.notification_counters;
CREATE POLICY p_notification_counters_update_app ON notifications.notification_counters
    FOR UPDATE TO notrelix_app
    USING (user_id = ops.current_user_id())
    WITH CHECK (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_notification_counters_worker_all ON notifications.notification_counters;
CREATE POLICY p_notification_counters_worker_all ON notifications.notification_counters
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- collab.resource_read_states
ALTER TABLE collab.resource_read_states ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_resource_read_states_select_self_app ON collab.resource_read_states;
CREATE POLICY p_resource_read_states_select_self_app ON collab.resource_read_states
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id() AND workspace_id = ops.current_workspace_id());

DROP POLICY IF EXISTS p_resource_read_states_insert_app ON collab.resource_read_states;
CREATE POLICY p_resource_read_states_insert_app ON collab.resource_read_states
    FOR INSERT TO notrelix_app
    WITH CHECK (user_id = ops.current_user_id() AND workspace_id = ops.current_workspace_id());

DROP POLICY IF EXISTS p_resource_read_states_update_app ON collab.resource_read_states;
CREATE POLICY p_resource_read_states_update_app ON collab.resource_read_states
    FOR UPDATE TO notrelix_app
    USING (user_id = ops.current_user_id() AND workspace_id = ops.current_workspace_id())
    WITH CHECK (user_id = ops.current_user_id() AND workspace_id = ops.current_workspace_id());

DROP POLICY IF EXISTS p_resource_read_states_worker_all ON collab.resource_read_states;
CREATE POLICY p_resource_read_states_worker_all ON collab.resource_read_states
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- collab.notification_deliveries (legacy table, not canonical notifications schema)
ALTER TABLE collab.notification_deliveries ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_notification_deliveries_worker_all ON collab.notification_deliveries;
CREATE POLICY p_notification_deliveries_worker_all ON collab.notification_deliveries
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_notification_deliveries_support_read ON collab.notification_deliveries;
CREATE POLICY p_notification_deliveries_support_read ON collab.notification_deliveries
    FOR SELECT TO notrelix_support_readonly
    USING (true);

-- notifications.email_outbox (internal — app must NOT read)
ALTER TABLE notifications.email_outbox ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_email_outbox_insert_app ON notifications.email_outbox;
CREATE POLICY p_email_outbox_insert_app ON notifications.email_outbox
    FOR INSERT TO notrelix_app, notrelix_auth, notrelix_worker
    WITH CHECK (
        workspace_id IS NULL
        OR (workspace_id = ops.current_workspace_id()
            AND authz.current_user_has_workspace_access(workspace_id))
        OR ops.current_request_scope() IN ('worker', 'system')
    );

DROP POLICY IF EXISTS p_email_outbox_worker_all ON notifications.email_outbox;
CREATE POLICY p_email_outbox_worker_all ON notifications.email_outbox
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_email_outbox_support_read ON notifications.email_outbox;
CREATE POLICY p_email_outbox_support_read ON notifications.email_outbox
    FOR SELECT TO notrelix_support_readonly
    USING (true);

-- notifications.email_delivery_attempts (internal)
ALTER TABLE notifications.email_delivery_attempts ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_email_delivery_attempts_worker_all ON notifications.email_delivery_attempts;
CREATE POLICY p_email_delivery_attempts_worker_all ON notifications.email_delivery_attempts
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_email_delivery_attempts_support_read ON notifications.email_delivery_attempts;
CREATE POLICY p_email_delivery_attempts_support_read ON notifications.email_delivery_attempts
    FOR SELECT TO notrelix_support_readonly
    USING (true);

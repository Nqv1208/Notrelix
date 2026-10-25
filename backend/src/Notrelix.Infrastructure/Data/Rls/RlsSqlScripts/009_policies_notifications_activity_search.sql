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
        -- A notification belongs to its recipients (Collaboration): the application login reads an item only
        -- through its own recipient row. There is no workspace-wide read branch; background readers run on the
        -- worker login (p_worker_all).
        PERFORM ops.create_policy(
            'notifications',
            'notification_items',
            'p_app_select_recipient',
            'SELECT',
            'notrelix_app',
            'EXISTS (
                SELECT 1
                FROM notifications.notification_recipients nr
                WHERE nr.notification_id = notification_items.id
                  AND nr.recipient_user_id = ops.current_user_id()
            )',
            NULL
        );
        PERFORM ops.create_policy('notifications', 'notification_items', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('notifications', 'notification_items', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;

SELECT ops.apply_user_owned_policies('notifications', 'notification_preferences', 'user_id', false, true);
SELECT ops.apply_user_owned_policies('notifications', 'notification_preference_sets', 'user_id', false, true);
SELECT ops.apply_user_owned_policies('notifications', 'notification_counters', 'user_id', false, true);
-- A thread mute is the muting member's own; producers read it on the worker login (p_worker_all).
SELECT ops.apply_user_owned_policies('notifications', 'notification_thread_mutes', 'user_id', false, true);
SELECT ops.apply_worker_internal_policies('notifications', 'notification_deliveries', true);
SELECT ops.apply_worker_internal_policies('notifications', 'email_outbox', true);
SELECT ops.apply_worker_internal_policies('notifications', 'email_delivery_attempts', false);
SELECT ops.apply_readonly_projection_policies('activity', 'workspace_activity_logs');
SELECT ops.apply_user_owned_policies('activity', 'activity_read_states', 'user_id', false, true);
SELECT ops.apply_readonly_projection_policies('analytics', 'workspace_usage_daily');
SELECT ops.apply_readonly_projection_policies('analytics', 'feature_usage_daily');

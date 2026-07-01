-- =============================================================================
-- 011_policies_activity.sql — Activity projection RLS policies
-- =============================================================================
-- activity.workspace_activity_logs is user-facing workspace feed.
-- App can SELECT only. Worker ALL. Support read. App cannot INSERT.
-- =============================================================================

ALTER TABLE activity.workspace_activity_logs ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_activity_logs_select_app ON activity.workspace_activity_logs;
CREATE POLICY p_activity_logs_select_app ON activity.workspace_activity_logs
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_activity_logs_worker_all ON activity.workspace_activity_logs;
CREATE POLICY p_activity_logs_worker_all ON activity.workspace_activity_logs
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_activity_logs_support_read ON activity.workspace_activity_logs;
CREATE POLICY p_activity_logs_support_read ON activity.workspace_activity_logs
    FOR SELECT TO notrelix_support_readonly
    USING (true);

-- activity.activity_read_states
ALTER TABLE activity.activity_read_states ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_activity_read_states_select_self_app ON activity.activity_read_states;
CREATE POLICY p_activity_read_states_select_self_app ON activity.activity_read_states
    FOR SELECT TO notrelix_app
    USING (
        user_id = ops.current_user_id()
        AND workspace_id = ops.current_workspace_id()
    );

DROP POLICY IF EXISTS p_activity_read_states_worker_all ON activity.activity_read_states;
CREATE POLICY p_activity_read_states_worker_all ON activity.activity_read_states
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- =============================================================================
-- 013_policies_projection.sql — Search, reporting, analytics RLS policies
-- =============================================================================
-- Projection/read models: app SELECT by workspace, worker ALL for rebuild.
-- =============================================================================

-- search.search_documents
ALTER TABLE search.search_documents ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_search_documents_select_app ON search.search_documents;
CREATE POLICY p_search_documents_select_app ON search.search_documents
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_search_documents_worker_all ON search.search_documents;
CREATE POLICY p_search_documents_worker_all ON search.search_documents
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- search.search_index_jobs
ALTER TABLE search.search_index_jobs ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_search_index_jobs_worker_all ON search.search_index_jobs;
CREATE POLICY p_search_index_jobs_worker_all ON search.search_index_jobs
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- reporting.dashboards
ALTER TABLE reporting.dashboards ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_reporting_dashboards_select_app ON reporting.dashboards;
CREATE POLICY p_reporting_dashboards_select_app ON reporting.dashboards
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
        AND deleted_at IS NULL
    );

DROP POLICY IF EXISTS p_reporting_dashboards_worker_all ON reporting.dashboards;
CREATE POLICY p_reporting_dashboards_worker_all ON reporting.dashboards
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- reporting.dashboard_widgets
ALTER TABLE reporting.dashboard_widgets ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_reporting_widgets_select_app ON reporting.dashboard_widgets;
CREATE POLICY p_reporting_widgets_select_app ON reporting.dashboard_widgets
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_reporting_widgets_worker_all ON reporting.dashboard_widgets;
CREATE POLICY p_reporting_widgets_worker_all ON reporting.dashboard_widgets
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- analytics.workspace_usage_daily
ALTER TABLE analytics.workspace_usage_daily ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_workspace_usage_daily_select_app ON analytics.workspace_usage_daily;
CREATE POLICY p_workspace_usage_daily_select_app ON analytics.workspace_usage_daily
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_workspace_usage_daily_worker_all ON analytics.workspace_usage_daily;
CREATE POLICY p_workspace_usage_daily_worker_all ON analytics.workspace_usage_daily
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- analytics.feature_usage_daily
ALTER TABLE analytics.feature_usage_daily ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_feature_usage_daily_select_app ON analytics.feature_usage_daily;
CREATE POLICY p_feature_usage_daily_select_app ON analytics.feature_usage_daily
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_feature_usage_daily_worker_all ON analytics.feature_usage_daily;
CREATE POLICY p_feature_usage_daily_worker_all ON analytics.feature_usage_daily
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

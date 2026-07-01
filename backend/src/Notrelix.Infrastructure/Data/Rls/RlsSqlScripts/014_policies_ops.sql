-- =============================================================================
-- 014_policies_ops.sql — Ops schema RLS policies
-- =============================================================================
-- No blanket policy. Each table has specific role-based policies.
-- =============================================================================

-- ops.idempotency_keys
ALTER TABLE ops.idempotency_keys ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_idempotency_keys_app_user ON ops.idempotency_keys;
CREATE POLICY p_idempotency_keys_app_user ON ops.idempotency_keys
    FOR ALL TO notrelix_app, notrelix_auth
    USING (
        user_id IS NULL
        OR user_id = ops.current_user_id()
    )
    WITH CHECK (
        user_id IS NULL
        OR user_id = ops.current_user_id()
    );

DROP POLICY IF EXISTS p_idempotency_keys_worker_all ON ops.idempotency_keys;
CREATE POLICY p_idempotency_keys_worker_all ON ops.idempotency_keys
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- ops.job_locks (worker only)
ALTER TABLE ops.job_locks ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_job_locks_worker_all ON ops.job_locks;
CREATE POLICY p_job_locks_worker_all ON ops.job_locks
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- ops.import_jobs
ALTER TABLE ops.import_jobs ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_import_jobs_select_app ON ops.import_jobs;
CREATE POLICY p_import_jobs_select_app ON ops.import_jobs
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_import_jobs_insert_app ON ops.import_jobs;
CREATE POLICY p_import_jobs_insert_app ON ops.import_jobs
    FOR INSERT TO notrelix_app
    WITH CHECK (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_import_jobs_worker_all ON ops.import_jobs;
CREATE POLICY p_import_jobs_worker_all ON ops.import_jobs
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

-- ops.export_jobs
ALTER TABLE ops.export_jobs ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_export_jobs_select_app ON ops.export_jobs;
CREATE POLICY p_export_jobs_select_app ON ops.export_jobs
    FOR SELECT TO notrelix_app
    USING (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_export_jobs_insert_app ON ops.export_jobs;
CREATE POLICY p_export_jobs_insert_app ON ops.export_jobs
    FOR INSERT TO notrelix_app
    WITH CHECK (
        workspace_id = ops.current_workspace_id()
        AND authz.current_user_has_workspace_access(workspace_id)
    );

DROP POLICY IF EXISTS p_export_jobs_worker_all ON ops.export_jobs;
CREATE POLICY p_export_jobs_worker_all ON ops.export_jobs
    FOR ALL TO notrelix_worker
    USING (true) WITH CHECK (true);

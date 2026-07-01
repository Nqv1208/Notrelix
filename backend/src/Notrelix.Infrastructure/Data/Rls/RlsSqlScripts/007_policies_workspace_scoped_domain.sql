-- =============================================================================
-- 007_policies_workspace_scoped_domain.sql
-- =============================================================================
-- Policies for work.*, docs.*, collab.*, automation.*, integration.*,
-- billing.*, reporting.*, search.* tables.
-- Pattern: SELECT/INSERT/UPDATE by workspace access, Worker ALL, soft-delete filter.
-- =============================================================================

DO $$
DECLARE
    sch text;
    tbl text;
    has_soft_delete boolean;
    workspace_schemas text[] := ARRAY[
        'work','docs','collab','automation','integration',
        'billing','reporting','search'
    ];
    exclude_tables text[] := ARRAY[
        'workspace_usage_daily','feature_usage_daily'
    ];
BEGIN
    FOREACH sch IN ARRAY workspace_schemas
    LOOP
        IF NOT EXISTS(SELECT 1 FROM information_schema.schemata s WHERE s.schema_name = sch) THEN
            CONTINUE;
        END IF;

        FOR tbl IN
            SELECT c.table_name FROM information_schema.columns c
            WHERE c.table_schema = sch
              AND c.column_name = 'workspace_id'
              AND c.table_name != ALL(exclude_tables)
            ORDER BY c.table_name
        LOOP
            EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY', sch, tbl);

            -- Drop old blanket policy if exists
            EXECUTE format('DROP POLICY IF EXISTS workspace_access ON %I.%I', sch, tbl);

            -- Check if table has deleted_at column
            has_soft_delete := EXISTS(
                SELECT 1 FROM information_schema.columns c
                WHERE c.table_schema = sch
                  AND c.table_name = tbl
                  AND c.column_name = 'deleted_at'
            );

            -- SELECT: workspace match + authz access + soft delete (if column exists)
            EXECUTE format(
                'DROP POLICY IF EXISTS p_%s_%s_select_app ON %I.%I', sch, tbl, sch, tbl);
            IF has_soft_delete THEN
                EXECUTE format(
                    'CREATE POLICY p_%s_%s_select_app ON %I.%I
                     FOR SELECT TO notrelix_app
                     USING (
                        workspace_id = ops.current_workspace_id()
                        AND authz.current_user_has_workspace_access(workspace_id)
                        AND deleted_at IS NULL
                     )', sch, tbl, sch, tbl);
            ELSE
                EXECUTE format(
                    'CREATE POLICY p_%s_%s_select_app ON %I.%I
                     FOR SELECT TO notrelix_app
                     USING (
                        workspace_id = ops.current_workspace_id()
                        AND authz.current_user_has_workspace_access(workspace_id)
                     )', sch, tbl, sch, tbl);
            END IF;

            -- INSERT: workspace match + authz access
            EXECUTE format(
                'DROP POLICY IF EXISTS p_%s_%s_insert_app ON %I.%I', sch, tbl, sch, tbl);
            EXECUTE format(
                'CREATE POLICY p_%s_%s_insert_app ON %I.%I
                 FOR INSERT TO notrelix_app
                 WITH CHECK (
                    workspace_id = ops.current_workspace_id()
                    AND authz.current_user_has_workspace_access(workspace_id)
                 )', sch, tbl, sch, tbl);

            -- UPDATE: workspace match + authz access
            EXECUTE format(
                'DROP POLICY IF EXISTS p_%s_%s_update_app ON %I.%I', sch, tbl, sch, tbl);
            EXECUTE format(
                'CREATE POLICY p_%s_%s_update_app ON %I.%I
                 FOR UPDATE TO notrelix_app
                 USING (
                    workspace_id = ops.current_workspace_id()
                    AND authz.current_user_has_workspace_access(workspace_id)
                 )
                 WITH CHECK (
                    workspace_id = ops.current_workspace_id()
                    AND authz.current_user_has_workspace_access(workspace_id)
                 )', sch, tbl, sch, tbl);

            -- Worker: ALL
            EXECUTE format(
                'DROP POLICY IF EXISTS p_%s_%s_worker_all ON %I.%I', sch, tbl, sch, tbl);
            EXECUTE format(
                'CREATE POLICY p_%s_%s_worker_all ON %I.%I
                 FOR ALL TO notrelix_worker
                 USING (true) WITH CHECK (true)', sch, tbl, sch, tbl);

            -- Support: read
            EXECUTE format(
                'DROP POLICY IF EXISTS p_%s_%s_support_read ON %I.%I', sch, tbl, sch, tbl);
            EXECUTE format(
                'CREATE POLICY p_%s_%s_support_read ON %I.%I
                 FOR SELECT TO notrelix_support_readonly
                 USING (true)', sch, tbl, sch, tbl);
        END LOOP;
    END LOOP;
END $$;

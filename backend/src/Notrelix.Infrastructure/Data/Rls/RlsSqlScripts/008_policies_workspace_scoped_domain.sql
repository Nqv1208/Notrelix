-- =============================================================================
-- 008_policies_workspace_scoped_domain.sql — Product/domain schema policies
-- =============================================================================
SELECT ops.apply_scoped_business_policies('work', 'boards', true);
SELECT ops.apply_scoped_business_policies('work', 'board_groups', true);
SELECT ops.apply_scoped_business_policies('work', 'board_fields', true);
SELECT ops.apply_scoped_business_policies('work', 'board_items', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_members', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_reminders', true);
SELECT ops.apply_scoped_business_policies('work', 'labels', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_labels', true);
SELECT ops.apply_scoped_business_policies('work', 'board_views', true);
SELECT ops.apply_scoped_business_policies('work', 'board_view_user_preferences', true);
SELECT ops.apply_scoped_business_policies('work', 'saved_filters', true);
SELECT ops.apply_scoped_business_policies('work', 'board_view_pins', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_links', true);
SELECT ops.apply_scoped_business_policies('work', 'checklists', true);
SELECT ops.apply_scoped_business_policies('work', 'board_relations', true);
SELECT ops.apply_scoped_business_policies('work', 'board_item_connections', true);
SELECT ops.apply_scoped_business_policies('work', 'formula_dependencies', true);
SELECT ops.apply_scoped_business_policies('work', 'mirror_value_snapshots', true);
SELECT ops.apply_scoped_business_policies('work', 'rollup_snapshots', true);
SELECT ops.apply_scoped_business_policies('work', 'approval_requests', true);
SELECT ops.apply_scoped_business_policies('work', 'workload_allocations', true);
SELECT ops.apply_scoped_business_policies('work', 'board_templates', true);
SELECT ops.apply_scoped_business_policies('work', 'item_templates', true);
SELECT ops.apply_scoped_business_policies('work', 'board_subscribers', true);
SELECT ops.apply_scoped_business_policies('work', 'item_dependencies', true);
SELECT ops.apply_scoped_business_policies('work', 'time_tracking_entries', true);
SELECT ops.apply_scoped_business_policies('work', 'forms', true);
SELECT ops.apply_scoped_business_policies('work', 'form_questions', true);
SELECT ops.apply_scoped_business_policies('work', 'form_submissions', true);

-- Work tables without tenant columns derive their scope from the parent row
-- (WM-V3-DATA-003). The generic helper above skips them because it can only
-- read account_id/workspace_id from the table itself.
SELECT ops.apply_scope_expression_policies('work', 'field_options',
    'EXISTS (SELECT 1 FROM work.board_fields f WHERE f.id = field_options.field_id AND ops.has_workspace_access(f.account_id, f.workspace_id))');
SELECT ops.apply_scope_expression_policies('work', 'checklist_items',
    'EXISTS (SELECT 1 FROM work.checklists c WHERE c.id = checklist_items.checklist_id AND ops.has_workspace_access(c.account_id, c.workspace_id))');
SELECT ops.apply_scope_expression_policies('work', 'board_item_values',
    'EXISTS (SELECT 1 FROM work.board_items i JOIN work.board_fields f ON f.board_id = i.board_id WHERE i.id = board_item_values.item_id AND f.id = board_item_values.field_id AND ops.has_workspace_access(i.account_id, i.workspace_id))');
SELECT ops.apply_scope_expression_policies('work', 'board_members',
    'EXISTS (SELECT 1 FROM work.boards b WHERE b.id = board_members.board_id AND ops.has_workspace_access(b.account_id, b.workspace_id))');
SELECT ops.apply_scope_expression_policies('work', 'approval_steps',
    'EXISTS (SELECT 1 FROM work.approval_requests r WHERE r.id = approval_steps.approval_request_id AND ops.has_workspace_access(r.account_id, r.workspace_id))');
SELECT ops.apply_scope_expression_policies('work', 'relation_field_configs',
    'EXISTS (SELECT 1 FROM work.board_fields f WHERE f.id = relation_field_configs.field_id AND ops.has_workspace_access(f.account_id, f.workspace_id))');

SELECT ops.apply_scope_expression_policies('work', 'board_view_filter_rules',
    'EXISTS (SELECT 1 FROM work.board_view_user_preferences p WHERE p.id = board_view_filter_rules.preference_id AND ops.has_workspace_access(p.account_id, p.workspace_id))');
SELECT ops.apply_scope_expression_policies('work', 'board_view_sort_rules',
    'EXISTS (SELECT 1 FROM work.board_view_user_preferences p WHERE p.id = board_view_sort_rules.preference_id AND ops.has_workspace_access(p.account_id, p.workspace_id))');
SELECT ops.apply_scope_expression_policies('work', 'saved_filter_rules',
    'EXISTS (SELECT 1 FROM work.saved_filters f WHERE f.id = saved_filter_rules.saved_filter_id AND ops.has_workspace_access(f.account_id, f.workspace_id))');
SELECT ops.apply_scope_expression_policies('work', 'saved_filter_sort_rules',
    'EXISTS (SELECT 1 FROM work.saved_filters f WHERE f.id = saved_filter_sort_rules.saved_filter_id AND ops.has_workspace_access(f.account_id, f.workspace_id))');

SELECT ops.apply_scoped_business_policies('docs', 'pages', true);
SELECT ops.apply_scoped_business_policies('docs', 'blocks', true);
SELECT ops.apply_scoped_business_policies('docs', 'document_versions', true);
SELECT ops.apply_scoped_business_policies('docs', 'resource_links', true);
SELECT ops.apply_scoped_business_policies('docs', 'page_templates', true);
SELECT ops.apply_scoped_business_policies('collab', 'comments', true);
SELECT ops.apply_scoped_business_policies('collab', 'reactions', true);
SELECT ops.apply_scoped_business_policies('collab', 'mentions', true);
SELECT ops.apply_scoped_business_policies('collab', 'attachments', true);
SELECT ops.apply_scoped_business_policies('collab', 'resource_watchers', true);
SELECT ops.apply_scoped_business_policies('collab', 'presence_sessions', true);
SELECT ops.apply_scoped_business_policies('collab', 'resource_read_states', true);
SELECT ops.apply_scoped_business_policies('automation', 'automation_rules', true);
SELECT ops.apply_scoped_business_policies('automation', 'automation_executions', true);
SELECT ops.apply_scoped_business_policies('automation', 'scheduled_jobs', true);
SELECT ops.apply_scoped_business_policies('automation', 'ai_agents', true);
SELECT ops.apply_scoped_business_policies('automation', 'ai_agent_runs', true);
SELECT ops.apply_scoped_business_policies('integration', 'integration_connections', true);
SELECT ops.apply_scoped_business_policies('integration', 'webhook_subscriptions', true);
SELECT ops.apply_scoped_business_policies('integration', 'webhook_deliveries', true);
SELECT ops.apply_scoped_business_policies('integration', 'inbound_webhook_events', true);
SELECT ops.apply_scoped_business_policies('integration', 'calendar_integrations', true);

-- Anonymous provider webhook bootstrap is a deliberately narrow exception to
-- tenant-scoped app SELECT. The request must set app.webhook_path before the
-- first lookup, then app.webhook_connection_id before the second lookup. It
-- never grants worker/system access and remains safe with FORCE RLS enabled.
DO $$
BEGIN
    IF ops.table_exists('integration', 'calendar_integrations') THEN
        PERFORM ops.create_policy(
            'integration',
            'calendar_integrations',
            'p_app_webhook_bootstrap_select',
            'SELECT',
            'notrelix_app',
            'webhook_path = ops.current_setting_text(''app.webhook_path'')',
            NULL);
    END IF;

    IF ops.table_exists('integration', 'integration_connections') THEN
        PERFORM ops.create_policy(
            'integration',
            'integration_connections',
            'p_app_webhook_connection_select',
            'SELECT',
            'notrelix_app',
            'id = ops.current_setting_uuid(''app.webhook_connection_id'')',
            NULL);
    END IF;
END $$;
SELECT ops.apply_scoped_business_policies('reporting', 'dashboards', true);
SELECT ops.apply_scoped_business_policies('reporting', 'dashboard_widgets', true);
SELECT ops.apply_scoped_business_policies('reporting', 'dashboard_sources', true);
SELECT ops.apply_scoped_business_policies('reporting', 'reporting_snapshots', true);
SELECT ops.apply_scoped_business_policies('reporting', 'workspace_work_item_placements', true);
SELECT ops.apply_scoped_business_policies('billing', 'billing_customers', true);
SELECT ops.apply_catalog_policies('billing', 'plans');
SELECT ops.apply_catalog_policies('billing', 'plan_prices');
SELECT ops.apply_catalog_policies('billing', 'plan_limits');
SELECT ops.apply_scoped_business_policies('billing', 'subscriptions', true);
SELECT ops.apply_scoped_business_policies('billing', 'payment_methods', true);
SELECT ops.apply_scoped_business_policies('billing', 'invoices', true);
SELECT ops.apply_scoped_business_policies('billing', 'entitlements', true);
SELECT ops.apply_scoped_business_policies('billing', 'usage_metrics', true);
SELECT ops.apply_scoped_business_policies('billing', 'feature_usage_ledger', true);
SELECT ops.apply_scoped_business_policies('billing', 'workspace_feature_usages', true);
SELECT ops.apply_worker_internal_policies('billing', 'billing_events', true);

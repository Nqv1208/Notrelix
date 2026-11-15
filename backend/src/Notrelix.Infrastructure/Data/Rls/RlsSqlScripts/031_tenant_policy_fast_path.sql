-- =============================================================================
-- 031_tenant_policy_fast_path.sql — Plain workspace-grant tables: one grant lookup per statement instead of one per row
-- =============================================================================
-- Why: see 030. Every table below has an application-role policy that is exactly
--     ops.has_workspace_access(account_id, workspace_id)
-- which is SECURITY DEFINER, cannot be inlined and costs about 0.12 ms per row a statement touches.
--
-- What: each such policy (SELECT, INSERT, UPDATE, DELETE, whichever the table has) becomes
--     (account_id, workspace_id) IN (SELECT ... FROM ops.accessible_workspace_pairs())   -- hashed, once per statement
--     OR ops.has_workspace_access(account_id, workspace_id)                              -- the original rule, unchanged
-- ops.accessible_workspace_pairs() (030) lists exactly the pairs the grant branch of ops.has_workspace_access accepts,
-- so the first arm can only short-circuit a row the function already accepts. No row becomes visible or writable
-- that was not before; worker scope, invitee scope and NULL arguments still reach the function through the second arm.
--
-- How, and why it is not ops.apply_scope_expression_policies: that helper drops every policy of the table and
-- recreates the standard set, which would restore the support SELECT policy that 020 removed and drop the operator
-- policies of 027 and the extra policies 014-025 added. This script only ALTERs the policy expression, and only of a
-- policy whose current expression is still the plain rule; a policy that is missing or already different (a stricter
-- rule) is left exactly as it is. The apply reruns 008 first on every start, so the plain rule is restored and
-- rewritten each time. A later script that tightens one of these tables must run after this one.
--
-- Not touched (they keep their policy): tables whose rule is anything other than the plain rule: account-only
-- access, (NULL, workspace_id) scope, own-rows-only, admin checks, parent-row EXISTS scopes, and tables with extra
-- application policies (boards, pages, api_tokens, calendar_integrations, integration_connections,
-- inbound_webhook_receipts, workspace_invitations, outbox_messages).
-- =============================================================================

DO $$
DECLARE
    plain constant text := 'ops.has_workspace_access(account_id, workspace_id)';
    fast constant text := '(account_id, workspace_id) IN (SELECT p.account_id, p.workspace_id FROM ops.accessible_workspace_pairs() p) '
                          || 'OR ops.has_workspace_access(account_id, workspace_id)';
    t text;
    pol record;
    clauses text;
BEGIN
    -- Deparsed policy text is schema-qualified only when the schema is off the path; pin it so the comparison is stable.
    PERFORM set_config('search_path', 'pg_catalog, pg_temp', true);

    FOREACH t IN ARRAY ARRAY[
        'account.workspace_routes',
        'automation.ai_agent_runs', 'automation.ai_agents', 'automation.automation_executions',
        'automation.automation_rules', 'automation.scheduled_jobs',
        'billing.entitlements', 'billing.feature_usage_ledger', 'billing.invoices', 'billing.payment_methods',
        'billing.subscriptions', 'billing.usage_metrics', 'billing.workspace_feature_usages',
        'collab.attachment_pending_uploads', 'collab.attachments', 'collab.comments', 'collab.mentions',
        'collab.presence_sessions', 'collab.reactions', 'collab.resource_read_states', 'collab.resource_watchers',
        'docs.blocks', 'docs.document_versions', 'docs.resource_links',
        'governance.custom_role_changes', 'governance.field_permissions', 'governance.permission_rules',
        'governance.permission_templates', 'governance.resource_access_changes', 'governance.resource_permissions',
        'governance.share_links', 'governance.workspace_policies',
        'integration.outbound_delivery_log_entries', 'integration.webhook_deliveries', 'integration.webhook_subscriptions',
        'reporting.dashboard_sources', 'reporting.dashboard_widgets', 'reporting.dashboards',
        'reporting.reporting_snapshots', 'reporting.workspace_work_item_placements',
        'search.search_documents',
        'work.approval_requests', 'work.board_fields', 'work.board_groups', 'work.board_item_connections',
        'work.board_item_key_aliases', 'work.board_item_labels', 'work.board_item_links', 'work.board_item_members',
        'work.board_item_reminders', 'work.board_item_views', 'work.board_item_votes', 'work.board_items',
        'work.board_relations', 'work.board_subscribers', 'work.board_templates', 'work.board_view_pins',
        'work.board_view_user_preferences', 'work.board_views', 'work.checklists', 'work.form_questions',
        'work.form_submissions', 'work.forms', 'work.item_dependencies', 'work.item_templates', 'work.labels',
        'work.mirror_value_snapshots', 'work.saved_filters', 'work.time_tracking_entries', 'work.workload_allocations',
        'workspace.space_members', 'workspace.space_paths', 'workspace.spaces', 'workspace.team_members',
        'workspace.teams', 'workspace.workspace_members'
    ]
    LOOP
        IF to_regclass(t) IS NULL THEN
            RAISE NOTICE 'RLS fast path skipped missing table %', t;
            CONTINUE;
        END IF;

        FOR pol IN
            SELECT p.polname,
                   pg_get_expr(p.polqual, p.polrelid) AS using_expr,
                   pg_get_expr(p.polwithcheck, p.polrelid) AS check_expr
              FROM pg_policy p
             WHERE p.polrelid = to_regclass(t)
               AND p.polname IN ('p_app_select', 'p_app_insert', 'p_app_update', 'p_app_delete')
               AND p.polroles = ARRAY[(SELECT oid FROM pg_roles WHERE rolname = 'notrelix_app')]
        LOOP
            IF (pol.using_expr IS NOT NULL AND pol.using_expr <> plain)
               OR (pol.check_expr IS NOT NULL AND pol.check_expr <> plain)
               OR (pol.using_expr IS NULL AND pol.check_expr IS NULL) THEN
                RAISE NOTICE 'RLS fast path skipped %.%: policy is not the plain workspace rule', t, pol.polname;
                CONTINUE;
            END IF;

            clauses := concat_ws(' ',
                CASE WHEN pol.using_expr IS NOT NULL THEN 'USING (' || fast || ')' END,
                CASE WHEN pol.check_expr IS NOT NULL THEN 'WITH CHECK (' || fast || ')' END);
            EXECUTE format('ALTER POLICY %I ON %s %s', pol.polname, to_regclass(t), clauses);
        END LOOP;
    END LOOP;
END
$$;

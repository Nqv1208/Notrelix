-- =============================================================================
-- 004_grants.sql — Minimal grants by runtime role
-- =============================================================================
-- Revokes PUBLIC access first, then grants only what each role needs.
-- App role must NOT have SELECT on internal tables.
-- =============================================================================

-- Revoke PUBLIC access
REVOKE ALL ON SCHEMA identity, workspace, governance, authz, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops FROM PUBLIC;

REVOKE ALL ON ALL TABLES IN SCHEMA
    identity, workspace, governance, authz, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops
FROM PUBLIC;

-- =============================================================================
-- notrelix_app — Normal authenticated API requests
-- =============================================================================
-- App can read/write workspace-scoped business data.
-- App must NOT read internal tables (events, messaging, email, audit, job_locks).

GRANT USAGE ON SCHEMA identity, workspace, governance, work, docs, collab,
    automation, integration, billing, reporting, search TO notrelix_app;

-- Identity tables — app can read/write own data via RLS
GRANT SELECT, INSERT, UPDATE ON identity.users TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON identity.user_profiles TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON identity.user_sessions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON identity.user_security_settings TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON identity.user_mfa_methods TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON identity.api_tokens TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON identity.oauth_accounts TO notrelix_app;

-- Workspace tables
GRANT SELECT, INSERT, UPDATE ON workspace.workspaces TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON workspace.workspace_members TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON workspace.teams TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON workspace.team_members TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON workspace.spaces TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON workspace.workspace_invitations TO notrelix_app;

-- Governance tables
GRANT SELECT, INSERT, UPDATE ON governance.resource_permissions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.field_permissions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.share_links TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.custom_roles TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.custom_role_permissions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.member_role_assignments TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.permission_templates TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.permission_rules TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.workspace_policies TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.resource_permission_inheritance_cache TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON governance.audit_retention_policies TO notrelix_app;

-- Work tables
GRANT SELECT, INSERT, UPDATE ON work.boards TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_groups TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_fields TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.field_options TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_views TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_items TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_item_values TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_item_members TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.labels TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_item_labels TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_item_links TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.checklists TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.checklist_items TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_view_user_preferences TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.saved_filters TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.relation_field_configs TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.formula_dependencies TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.rollup_snapshots TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.approval_requests TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.approval_steps TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.workload_allocations TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_templates TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.item_templates TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_relations TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_item_connections TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.mirror_value_snapshots TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.item_dependencies TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.time_tracking_entries TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.forms TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.form_questions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.form_submissions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_subscribers TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON work.board_view_pins TO notrelix_app;

-- Docs tables
GRANT SELECT, INSERT, UPDATE ON docs.pages TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON docs.blocks TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON docs.document_versions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON docs.page_templates TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON docs.resource_links TO notrelix_app;

-- Collab tables (source state only, not notification center)
GRANT SELECT, INSERT, UPDATE ON collab.comments TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON collab.mentions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON collab.reactions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON collab.attachments TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON collab.resource_watchers TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON collab.presence_sessions TO notrelix_app;

-- Automation tables
GRANT SELECT, INSERT, UPDATE ON automation.automation_rules TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON automation.automation_executions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON automation.scheduled_jobs TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON automation.automation_templates TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON automation.ai_agents TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON automation.ai_agent_runs TO notrelix_app;

-- Integration tables
GRANT SELECT, INSERT, UPDATE ON integration.integration_connections TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON integration.integration_scopes TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON integration.integration_secret_versions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON integration.webhook_subscriptions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON integration.webhook_deliveries TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON integration.inbound_webhook_events TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON integration.calendar_integrations TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON integration.calendar_event_links TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON integration.integration_sync_cursors TO notrelix_app;

-- Billing tables
GRANT SELECT ON billing.plans TO notrelix_app;
GRANT SELECT ON billing.plan_limits TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.subscriptions TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.entitlements TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.invoices TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.payment_methods TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.billing_events TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.usage_metrics TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.usage_metric_history TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.workspace_feature_usages TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON billing.feature_usage_ledger TO notrelix_app;

-- Reporting tables
GRANT SELECT, INSERT, UPDATE ON reporting.dashboards TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON reporting.dashboard_widgets TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON reporting.dashboard_sources TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON reporting.reporting_snapshots TO notrelix_app;

-- Search tables
GRANT SELECT, INSERT, UPDATE ON search.search_documents TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON search.search_index_jobs TO notrelix_app;

-- Analytics tables
GRANT SELECT ON analytics.workspace_usage_daily TO notrelix_app;
GRANT SELECT ON analytics.feature_usage_daily TO notrelix_app;

-- Notification tables (user-facing only)
GRANT SELECT, INSERT, UPDATE ON notifications.notification_items TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON notifications.notification_recipients TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON notifications.notification_preferences TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON notifications.notification_counters TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON collab.resource_read_states TO notrelix_app;
-- NO SELECT on notifications.email_outbox — internal worker table
-- NO SELECT on notifications.email_delivery_attempts — internal worker table

-- Activity tables (read-only for app)
GRANT SELECT ON activity.workspace_activity_logs TO notrelix_app;
GRANT SELECT ON activity.activity_read_states TO notrelix_app;
-- NO INSERT/UPDATE — app does not write activity directly

-- Ops tables (limited)
GRANT SELECT, INSERT, UPDATE ON ops.idempotency_keys TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON ops.import_jobs TO notrelix_app;
GRANT SELECT, INSERT, UPDATE ON ops.export_jobs TO notrelix_app;
-- NO SELECT on ops.job_locks — worker only

-- Internal tables — NO access for app
-- events.domain_event_logs        — NO
-- messaging.outbox_messages       — NO
-- messaging.outbox_delivery_attempts — NO
-- messaging.processed_events      — NO
-- notifications.email_outbox      — NO
-- notifications.email_delivery_attempts — NO
-- audit.audit_logs                — NO
-- audit.security_events           — NO
-- audit.activity_logs             — NO (compatibility only)
-- ops.job_locks                   — NO

-- Sequences
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA
    identity, workspace, governance, work, docs, collab,
    automation, integration, billing, reporting, search TO notrelix_app;

-- =============================================================================
-- notrelix_auth — Register/login/refresh-token flows
-- =============================================================================
GRANT USAGE ON SCHEMA identity, messaging, events TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.users TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.user_profiles TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.user_sessions TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.user_security_settings TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.user_mfa_methods TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.oauth_accounts TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.email_verification_tokens TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.password_reset_tokens TO notrelix_auth;
GRANT SELECT, INSERT, UPDATE ON identity.user_login_attempts TO notrelix_auth;
GRANT INSERT ON events.domain_event_logs TO notrelix_auth;
GRANT INSERT ON messaging.outbox_messages TO notrelix_auth;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA identity, messaging, events TO notrelix_auth;

-- =============================================================================
-- notrelix_worker — Background workers, dispatchers, consumers
-- =============================================================================
GRANT USAGE ON SCHEMA identity, workspace, governance, authz, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops TO notrelix_worker;

-- Worker has full access to all tables (RLS handles workspace isolation)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA
    identity, workspace, governance, authz, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops TO notrelix_worker;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA
    identity, workspace, governance, authz, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops TO notrelix_worker;

-- =============================================================================
-- notrelix_support_readonly — Controlled support/debug read-only
-- =============================================================================
GRANT USAGE ON SCHEMA identity, workspace, governance, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops TO notrelix_support_readonly;

-- Support can read approved tables only
GRANT SELECT ON ALL TABLES IN SCHEMA
    identity, workspace, governance, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops TO notrelix_support_readonly;

-- =============================================================================
-- notrelix_migrator — Schema migration, seed bootstrap
-- =============================================================================
GRANT USAGE ON SCHEMA identity, workspace, governance, authz, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops TO notrelix_migrator;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA
    identity, workspace, governance, authz, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops TO notrelix_migrator;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA
    identity, workspace, governance, authz, work, docs, collab,
    notifications, activity, automation, integration, billing,
    events, messaging, audit, search, reporting, analytics, ops TO notrelix_migrator;

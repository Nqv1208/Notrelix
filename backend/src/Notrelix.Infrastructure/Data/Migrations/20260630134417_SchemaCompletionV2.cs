using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SchemaCompletionV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_approval_steps_approval_requests_approval_request_id",
                schema: "work",
                table: "approval_steps");

            migrationBuilder.DropForeignKey(
                name: "FK_automation_execution_steps_automation_executions_execution_~",
                schema: "automation",
                table: "automation_execution_steps");

            migrationBuilder.DropForeignKey(
                name: "FK_automation_executions_automation_rules_rule_id",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropForeignKey(
                name: "FK_blocks_blocks_parent_id",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_blocks_pages_page_id",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_board_fields_boards_board_id",
                schema: "work",
                table: "board_fields");

            migrationBuilder.DropForeignKey(
                name: "FK_board_groups_boards_board_id",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_connections_board_relations_relation_id",
                schema: "work",
                table: "board_item_connections");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_labels_board_items_item_id",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_labels_labels_label_id",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_links_board_items_source_item_id",
                schema: "work",
                table: "board_item_links");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_members_board_items_item_id",
                schema: "work",
                table: "board_item_members");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_values_board_fields_field_id",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_values_board_items_item_id",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropForeignKey(
                name: "FK_board_items_board_groups_group_id",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropForeignKey(
                name: "FK_board_items_boards_board_id",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropForeignKey(
                name: "FK_board_members_boards_board_id",
                schema: "work",
                table: "board_members");

            migrationBuilder.DropForeignKey(
                name: "FK_board_relations_boards_source_board_id",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropForeignKey(
                name: "FK_board_relations_boards_target_board_id",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropForeignKey(
                name: "FK_board_subscribers_boards_board_id",
                schema: "work",
                table: "board_subscribers");

            migrationBuilder.DropForeignKey(
                name: "FK_board_view_filter_rules_board_view_user_preferences_prefere~",
                schema: "work",
                table: "board_view_filter_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_board_view_pins_board_views_board_view_id",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropForeignKey(
                name: "FK_board_view_pins_boards_board_id",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropForeignKey(
                name: "FK_board_view_sort_rules_board_view_user_preferences_preferenc~",
                schema: "work",
                table: "board_view_sort_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_board_view_user_preferences_board_views_view_id",
                schema: "work",
                table: "board_view_user_preferences");

            migrationBuilder.DropForeignKey(
                name: "FK_board_views_boards_board_id",
                schema: "work",
                table: "board_views");

            migrationBuilder.DropForeignKey(
                name: "FK_calendar_event_links_calendar_integrations_integration_id",
                schema: "integration",
                table: "calendar_event_links");

            migrationBuilder.DropForeignKey(
                name: "FK_calendar_events_calendar_integrations_integration_id",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropForeignKey(
                name: "FK_checklist_items_checklists_checklist_id",
                schema: "work",
                table: "checklist_items");

            migrationBuilder.DropForeignKey(
                name: "FK_checklists_board_items_item_id",
                schema: "work",
                table: "checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_comments_comments_parent_id",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_custom_role_permissions_custom_roles_custom_role_id",
                schema: "governance",
                table: "custom_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_dashboard_widgets_dashboards_dashboard_id",
                schema: "reporting",
                table: "dashboard_widgets");

            migrationBuilder.DropForeignKey(
                name: "FK_field_options_board_fields_field_id",
                schema: "work",
                table: "field_options");

            migrationBuilder.DropForeignKey(
                name: "FK_form_questions_forms_form_id",
                schema: "work",
                table: "form_questions");

            migrationBuilder.DropForeignKey(
                name: "FK_form_submissions_boards_board_id",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_form_submissions_forms_form_id",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_integration_scopes_integration_connections_connection_id",
                schema: "integration",
                table: "integration_scopes");

            migrationBuilder.DropForeignKey(
                name: "FK_integration_secret_versions_integration_connections_connect~",
                schema: "integration",
                table: "integration_secret_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_item_dependencies_board_items_predecessor_item_id",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_item_dependencies_board_items_successor_item_id",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_item_templates_boards_board_id",
                schema: "work",
                table: "item_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_labels_boards_board_id",
                schema: "work",
                table: "labels");

            migrationBuilder.DropForeignKey(
                name: "FK_mirror_value_snapshots_board_item_connections_connection_id",
                schema: "work",
                table: "mirror_value_snapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_accounts_users_user_id",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_pages_pages_parent_id",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropForeignKey(
                name: "FK_plan_limits_plans_plan_id",
                schema: "billing",
                table: "plan_limits");

            migrationBuilder.DropForeignKey(
                name: "FK_rollup_snapshots_board_items_item_id",
                schema: "work",
                table: "rollup_snapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_filter_rules_saved_filters_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_filter_sort_rules_saved_filters_saved_filter_id",
                schema: "work",
                table: "saved_filter_sort_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_filters_board_views_view_id",
                schema: "work",
                table: "saved_filters");

            migrationBuilder.DropForeignKey(
                name: "FK_team_members_teams_team_id",
                schema: "workspace",
                table: "team_members");

            migrationBuilder.DropForeignKey(
                name: "FK_time_tracking_entries_board_items_item_id",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_time_tracking_entries_boards_board_id",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_usage_metric_history_usage_metrics_metric_id",
                schema: "billing",
                table: "usage_metric_history");

            migrationBuilder.DropForeignKey(
                name: "FK_user_profiles_users_user_id",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspaces",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_policies",
                schema: "governance",
                table: "workspace_policies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_members",
                schema: "workspace",
                table: "workspace_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_invitations",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_feature_usages",
                schema: "billing",
                table: "workspace_feature_usages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workload_allocations",
                schema: "work",
                table: "workload_allocations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_webhook_subscriptions",
                schema: "integration",
                table: "webhook_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_webhook_deliveries",
                schema: "integration",
                table: "webhook_deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                schema: "identity",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_sessions",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_security_settings",
                schema: "identity",
                table: "user_security_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_profiles",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_mfa_methods",
                schema: "identity",
                table: "user_mfa_methods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_login_attempts",
                schema: "identity",
                table: "user_login_attempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usage_metrics",
                schema: "billing",
                table: "usage_metrics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usage_metric_history",
                schema: "billing",
                table: "usage_metric_history");

            migrationBuilder.DropPrimaryKey(
                name: "PK_unread_counters",
                schema: "collab",
                table: "unread_counters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_time_tracking_entries",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_teams",
                schema: "workspace",
                table: "teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_team_members",
                schema: "workspace",
                table: "team_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subscriptions",
                schema: "billing",
                table: "subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sso_providers",
                schema: "identity",
                table: "sso_providers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_spaces",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_share_links",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_security_events",
                schema: "governance",
                table: "security_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_search_index_jobs",
                schema: "search",
                table: "search_index_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_search_documents",
                schema: "search",
                table: "search_documents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scim_directory_syncs",
                schema: "identity",
                table: "scim_directory_syncs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scheduled_jobs",
                schema: "automation",
                table: "scheduled_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_saved_filters",
                schema: "work",
                table: "saved_filters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_saved_filter_sort_rules",
                schema: "work",
                table: "saved_filter_sort_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_saved_filter_rules",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rollup_snapshots",
                schema: "work",
                table: "rollup_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_watchers",
                schema: "collab",
                table: "resource_watchers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_permissions",
                schema: "governance",
                table: "resource_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_permission_inheritance_cache",
                schema: "governance",
                table: "resource_permission_inheritance_cache");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_links",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reporting_snapshots",
                schema: "reporting",
                table: "reporting_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_relation_field_configs",
                schema: "work",
                table: "relation_field_configs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reactions",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_processed_events",
                schema: "ops",
                table: "processed_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_presence_sessions",
                schema: "collab",
                table: "presence_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_plans",
                schema: "billing",
                table: "plans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_plan_limits",
                schema: "billing",
                table: "plan_limits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permission_templates",
                schema: "governance",
                table: "permission_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permission_rules",
                schema: "governance",
                table: "permission_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payment_methods",
                schema: "billing",
                table: "payment_methods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_password_reset_tokens",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_pages",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_page_templates",
                schema: "docs",
                table: "page_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_outbox_messages",
                schema: "ops",
                table: "outbox_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_oauth_accounts",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notifications",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notification_preferences",
                schema: "collab",
                table: "notification_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notification_deliveries",
                schema: "collab",
                table: "notification_deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_mirror_value_snapshots",
                schema: "work",
                table: "mirror_value_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_mentions",
                schema: "collab",
                table: "mentions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_member_role_assignments",
                schema: "governance",
                table: "member_role_assignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_labels",
                schema: "work",
                table: "labels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_job_locks",
                schema: "ops",
                table: "job_locks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_item_templates",
                schema: "work",
                table: "item_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_item_dependencies",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invoices",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_sync_cursors",
                schema: "integration",
                table: "integration_sync_cursors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_secret_versions",
                schema: "integration",
                table: "integration_secret_versions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_scopes",
                schema: "integration",
                table: "integration_scopes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_connections",
                schema: "integration",
                table: "integration_connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbound_webhook_events",
                schema: "integration",
                table: "inbound_webhook_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_import_jobs",
                schema: "ops",
                table: "import_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_idempotency_keys",
                schema: "ops",
                table: "idempotency_keys");

            migrationBuilder.DropPrimaryKey(
                name: "PK_formula_dependencies",
                schema: "work",
                table: "formula_dependencies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_forms",
                schema: "work",
                table: "forms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_form_submissions",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_form_questions",
                schema: "work",
                table: "form_questions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_field_permissions",
                schema: "governance",
                table: "field_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_field_options",
                schema: "work",
                table: "field_options");

            migrationBuilder.DropPrimaryKey(
                name: "PK_feature_usage_ledger",
                schema: "billing",
                table: "feature_usage_ledger");

            migrationBuilder.DropPrimaryKey(
                name: "PK_export_jobs",
                schema: "ops",
                table: "export_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_entitlements",
                schema: "billing",
                table: "entitlements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_email_verification_tokens",
                schema: "identity",
                table: "email_verification_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_document_versions",
                schema: "docs",
                table: "document_versions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dashboards",
                schema: "reporting",
                table: "dashboards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dashboard_widgets",
                schema: "reporting",
                table: "dashboard_widgets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dashboard_sources",
                schema: "reporting",
                table: "dashboard_sources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_custom_roles",
                schema: "governance",
                table: "custom_roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_custom_role_permissions",
                schema: "governance",
                table: "custom_role_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_comments",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_checklists",
                schema: "work",
                table: "checklists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_checklist_items",
                schema: "work",
                table: "checklist_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_integrations",
                schema: "integration",
                table: "calendar_integrations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_events",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_event_links",
                schema: "integration",
                table: "calendar_event_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_boards",
                schema: "work",
                table: "boards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_views",
                schema: "work",
                table: "board_views");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_view_user_preferences",
                schema: "work",
                table: "board_view_user_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_view_pins",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_templates",
                schema: "work",
                table: "board_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_subscribers",
                schema: "work",
                table: "board_subscribers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_relations",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_members",
                schema: "work",
                table: "board_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_items",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_values",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_members",
                schema: "work",
                table: "board_item_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_links",
                schema: "work",
                table: "board_item_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_labels",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_connections",
                schema: "work",
                table: "board_item_connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_groups",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_fields",
                schema: "work",
                table: "board_fields");

            migrationBuilder.DropPrimaryKey(
                name: "PK_blocks",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_billing_events",
                schema: "billing",
                table: "billing_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_templates",
                schema: "automation",
                table: "automation_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_rules",
                schema: "automation",
                table: "automation_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_executions",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_execution_steps",
                schema: "automation",
                table: "automation_execution_steps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_retention_policies",
                schema: "governance",
                table: "audit_retention_policies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_attachments",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_approval_steps",
                schema: "work",
                table: "approval_steps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_approval_requests",
                schema: "work",
                table: "approval_requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_api_tokens",
                schema: "identity",
                table: "api_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_agents",
                schema: "automation",
                table: "ai_agents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_agent_runs",
                schema: "automation",
                table: "ai_agent_runs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_activity_logs",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "activity");

            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.EnsureSchema(
                name: "authz");

            migrationBuilder.RenameColumn(
                name: "MaxRetries",
                schema: "integration",
                table: "webhook_deliveries",
                newName: "max_retries");

            migrationBuilder.RenameColumn(
                name: "FailureReason",
                schema: "integration",
                table: "webhook_deliveries",
                newName: "failure_reason");

            migrationBuilder.RenameColumn(
                name: "FailedAt",
                schema: "integration",
                table: "webhook_deliveries",
                newName: "failed_at");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                schema: "identity",
                table: "user_sessions",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "ExpiredAt",
                schema: "identity",
                table: "user_sessions",
                newName: "expired_at");

            migrationBuilder.RenameColumn(
                name: "preferences",
                schema: "identity",
                table: "user_profiles",
                newName: "preferences_json");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "user_profiles",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "RestoredBy",
                schema: "identity",
                table: "user_profiles",
                newName: "restored_by");

            migrationBuilder.RenameColumn(
                name: "RestoredAt",
                schema: "identity",
                table: "user_profiles",
                newName: "restored_at");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                schema: "identity",
                table: "user_profiles",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "user_profiles",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DeleteReason",
                schema: "identity",
                table: "user_profiles",
                newName: "delete_reason");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "user_profiles",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "user_profiles",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_time_tracking_entries_board_id",
                schema: "work",
                table: "time_tracking_entries",
                newName: "ix_time_tracking_entries_board_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "workspace",
                table: "team_members",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "WorkspaceMemberId",
                schema: "workspace",
                table: "team_members",
                newName: "workspace_member_id");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                schema: "workspace",
                table: "team_members",
                newName: "workspace_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "workspace",
                table: "team_members",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "workspace",
                table: "team_members",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "workspace",
                table: "team_members",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "workspace",
                table: "team_members",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "SpaceType",
                schema: "workspace",
                table: "spaces",
                newName: "space_type");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "governance",
                table: "security_events",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "governance",
                table: "security_events",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RestoredBy",
                schema: "governance",
                table: "security_events",
                newName: "restored_by");

            migrationBuilder.RenameColumn(
                name: "RestoredAt",
                schema: "governance",
                table: "security_events",
                newName: "restored_at");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                schema: "governance",
                table: "security_events",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                schema: "governance",
                table: "security_events",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DeleteReason",
                schema: "governance",
                table: "security_events",
                newName: "delete_reason");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "governance",
                table: "security_events",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "governance",
                table: "security_events",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_saved_filters_view_id",
                schema: "work",
                table: "saved_filters",
                newName: "ix_saved_filters_view_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "work",
                table: "saved_filter_sort_rules",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_saved_filter_sort_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_sort_rules",
                newName: "ix_saved_filter_sort_rules_saved_filter_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "work",
                table: "saved_filter_rules",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_saved_filter_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules",
                newName: "ix_saved_filter_rules_saved_filter_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "collab",
                table: "reactions",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "collab",
                table: "reactions",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RestoredBy",
                schema: "collab",
                table: "reactions",
                newName: "restored_by");

            migrationBuilder.RenameColumn(
                name: "RestoredAt",
                schema: "collab",
                table: "reactions",
                newName: "restored_at");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                schema: "collab",
                table: "reactions",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                schema: "collab",
                table: "reactions",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DeleteReason",
                schema: "collab",
                table: "reactions",
                newName: "delete_reason");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "collab",
                table: "reactions",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "collab",
                table: "reactions",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_form_submissions_board_id",
                schema: "work",
                table: "form_submissions",
                newName: "ix_form_submissions_board_id");

            migrationBuilder.RenameColumn(
                name: "Config",
                schema: "work",
                table: "form_questions",
                newName: "config");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "governance",
                table: "custom_roles",
                newName: "status");

            migrationBuilder.RenameIndex(
                name: "IX_comments_parent_id",
                schema: "collab",
                table: "comments",
                newName: "ix_comments_parent_id");

            migrationBuilder.RenameColumn(
                name: "SpaceId",
                schema: "work",
                table: "boards",
                newName: "space_id");

            migrationBuilder.RenameColumn(
                name: "ItemSequence",
                schema: "work",
                table: "boards",
                newName: "item_sequence");

            migrationBuilder.RenameColumn(
                name: "ItemKeyPrefix",
                schema: "work",
                table: "boards",
                newName: "item_key_prefix");

            migrationBuilder.RenameColumn(
                name: "DefaultItemGroupId",
                schema: "work",
                table: "boards",
                newName: "default_item_group_id");

            migrationBuilder.RenameColumn(
                name: "BoardType",
                schema: "work",
                table: "boards",
                newName: "board_type");

            migrationBuilder.RenameColumn(
                name: "BoardFamily",
                schema: "work",
                table: "boards",
                newName: "board_family");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "work",
                table: "board_view_sort_rules",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_board_view_sort_rules_preference_id",
                schema: "work",
                table: "board_view_sort_rules",
                newName: "ix_board_view_sort_rules_preference_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "work",
                table: "board_view_filter_rules",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_board_view_filter_rules_preference_id",
                schema: "work",
                table: "board_view_filter_rules",
                newName: "ix_board_view_filter_rules_preference_id");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                schema: "work",
                table: "board_items",
                newName: "started_at");

            migrationBuilder.RenameColumn(
                name: "ParentItemId",
                schema: "work",
                table: "board_items",
                newName: "parent_item_id");

            migrationBuilder.RenameColumn(
                name: "ItemSequence",
                schema: "work",
                table: "board_items",
                newName: "item_sequence");

            migrationBuilder.RenameColumn(
                name: "ItemLevel",
                schema: "work",
                table: "board_items",
                newName: "item_level");

            migrationBuilder.RenameColumn(
                name: "ItemKey",
                schema: "work",
                table: "board_items",
                newName: "item_key");

            migrationBuilder.RenameColumn(
                name: "DueAt",
                schema: "work",
                table: "board_items",
                newName: "due_at");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                schema: "work",
                table: "board_items",
                newName: "completed_at");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_values_field_id",
                schema: "work",
                table: "board_item_values",
                newName: "ix_board_item_values_field_id");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                schema: "work",
                table: "board_item_members",
                newName: "workspace_id");

            migrationBuilder.RenameColumn(
                name: "BoardId",
                schema: "work",
                table: "board_item_members",
                newName: "board_id");

            migrationBuilder.RenameColumn(
                name: "AssignedByUserId",
                schema: "work",
                table: "board_item_members",
                newName: "assigned_by_user_id");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                schema: "work",
                table: "board_item_links",
                newName: "workspace_id");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "work",
                table: "board_item_links",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "work",
                table: "board_item_links",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BoardId",
                schema: "work",
                table: "board_item_links",
                newName: "board_id");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "work",
                table: "board_item_labels",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "work",
                table: "board_item_labels",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_labels_label_id",
                schema: "work",
                table: "board_item_labels",
                newName: "ix_board_item_labels_label_id");

            migrationBuilder.RenameColumn(
                name: "MirrorSourceJson",
                schema: "work",
                table: "board_fields",
                newName: "mirror_source_json");

            migrationBuilder.RenameColumn(
                name: "IsSensitive",
                schema: "work",
                table: "board_fields",
                newName: "is_sensitive");

            migrationBuilder.RenameColumn(
                name: "IsFormula",
                schema: "work",
                table: "board_fields",
                newName: "is_formula");

            migrationBuilder.RenameColumn(
                name: "FormulaExpression",
                schema: "work",
                table: "board_fields",
                newName: "formula_expression");

            migrationBuilder.RenameColumn(
                name: "DataClassification",
                schema: "work",
                table: "board_fields",
                newName: "data_classification");

            migrationBuilder.AlterColumn<bool>(
                name: "is_system",
                schema: "governance",
                table: "custom_roles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "pk_workspaces",
                schema: "workspace",
                table: "workspaces",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_workspace_policies",
                schema: "governance",
                table: "workspace_policies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_workspace_members",
                schema: "workspace",
                table: "workspace_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_workspace_invitations",
                schema: "workspace",
                table: "workspace_invitations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_workspace_feature_usages",
                schema: "billing",
                table: "workspace_feature_usages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_workload_allocations",
                schema: "work",
                table: "workload_allocations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_webhook_subscriptions",
                schema: "integration",
                table: "webhook_subscriptions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_webhook_deliveries",
                schema: "integration",
                table: "webhook_deliveries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                schema: "identity",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_sessions",
                schema: "identity",
                table: "user_sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_security_settings",
                schema: "identity",
                table: "user_security_settings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_profiles",
                schema: "identity",
                table: "user_profiles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_mfa_methods",
                schema: "identity",
                table: "user_mfa_methods",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_login_attempts",
                schema: "identity",
                table: "user_login_attempts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_usage_metrics",
                schema: "billing",
                table: "usage_metrics",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_usage_metric_history",
                schema: "billing",
                table: "usage_metric_history",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_unread_counters",
                schema: "collab",
                table: "unread_counters",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_time_tracking_entries",
                schema: "work",
                table: "time_tracking_entries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_teams",
                schema: "workspace",
                table: "teams",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_team_members",
                schema: "workspace",
                table: "team_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_subscriptions",
                schema: "billing",
                table: "subscriptions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sso_providers",
                schema: "identity",
                table: "sso_providers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_spaces",
                schema: "workspace",
                table: "spaces",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_share_links",
                schema: "governance",
                table: "share_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_security_events",
                schema: "governance",
                table: "security_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_search_index_jobs",
                schema: "search",
                table: "search_index_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_search_documents",
                schema: "search",
                table: "search_documents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_scim_directory_syncs",
                schema: "identity",
                table: "scim_directory_syncs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_scheduled_jobs",
                schema: "automation",
                table: "scheduled_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_saved_filters",
                schema: "work",
                table: "saved_filters",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_saved_filter_sort_rules",
                schema: "work",
                table: "saved_filter_sort_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_saved_filter_rules",
                schema: "work",
                table: "saved_filter_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_rollup_snapshots",
                schema: "work",
                table: "rollup_snapshots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_resource_watchers",
                schema: "collab",
                table: "resource_watchers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_resource_permissions",
                schema: "governance",
                table: "resource_permissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_resource_permission_inheritance_cache",
                schema: "governance",
                table: "resource_permission_inheritance_cache",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_resource_links",
                schema: "docs",
                table: "resource_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_reporting_snapshots",
                schema: "reporting",
                table: "reporting_snapshots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_relation_field_configs",
                schema: "work",
                table: "relation_field_configs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_reactions",
                schema: "collab",
                table: "reactions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_processed_events",
                schema: "ops",
                table: "processed_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_presence_sessions",
                schema: "collab",
                table: "presence_sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_plans",
                schema: "billing",
                table: "plans",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_plan_limits",
                schema: "billing",
                table: "plan_limits",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_permission_templates",
                schema: "governance",
                table: "permission_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_permission_rules",
                schema: "governance",
                table: "permission_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_payment_methods",
                schema: "billing",
                table: "payment_methods",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_password_reset_tokens",
                schema: "identity",
                table: "password_reset_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_pages",
                schema: "docs",
                table: "pages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_page_templates",
                schema: "docs",
                table: "page_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_outbox_messages",
                schema: "ops",
                table: "outbox_messages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_oauth_accounts",
                schema: "identity",
                table: "oauth_accounts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_notifications",
                schema: "collab",
                table: "notifications",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_notification_preferences",
                schema: "collab",
                table: "notification_preferences",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_notification_deliveries",
                schema: "collab",
                table: "notification_deliveries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mirror_value_snapshots",
                schema: "work",
                table: "mirror_value_snapshots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mentions",
                schema: "collab",
                table: "mentions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_member_role_assignments",
                schema: "governance",
                table: "member_role_assignments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_labels",
                schema: "work",
                table: "labels",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_job_locks",
                schema: "ops",
                table: "job_locks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_item_templates",
                schema: "work",
                table: "item_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_item_dependencies",
                schema: "work",
                table: "item_dependencies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_invoices",
                schema: "billing",
                table: "invoices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_integration_sync_cursors",
                schema: "integration",
                table: "integration_sync_cursors",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_integration_secret_versions",
                schema: "integration",
                table: "integration_secret_versions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_integration_scopes",
                schema: "integration",
                table: "integration_scopes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_integration_connections",
                schema: "integration",
                table: "integration_connections",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_inbound_webhook_events",
                schema: "integration",
                table: "inbound_webhook_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_import_jobs",
                schema: "ops",
                table: "import_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_idempotency_keys",
                schema: "ops",
                table: "idempotency_keys",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_formula_dependencies",
                schema: "work",
                table: "formula_dependencies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_forms",
                schema: "work",
                table: "forms",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_form_submissions",
                schema: "work",
                table: "form_submissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_form_questions",
                schema: "work",
                table: "form_questions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_field_permissions",
                schema: "governance",
                table: "field_permissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_field_options",
                schema: "work",
                table: "field_options",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_feature_usage_ledger",
                schema: "billing",
                table: "feature_usage_ledger",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_export_jobs",
                schema: "ops",
                table: "export_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_entitlements",
                schema: "billing",
                table: "entitlements",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_email_verification_tokens",
                schema: "identity",
                table: "email_verification_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_document_versions",
                schema: "docs",
                table: "document_versions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_dashboards",
                schema: "reporting",
                table: "dashboards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_dashboard_widgets",
                schema: "reporting",
                table: "dashboard_widgets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_dashboard_sources",
                schema: "reporting",
                table: "dashboard_sources",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_custom_roles",
                schema: "governance",
                table: "custom_roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_custom_role_permissions",
                schema: "governance",
                table: "custom_role_permissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_comments",
                schema: "collab",
                table: "comments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_checklists",
                schema: "work",
                table: "checklists",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_checklist_items",
                schema: "work",
                table: "checklist_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_calendar_integrations",
                schema: "integration",
                table: "calendar_integrations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_calendar_events",
                schema: "integration",
                table: "calendar_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_calendar_event_links",
                schema: "integration",
                table: "calendar_event_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_boards",
                schema: "work",
                table: "boards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_views",
                schema: "work",
                table: "board_views",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_view_user_preferences",
                schema: "work",
                table: "board_view_user_preferences",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_view_pins",
                schema: "work",
                table: "board_view_pins",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_templates",
                schema: "work",
                table: "board_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_subscribers",
                schema: "work",
                table: "board_subscribers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_relations",
                schema: "work",
                table: "board_relations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_members",
                schema: "work",
                table: "board_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_items",
                schema: "work",
                table: "board_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_item_values",
                schema: "work",
                table: "board_item_values",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_item_members",
                schema: "work",
                table: "board_item_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_item_links",
                schema: "work",
                table: "board_item_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_item_labels",
                schema: "work",
                table: "board_item_labels",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_item_connections",
                schema: "work",
                table: "board_item_connections",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_groups",
                schema: "work",
                table: "board_groups",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_fields",
                schema: "work",
                table: "board_fields",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_blocks",
                schema: "docs",
                table: "blocks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_billing_events",
                schema: "billing",
                table: "billing_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_automation_templates",
                schema: "automation",
                table: "automation_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_automation_rules",
                schema: "automation",
                table: "automation_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_automation_executions",
                schema: "automation",
                table: "automation_executions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_automation_execution_steps",
                schema: "automation",
                table: "automation_execution_steps",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_audit_retention_policies",
                schema: "governance",
                table: "audit_retention_policies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_audit_logs",
                schema: "governance",
                table: "audit_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_attachments",
                schema: "collab",
                table: "attachments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_approval_steps",
                schema: "work",
                table: "approval_steps",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_approval_requests",
                schema: "work",
                table: "approval_requests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_api_tokens",
                schema: "identity",
                table: "api_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_ai_agents",
                schema: "automation",
                table: "ai_agents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_ai_agent_runs",
                schema: "automation",
                table: "ai_agent_runs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_activity_logs",
                schema: "collab",
                table: "activity_logs",
                column: "id");

            migrationBuilder.CreateTable(
                name: "activity_logs",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_display_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    activity_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    verb = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    resource_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    target_resource_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    target_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_resource_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    hidden_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hidden_by = table.Column<Guid>(type: "uuid", nullable: true),
                    hide_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "activity_read_states",
                schema: "activity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_read_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "User"),
                    action = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    severity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Info"),
                    outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Succeeded"),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    request_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    causation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    before_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    after_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retention_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain_event_logs",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    event_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    event_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    aggregate_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    causation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    retention_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domain_event_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_delivery_attempts",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_no = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    provider_response_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_delivery_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_outbox",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    source_context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_email = table.Column<string>(type: "text", nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    template_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    template_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    subject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: true),
                    body_text = table.Column<string>(type: "text", nullable: true),
                    template_data_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    headers_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    locked_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature_usage_daily",
                schema: "analytics",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usage_date = table.Column<DateOnly>(type: "date", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    usage_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    unique_actor_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    unit = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    calculated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_watermark_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_usage_daily", x => new { x.workspace_id, x.usage_date, x.feature_code });
                });

            migrationBuilder.CreateTable(
                name: "notification_items",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    source_context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Info"),
                    subject_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    action_url = table.Column<string>(type: "text", nullable: true),
                    data_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true),
                    restored_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    restored_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    delivery_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Immediate"),
                    digest_interval_minutes = table.Column<int>(type: "integer", nullable: true),
                    quiet_hours_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    timezone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_delivery_attempts",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_no = table.Column<int>(type: "integer", nullable: false),
                    dispatcher_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    broker = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    destination = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    error_detail_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_delivery_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    message_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    destination = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    content_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: "application/json"),
                    subject_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    aggregate_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    causation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    partition_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    payload_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    headers_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    locked_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_events",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    source_context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    message_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    message_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    causation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    result = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Succeeded"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "security_events",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    severity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Info"),
                    outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Observed"),
                    risk_score = table.Column<int>(type: "integer", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    device_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retention_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_access_grants",
                schema: "authz",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "Workspace"),
                    membership_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    role_codes = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    permission_codes = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    is_workspace_admin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_version = table.Column<long>(type: "bigint", nullable: true),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_access_grants", x => new { x.workspace_id, x.user_id });
                });

            migrationBuilder.CreateTable(
                name: "workspace_activity_logs",
                schema: "activity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    activity_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    actor_avatar_url = table.Column<string>(type: "text", nullable: true),
                    subject_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    target_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    resource_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    data_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    visibility = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Workspace"),
                    importance = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Normal"),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_activity_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_usage_daily",
                schema: "analytics",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usage_date = table.Column<DateOnly>(type: "date", nullable: false),
                    active_users = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    new_users = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    boards_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    items_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    items_completed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    docs_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    comments_created = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    automations_executed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    integrations_executed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    storage_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    attachment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    calculated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_watermark_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_usage_daily", x => new { x.workspace_id, x.usage_date });
                });

            migrationBuilder.CreateTable(
                name: "notification_recipients",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_email = table.Column<string>(type: "text", nullable: true),
                    recipient_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    delivery_policy_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Unread"),
                    seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dismissed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_recipients_notification_items_notification_id",
                        column: x => x.notification_id,
                        principalSchema: "notifications",
                        principalTable: "notification_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_actor_user_id_occurred_at",
                schema: "audit",
                table: "activity_logs",
                columns: new[] { "actor_user_id", "occurred_at" },
                descending: new[] { false, true },
                filter: "\"actor_user_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_resource_type_resource_id_occurred_at",
                schema: "audit",
                table: "activity_logs",
                columns: new[] { "resource_type", "resource_id", "occurred_at" },
                descending: new[] { false, false, true },
                filter: "\"resource_type\" IS NOT NULL AND \"resource_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_workspace_id_is_visible_occurred_at",
                schema: "audit",
                table: "activity_logs",
                columns: new[] { "workspace_id", "is_visible", "occurred_at" },
                descending: new[] { false, false, true },
                filter: "\"is_visible\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_workspace_id_occurred_at",
                schema: "audit",
                table: "activity_logs",
                columns: new[] { "workspace_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_activity_read_states_workspace_id_user_id",
                schema: "activity",
                table: "activity_read_states",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_action_occurred_at",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "action", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_user_id_occurred_at",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "occurred_at" },
                descending: new[] { false, true },
                filter: "\"actor_user_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_correlation_id",
                schema: "audit",
                table: "audit_logs",
                column: "correlation_id",
                filter: "\"correlation_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_resource_type_resource_id_occurred_at",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "resource_type", "resource_id", "occurred_at" },
                descending: new[] { false, false, true },
                filter: "\"resource_type\" IS NOT NULL AND \"resource_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_workspace_id_occurred_at",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "workspace_id", "occurred_at" },
                descending: new[] { false, true },
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_domain_event_logs_correlation_id",
                schema: "events",
                table: "domain_event_logs",
                column: "correlation_id",
                filter: "\"correlation_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_domain_event_logs_event_id",
                schema: "events",
                table: "domain_event_logs",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_domain_event_logs_payload_json",
                schema: "events",
                table: "domain_event_logs",
                column: "payload_json")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_domain_event_logs_recorded_at",
                schema: "events",
                table: "domain_event_logs",
                column: "recorded_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_domain_event_logs_source_context_aggregate_type_aggregate_i",
                schema: "events",
                table: "domain_event_logs",
                columns: new[] { "source_context", "aggregate_type", "aggregate_id", "recorded_at" },
                descending: new[] { false, false, false, true },
                filter: "\"aggregate_type\" IS NOT NULL AND \"aggregate_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_domain_event_logs_source_context_event_name_recorded_at",
                schema: "events",
                table: "domain_event_logs",
                columns: new[] { "source_context", "event_name", "recorded_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_domain_event_logs_subject_type_subject_id_recorded_at",
                schema: "events",
                table: "domain_event_logs",
                columns: new[] { "subject_type", "subject_id", "recorded_at" },
                descending: new[] { false, false, true },
                filter: "\"subject_type\" IS NOT NULL AND \"subject_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_domain_event_logs_workspace_id_recorded_at",
                schema: "events",
                table: "domain_event_logs",
                columns: new[] { "workspace_id", "recorded_at" },
                descending: new[] { false, true },
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_email_delivery_attempts_email_outbox_id_attempt_no",
                schema: "notifications",
                table: "email_delivery_attempts",
                columns: new[] { "email_outbox_id", "attempt_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_delivery_attempts_provider_provider_message_id",
                schema: "notifications",
                table: "email_delivery_attempts",
                columns: new[] { "provider", "provider_message_id" },
                filter: "\"provider\" IS NOT NULL AND \"provider_message_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_email_delivery_attempts_status_started_at",
                schema: "notifications",
                table: "email_delivery_attempts",
                columns: new[] { "status", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_deduplication_key",
                schema: "notifications",
                table: "email_outbox",
                column: "deduplication_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_recipient_user_id_created_at",
                schema: "notifications",
                table: "email_outbox",
                columns: new[] { "recipient_user_id", "created_at" },
                descending: new[] { false, true },
                filter: "\"recipient_user_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_source_message_id",
                schema: "notifications",
                table: "email_outbox",
                column: "source_message_id",
                filter: "\"source_message_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_status_locked_until_processing_started_at",
                schema: "notifications",
                table: "email_outbox",
                columns: new[] { "status", "locked_until", "processing_started_at" },
                filter: "\"status\" = 'Sending'");

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_status_priority_next_attempt_at_created_at",
                schema: "notifications",
                table: "email_outbox",
                columns: new[] { "status", "priority", "next_attempt_at", "created_at" },
                filter: "\"status\" IN ('Pending', 'Failed')");

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_workspace_id_created_at",
                schema: "notifications",
                table: "email_outbox",
                columns: new[] { "workspace_id", "created_at" },
                descending: new[] { false, true },
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_feature_usage_daily_feature_code_usage_date",
                schema: "analytics",
                table: "feature_usage_daily",
                columns: new[] { "feature_code", "usage_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_items_resource",
                schema: "notifications",
                table: "notification_items",
                columns: new[] { "resource_type", "resource_id", "created_at" },
                descending: new[] { false, false, true },
                filter: "\"resource_type\" IS NOT NULL AND \"resource_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_items_source_event",
                schema: "notifications",
                table: "notification_items",
                column: "source_event_id",
                filter: "\"source_event_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_items_source_message",
                schema: "notifications",
                table: "notification_items",
                column: "source_message_id",
                filter: "\"source_message_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_items_subject",
                schema: "notifications",
                table: "notification_items",
                columns: new[] { "subject_type", "subject_id", "created_at" },
                descending: new[] { false, false, true },
                filter: "\"subject_type\" IS NOT NULL AND \"subject_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_items_workspace_time",
                schema: "notifications",
                table: "notification_items",
                columns: new[] { "workspace_id", "created_at" },
                descending: new[] { false, true },
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notifications_items_dedup",
                schema: "notifications",
                table: "notification_items",
                column: "deduplication_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_preferences_user",
                schema: "notifications",
                table: "notification_preferences",
                columns: new[] { "user_id", "workspace_id" });

            migrationBuilder.CreateIndex(
                name: "ux_notifications_preferences_global",
                schema: "notifications",
                table: "notification_preferences",
                columns: new[] { "user_id", "notification_type", "channel" },
                unique: true,
                filter: "\"workspace_id\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notifications_preferences_workspace",
                schema: "notifications",
                table: "notification_preferences",
                columns: new[] { "workspace_id", "user_id", "notification_type", "channel" },
                unique: true,
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipients_notification",
                schema: "notifications",
                table: "notification_recipients",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipients_user_status_time",
                schema: "notifications",
                table: "notification_recipients",
                columns: new[] { "recipient_user_id", "status", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipients_workspace_user_status",
                schema: "notifications",
                table: "notification_recipients",
                columns: new[] { "workspace_id", "recipient_user_id", "status", "created_at" },
                descending: new[] { false, false, false, true },
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notifications_recipients_notification_user",
                schema: "notifications",
                table: "notification_recipients",
                columns: new[] { "notification_id", "recipient_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_delivery_attempts_event_id_started_at",
                schema: "messaging",
                table: "outbox_delivery_attempts",
                columns: new[] { "event_id", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_delivery_attempts_outbox_message_id_attempt_no",
                schema: "messaging",
                table: "outbox_delivery_attempts",
                columns: new[] { "outbox_message_id", "attempt_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_delivery_attempts_status_started_at",
                schema: "messaging",
                table: "outbox_delivery_attempts",
                columns: new[] { "status", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_correlation_id",
                schema: "messaging",
                table: "outbox_messages",
                column: "correlation_id",
                filter: "\"correlation_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_event_id",
                schema: "messaging",
                table: "outbox_messages",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_partition_key_created_at",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "partition_key", "created_at" },
                descending: new[] { false, true },
                filter: "\"partition_key\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_source_context_message_name_created_at",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "source_context", "message_name", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status_locked_until_processing_started_at",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "status", "locked_until", "processing_started_at" },
                filter: "\"status\" = 'Processing'");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status_next_attempt_at_created_at",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "status", "next_attempt_at", "created_at" },
                filter: "\"status\" IN ('Pending', 'Failed')");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_subject_type_subject_id_created_at",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "subject_type", "subject_id", "created_at" },
                descending: new[] { false, false, true },
                filter: "\"subject_type\" IS NOT NULL AND \"subject_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_workspace_id_created_at",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "workspace_id", "created_at" },
                descending: new[] { false, true },
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_consumer_name_processed_at",
                schema: "messaging",
                table: "processed_events",
                columns: new[] { "consumer_name", "processed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_correlation_id",
                schema: "messaging",
                table: "processed_events",
                column: "correlation_id",
                filter: "\"correlation_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_event_id_consumer_name",
                schema: "messaging",
                table: "processed_events",
                columns: new[] { "event_id", "consumer_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_message_name_processed_at",
                schema: "messaging",
                table: "processed_events",
                columns: new[] { "message_name", "processed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_workspace_id_processed_at",
                schema: "messaging",
                table: "processed_events",
                columns: new[] { "workspace_id", "processed_at" },
                descending: new[] { false, true },
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_security_events_event_type_occurred_at",
                schema: "audit",
                table: "security_events",
                columns: new[] { "event_type", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_security_events_severity_occurred_at",
                schema: "audit",
                table: "security_events",
                columns: new[] { "severity", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_security_events_user_id_occurred_at",
                schema: "audit",
                table: "security_events",
                columns: new[] { "user_id", "occurred_at" },
                descending: new[] { false, true },
                filter: "\"user_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_security_events_workspace_id_occurred_at",
                schema: "audit",
                table: "security_events",
                columns: new[] { "workspace_id", "occurred_at" },
                descending: new[] { false, true },
                filter: "\"workspace_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_access_grants_user_id_workspace_id",
                schema: "authz",
                table: "workspace_access_grants",
                columns: new[] { "user_id", "workspace_id" },
                filter: "\"membership_status\" = 'Active' AND \"revoked_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_access_grants_workspace_id_user_id",
                schema: "authz",
                table: "workspace_access_grants",
                columns: new[] { "workspace_id", "user_id" },
                filter: "\"membership_status\" = 'Active' AND \"revoked_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_workspace_logs_actor",
                schema: "activity",
                table: "workspace_activity_logs",
                columns: new[] { "actor_user_id", "occurred_at" },
                descending: new[] { false, true },
                filter: "\"actor_user_id\" IS NOT NULL AND \"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_workspace_logs_data_gin",
                schema: "activity",
                table: "workspace_activity_logs",
                column: "data_json")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_activity_workspace_logs_resource",
                schema: "activity",
                table: "workspace_activity_logs",
                columns: new[] { "resource_type", "resource_id", "occurred_at" },
                descending: new[] { false, false, true },
                filter: "\"resource_type\" IS NOT NULL AND \"resource_id\" IS NOT NULL AND \"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_workspace_logs_source_event",
                schema: "activity",
                table: "workspace_activity_logs",
                column: "source_event_id",
                filter: "\"source_event_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_workspace_logs_source_message",
                schema: "activity",
                table: "workspace_activity_logs",
                column: "source_message_id",
                filter: "\"source_message_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_workspace_logs_subject",
                schema: "activity",
                table: "workspace_activity_logs",
                columns: new[] { "subject_type", "subject_id", "occurred_at" },
                descending: new[] { false, false, true },
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_workspace_logs_type",
                schema: "activity",
                table: "workspace_activity_logs",
                columns: new[] { "activity_type", "occurred_at" },
                descending: new[] { false, true },
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_activity_workspace_logs_workspace_time",
                schema: "activity",
                table: "workspace_activity_logs",
                columns: new[] { "workspace_id", "occurred_at" },
                descending: new[] { false, true },
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_usage_daily_usage_date",
                schema: "analytics",
                table: "workspace_usage_daily",
                column: "usage_date",
                descending: new bool[0]);

            migrationBuilder.AddForeignKey(
                name: "fk_approval_steps_approval_requests_approval_request_id",
                schema: "work",
                table: "approval_steps",
                column: "approval_request_id",
                principalSchema: "work",
                principalTable: "approval_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_automation_execution_steps_automation_executions_execution_",
                schema: "automation",
                table: "automation_execution_steps",
                column: "execution_id",
                principalSchema: "automation",
                principalTable: "automation_executions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_automation_executions_automation_rules_rule_id",
                schema: "automation",
                table: "automation_executions",
                column: "rule_id",
                principalSchema: "automation",
                principalTable: "automation_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_blocks_blocks_parent_id",
                schema: "docs",
                table: "blocks",
                column: "parent_id",
                principalSchema: "docs",
                principalTable: "blocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_blocks_pages_page_id",
                schema: "docs",
                table: "blocks",
                column: "page_id",
                principalSchema: "docs",
                principalTable: "pages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_fields_boards_board_id",
                schema: "work",
                table: "board_fields",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_groups_boards_board_id",
                schema: "work",
                table: "board_groups",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_item_connections_board_relations_relation_id",
                schema: "work",
                table: "board_item_connections",
                column: "relation_id",
                principalSchema: "work",
                principalTable: "board_relations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_item_labels_board_items_item_id",
                schema: "work",
                table: "board_item_labels",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_item_labels_labels_label_id",
                schema: "work",
                table: "board_item_labels",
                column: "label_id",
                principalSchema: "work",
                principalTable: "labels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_board_item_links_board_items_source_item_id",
                schema: "work",
                table: "board_item_links",
                column: "source_item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_item_members_board_items_item_id",
                schema: "work",
                table: "board_item_members",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_item_values_board_fields_field_id",
                schema: "work",
                table: "board_item_values",
                column: "field_id",
                principalSchema: "work",
                principalTable: "board_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_board_item_values_board_items_item_id",
                schema: "work",
                table: "board_item_values",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_items_board_groups_group_id",
                schema: "work",
                table: "board_items",
                column: "group_id",
                principalSchema: "work",
                principalTable: "board_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_board_items_boards_board_id",
                schema: "work",
                table: "board_items",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_members_boards_board_id",
                schema: "work",
                table: "board_members",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_relations_boards_source_board_id",
                schema: "work",
                table: "board_relations",
                column: "source_board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_board_relations_boards_target_board_id",
                schema: "work",
                table: "board_relations",
                column: "target_board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_board_subscribers_boards_board_id",
                schema: "work",
                table: "board_subscribers",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_view_filter_rules_board_view_user_preferences_prefere",
                schema: "work",
                table: "board_view_filter_rules",
                column: "preference_id",
                principalSchema: "work",
                principalTable: "board_view_user_preferences",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_view_pins_board_views_board_view_id",
                schema: "work",
                table: "board_view_pins",
                column: "board_view_id",
                principalSchema: "work",
                principalTable: "board_views",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_view_pins_boards_board_id",
                schema: "work",
                table: "board_view_pins",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_view_sort_rules_board_view_user_preferences_preferenc",
                schema: "work",
                table: "board_view_sort_rules",
                column: "preference_id",
                principalSchema: "work",
                principalTable: "board_view_user_preferences",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_view_user_preferences_board_views_view_id",
                schema: "work",
                table: "board_view_user_preferences",
                column: "view_id",
                principalSchema: "work",
                principalTable: "board_views",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_views_boards_board_id",
                schema: "work",
                table: "board_views",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_calendar_event_links_calendar_integrations_integration_id",
                schema: "integration",
                table: "calendar_event_links",
                column: "integration_id",
                principalSchema: "integration",
                principalTable: "calendar_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_calendar_events_calendar_integrations_integration_id",
                schema: "integration",
                table: "calendar_events",
                column: "integration_id",
                principalSchema: "integration",
                principalTable: "calendar_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_checklist_items_checklists_checklist_id",
                schema: "work",
                table: "checklist_items",
                column: "checklist_id",
                principalSchema: "work",
                principalTable: "checklists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_checklists_board_items_item_id",
                schema: "work",
                table: "checklists",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_comments_comments_parent_id",
                schema: "collab",
                table: "comments",
                column: "parent_id",
                principalSchema: "collab",
                principalTable: "comments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_custom_role_permissions_custom_roles_custom_role_id",
                schema: "governance",
                table: "custom_role_permissions",
                column: "custom_role_id",
                principalSchema: "governance",
                principalTable: "custom_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_dashboard_widgets_dashboards_dashboard_id",
                schema: "reporting",
                table: "dashboard_widgets",
                column: "dashboard_id",
                principalSchema: "reporting",
                principalTable: "dashboards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_field_options_board_fields_field_id",
                schema: "work",
                table: "field_options",
                column: "field_id",
                principalSchema: "work",
                principalTable: "board_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_form_questions_forms_form_id",
                schema: "work",
                table: "form_questions",
                column: "form_id",
                principalSchema: "work",
                principalTable: "forms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_form_submissions_boards_board_id",
                schema: "work",
                table: "form_submissions",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_form_submissions_forms_form_id",
                schema: "work",
                table: "form_submissions",
                column: "form_id",
                principalSchema: "work",
                principalTable: "forms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_scopes_integration_connections_connection_id",
                schema: "integration",
                table: "integration_scopes",
                column: "connection_id",
                principalSchema: "integration",
                principalTable: "integration_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_secret_versions_integration_connections_connect",
                schema: "integration",
                table: "integration_secret_versions",
                column: "connection_id",
                principalSchema: "integration",
                principalTable: "integration_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_item_dependencies_board_items_predecessor_item_id",
                schema: "work",
                table: "item_dependencies",
                column: "predecessor_item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_item_dependencies_board_items_successor_item_id",
                schema: "work",
                table: "item_dependencies",
                column: "successor_item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_item_templates_boards_board_id",
                schema: "work",
                table: "item_templates",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_labels_boards_board_id",
                schema: "work",
                table: "labels",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mirror_value_snapshots_board_item_connections_connection_id",
                schema: "work",
                table: "mirror_value_snapshots",
                column: "connection_id",
                principalSchema: "work",
                principalTable: "board_item_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_oauth_accounts_users_user_id",
                schema: "identity",
                table: "oauth_accounts",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pages_pages_parent_id",
                schema: "docs",
                table: "pages",
                column: "parent_id",
                principalSchema: "docs",
                principalTable: "pages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_plan_limits_plans_plan_id",
                schema: "billing",
                table: "plan_limits",
                column: "plan_id",
                principalSchema: "billing",
                principalTable: "plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_rollup_snapshots_board_items_item_id",
                schema: "work",
                table: "rollup_snapshots",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_saved_filter_rules_saved_filters_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules",
                column: "saved_filter_id",
                principalSchema: "work",
                principalTable: "saved_filters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_saved_filter_sort_rules_saved_filters_saved_filter_id",
                schema: "work",
                table: "saved_filter_sort_rules",
                column: "saved_filter_id",
                principalSchema: "work",
                principalTable: "saved_filters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_saved_filters_board_views_view_id",
                schema: "work",
                table: "saved_filters",
                column: "view_id",
                principalSchema: "work",
                principalTable: "board_views",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_team_members_teams_team_id",
                schema: "workspace",
                table: "team_members",
                column: "team_id",
                principalSchema: "workspace",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_time_tracking_entries_board_items_item_id",
                schema: "work",
                table: "time_tracking_entries",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_time_tracking_entries_boards_board_id",
                schema: "work",
                table: "time_tracking_entries",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_usage_metric_history_usage_metrics_metric_id",
                schema: "billing",
                table: "usage_metric_history",
                column: "metric_id",
                principalSchema: "billing",
                principalTable: "usage_metrics",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_profiles_users_user_id",
                schema: "identity",
                table: "user_profiles",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_approval_steps_approval_requests_approval_request_id",
                schema: "work",
                table: "approval_steps");

            migrationBuilder.DropForeignKey(
                name: "fk_automation_execution_steps_automation_executions_execution_",
                schema: "automation",
                table: "automation_execution_steps");

            migrationBuilder.DropForeignKey(
                name: "fk_automation_executions_automation_rules_rule_id",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropForeignKey(
                name: "fk_blocks_blocks_parent_id",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_blocks_pages_page_id",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_board_fields_boards_board_id",
                schema: "work",
                table: "board_fields");

            migrationBuilder.DropForeignKey(
                name: "fk_board_groups_boards_board_id",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_board_item_connections_board_relations_relation_id",
                schema: "work",
                table: "board_item_connections");

            migrationBuilder.DropForeignKey(
                name: "fk_board_item_labels_board_items_item_id",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropForeignKey(
                name: "fk_board_item_labels_labels_label_id",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropForeignKey(
                name: "fk_board_item_links_board_items_source_item_id",
                schema: "work",
                table: "board_item_links");

            migrationBuilder.DropForeignKey(
                name: "fk_board_item_members_board_items_item_id",
                schema: "work",
                table: "board_item_members");

            migrationBuilder.DropForeignKey(
                name: "fk_board_item_values_board_fields_field_id",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropForeignKey(
                name: "fk_board_item_values_board_items_item_id",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropForeignKey(
                name: "fk_board_items_board_groups_group_id",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropForeignKey(
                name: "fk_board_items_boards_board_id",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropForeignKey(
                name: "fk_board_members_boards_board_id",
                schema: "work",
                table: "board_members");

            migrationBuilder.DropForeignKey(
                name: "fk_board_relations_boards_source_board_id",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropForeignKey(
                name: "fk_board_relations_boards_target_board_id",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropForeignKey(
                name: "fk_board_subscribers_boards_board_id",
                schema: "work",
                table: "board_subscribers");

            migrationBuilder.DropForeignKey(
                name: "fk_board_view_filter_rules_board_view_user_preferences_prefere",
                schema: "work",
                table: "board_view_filter_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_board_view_pins_board_views_board_view_id",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropForeignKey(
                name: "fk_board_view_pins_boards_board_id",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropForeignKey(
                name: "fk_board_view_sort_rules_board_view_user_preferences_preferenc",
                schema: "work",
                table: "board_view_sort_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_board_view_user_preferences_board_views_view_id",
                schema: "work",
                table: "board_view_user_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_board_views_boards_board_id",
                schema: "work",
                table: "board_views");

            migrationBuilder.DropForeignKey(
                name: "fk_calendar_event_links_calendar_integrations_integration_id",
                schema: "integration",
                table: "calendar_event_links");

            migrationBuilder.DropForeignKey(
                name: "fk_calendar_events_calendar_integrations_integration_id",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropForeignKey(
                name: "fk_checklist_items_checklists_checklist_id",
                schema: "work",
                table: "checklist_items");

            migrationBuilder.DropForeignKey(
                name: "fk_checklists_board_items_item_id",
                schema: "work",
                table: "checklists");

            migrationBuilder.DropForeignKey(
                name: "fk_comments_comments_parent_id",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "fk_custom_role_permissions_custom_roles_custom_role_id",
                schema: "governance",
                table: "custom_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_dashboard_widgets_dashboards_dashboard_id",
                schema: "reporting",
                table: "dashboard_widgets");

            migrationBuilder.DropForeignKey(
                name: "fk_field_options_board_fields_field_id",
                schema: "work",
                table: "field_options");

            migrationBuilder.DropForeignKey(
                name: "fk_form_questions_forms_form_id",
                schema: "work",
                table: "form_questions");

            migrationBuilder.DropForeignKey(
                name: "fk_form_submissions_boards_board_id",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropForeignKey(
                name: "fk_form_submissions_forms_form_id",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_scopes_integration_connections_connection_id",
                schema: "integration",
                table: "integration_scopes");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_secret_versions_integration_connections_connect",
                schema: "integration",
                table: "integration_secret_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_item_dependencies_board_items_predecessor_item_id",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropForeignKey(
                name: "fk_item_dependencies_board_items_successor_item_id",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropForeignKey(
                name: "fk_item_templates_boards_board_id",
                schema: "work",
                table: "item_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_labels_boards_board_id",
                schema: "work",
                table: "labels");

            migrationBuilder.DropForeignKey(
                name: "fk_mirror_value_snapshots_board_item_connections_connection_id",
                schema: "work",
                table: "mirror_value_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_oauth_accounts_users_user_id",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropForeignKey(
                name: "fk_pages_pages_parent_id",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropForeignKey(
                name: "fk_plan_limits_plans_plan_id",
                schema: "billing",
                table: "plan_limits");

            migrationBuilder.DropForeignKey(
                name: "fk_rollup_snapshots_board_items_item_id",
                schema: "work",
                table: "rollup_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_saved_filter_rules_saved_filters_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_saved_filter_sort_rules_saved_filters_saved_filter_id",
                schema: "work",
                table: "saved_filter_sort_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_saved_filters_board_views_view_id",
                schema: "work",
                table: "saved_filters");

            migrationBuilder.DropForeignKey(
                name: "fk_team_members_teams_team_id",
                schema: "workspace",
                table: "team_members");

            migrationBuilder.DropForeignKey(
                name: "fk_time_tracking_entries_board_items_item_id",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_time_tracking_entries_boards_board_id",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_usage_metric_history_usage_metrics_metric_id",
                schema: "billing",
                table: "usage_metric_history");

            migrationBuilder.DropForeignKey(
                name: "fk_user_profiles_users_user_id",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropTable(
                name: "activity_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "activity_read_states",
                schema: "activity");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "domain_event_logs",
                schema: "events");

            migrationBuilder.DropTable(
                name: "email_delivery_attempts",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "email_outbox",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "feature_usage_daily",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "notification_preferences",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notification_recipients",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "outbox_delivery_attempts",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "processed_events",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "security_events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "workspace_access_grants",
                schema: "authz");

            migrationBuilder.DropTable(
                name: "workspace_activity_logs",
                schema: "activity");

            migrationBuilder.DropTable(
                name: "workspace_usage_daily",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "notification_items",
                schema: "notifications");

            migrationBuilder.DropPrimaryKey(
                name: "pk_workspaces",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropPrimaryKey(
                name: "pk_workspace_policies",
                schema: "governance",
                table: "workspace_policies");

            migrationBuilder.DropPrimaryKey(
                name: "pk_workspace_members",
                schema: "workspace",
                table: "workspace_members");

            migrationBuilder.DropPrimaryKey(
                name: "pk_workspace_invitations",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_workspace_feature_usages",
                schema: "billing",
                table: "workspace_feature_usages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_workload_allocations",
                schema: "work",
                table: "workload_allocations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_webhook_subscriptions",
                schema: "integration",
                table: "webhook_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_webhook_deliveries",
                schema: "integration",
                table: "webhook_deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                schema: "identity",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_sessions",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_security_settings",
                schema: "identity",
                table: "user_security_settings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_profiles",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_mfa_methods",
                schema: "identity",
                table: "user_mfa_methods");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_login_attempts",
                schema: "identity",
                table: "user_login_attempts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_usage_metrics",
                schema: "billing",
                table: "usage_metrics");

            migrationBuilder.DropPrimaryKey(
                name: "pk_usage_metric_history",
                schema: "billing",
                table: "usage_metric_history");

            migrationBuilder.DropPrimaryKey(
                name: "pk_unread_counters",
                schema: "collab",
                table: "unread_counters");

            migrationBuilder.DropPrimaryKey(
                name: "pk_time_tracking_entries",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_teams",
                schema: "workspace",
                table: "teams");

            migrationBuilder.DropPrimaryKey(
                name: "pk_team_members",
                schema: "workspace",
                table: "team_members");

            migrationBuilder.DropPrimaryKey(
                name: "pk_subscriptions",
                schema: "billing",
                table: "subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sso_providers",
                schema: "identity",
                table: "sso_providers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_spaces",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropPrimaryKey(
                name: "pk_share_links",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropPrimaryKey(
                name: "pk_security_events",
                schema: "governance",
                table: "security_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_search_index_jobs",
                schema: "search",
                table: "search_index_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_search_documents",
                schema: "search",
                table: "search_documents");

            migrationBuilder.DropPrimaryKey(
                name: "pk_scim_directory_syncs",
                schema: "identity",
                table: "scim_directory_syncs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_scheduled_jobs",
                schema: "automation",
                table: "scheduled_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_saved_filters",
                schema: "work",
                table: "saved_filters");

            migrationBuilder.DropPrimaryKey(
                name: "pk_saved_filter_sort_rules",
                schema: "work",
                table: "saved_filter_sort_rules");

            migrationBuilder.DropPrimaryKey(
                name: "pk_saved_filter_rules",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropPrimaryKey(
                name: "pk_rollup_snapshots",
                schema: "work",
                table: "rollup_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "pk_resource_watchers",
                schema: "collab",
                table: "resource_watchers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_resource_permissions",
                schema: "governance",
                table: "resource_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_resource_permission_inheritance_cache",
                schema: "governance",
                table: "resource_permission_inheritance_cache");

            migrationBuilder.DropPrimaryKey(
                name: "pk_resource_links",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropPrimaryKey(
                name: "pk_reporting_snapshots",
                schema: "reporting",
                table: "reporting_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "pk_relation_field_configs",
                schema: "work",
                table: "relation_field_configs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_reactions",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_processed_events",
                schema: "ops",
                table: "processed_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_presence_sessions",
                schema: "collab",
                table: "presence_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_plans",
                schema: "billing",
                table: "plans");

            migrationBuilder.DropPrimaryKey(
                name: "pk_plan_limits",
                schema: "billing",
                table: "plan_limits");

            migrationBuilder.DropPrimaryKey(
                name: "pk_permission_templates",
                schema: "governance",
                table: "permission_templates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_permission_rules",
                schema: "governance",
                table: "permission_rules");

            migrationBuilder.DropPrimaryKey(
                name: "pk_payment_methods",
                schema: "billing",
                table: "payment_methods");

            migrationBuilder.DropPrimaryKey(
                name: "pk_password_reset_tokens",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_pages",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_page_templates",
                schema: "docs",
                table: "page_templates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_outbox_messages",
                schema: "ops",
                table: "outbox_messages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_oauth_accounts",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_notifications",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropPrimaryKey(
                name: "pk_notification_preferences",
                schema: "collab",
                table: "notification_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "pk_notification_deliveries",
                schema: "collab",
                table: "notification_deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mirror_value_snapshots",
                schema: "work",
                table: "mirror_value_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mentions",
                schema: "collab",
                table: "mentions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_member_role_assignments",
                schema: "governance",
                table: "member_role_assignments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_labels",
                schema: "work",
                table: "labels");

            migrationBuilder.DropPrimaryKey(
                name: "pk_job_locks",
                schema: "ops",
                table: "job_locks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_item_templates",
                schema: "work",
                table: "item_templates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_item_dependencies",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropPrimaryKey(
                name: "pk_invoices",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_integration_sync_cursors",
                schema: "integration",
                table: "integration_sync_cursors");

            migrationBuilder.DropPrimaryKey(
                name: "pk_integration_secret_versions",
                schema: "integration",
                table: "integration_secret_versions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_integration_scopes",
                schema: "integration",
                table: "integration_scopes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_integration_connections",
                schema: "integration",
                table: "integration_connections");

            migrationBuilder.DropPrimaryKey(
                name: "pk_inbound_webhook_events",
                schema: "integration",
                table: "inbound_webhook_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_import_jobs",
                schema: "ops",
                table: "import_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_idempotency_keys",
                schema: "ops",
                table: "idempotency_keys");

            migrationBuilder.DropPrimaryKey(
                name: "pk_formula_dependencies",
                schema: "work",
                table: "formula_dependencies");

            migrationBuilder.DropPrimaryKey(
                name: "pk_forms",
                schema: "work",
                table: "forms");

            migrationBuilder.DropPrimaryKey(
                name: "pk_form_submissions",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_form_questions",
                schema: "work",
                table: "form_questions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_field_permissions",
                schema: "governance",
                table: "field_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_field_options",
                schema: "work",
                table: "field_options");

            migrationBuilder.DropPrimaryKey(
                name: "pk_feature_usage_ledger",
                schema: "billing",
                table: "feature_usage_ledger");

            migrationBuilder.DropPrimaryKey(
                name: "pk_export_jobs",
                schema: "ops",
                table: "export_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_entitlements",
                schema: "billing",
                table: "entitlements");

            migrationBuilder.DropPrimaryKey(
                name: "pk_email_verification_tokens",
                schema: "identity",
                table: "email_verification_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_document_versions",
                schema: "docs",
                table: "document_versions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_dashboards",
                schema: "reporting",
                table: "dashboards");

            migrationBuilder.DropPrimaryKey(
                name: "pk_dashboard_widgets",
                schema: "reporting",
                table: "dashboard_widgets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_dashboard_sources",
                schema: "reporting",
                table: "dashboard_sources");

            migrationBuilder.DropPrimaryKey(
                name: "pk_custom_roles",
                schema: "governance",
                table: "custom_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_custom_role_permissions",
                schema: "governance",
                table: "custom_role_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_comments",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_checklists",
                schema: "work",
                table: "checklists");

            migrationBuilder.DropPrimaryKey(
                name: "pk_checklist_items",
                schema: "work",
                table: "checklist_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_calendar_integrations",
                schema: "integration",
                table: "calendar_integrations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_calendar_events",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_calendar_event_links",
                schema: "integration",
                table: "calendar_event_links");

            migrationBuilder.DropPrimaryKey(
                name: "pk_boards",
                schema: "work",
                table: "boards");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_views",
                schema: "work",
                table: "board_views");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_view_user_preferences",
                schema: "work",
                table: "board_view_user_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_view_pins",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_templates",
                schema: "work",
                table: "board_templates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_subscribers",
                schema: "work",
                table: "board_subscribers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_relations",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_members",
                schema: "work",
                table: "board_members");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_items",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_item_values",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_item_members",
                schema: "work",
                table: "board_item_members");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_item_links",
                schema: "work",
                table: "board_item_links");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_item_labels",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_item_connections",
                schema: "work",
                table: "board_item_connections");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_groups",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_fields",
                schema: "work",
                table: "board_fields");

            migrationBuilder.DropPrimaryKey(
                name: "pk_blocks",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_billing_events",
                schema: "billing",
                table: "billing_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_automation_templates",
                schema: "automation",
                table: "automation_templates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_automation_rules",
                schema: "automation",
                table: "automation_rules");

            migrationBuilder.DropPrimaryKey(
                name: "pk_automation_executions",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_automation_execution_steps",
                schema: "automation",
                table: "automation_execution_steps");

            migrationBuilder.DropPrimaryKey(
                name: "pk_audit_retention_policies",
                schema: "governance",
                table: "audit_retention_policies");

            migrationBuilder.DropPrimaryKey(
                name: "pk_audit_logs",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_attachments",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_approval_steps",
                schema: "work",
                table: "approval_steps");

            migrationBuilder.DropPrimaryKey(
                name: "pk_approval_requests",
                schema: "work",
                table: "approval_requests");

            migrationBuilder.DropPrimaryKey(
                name: "pk_api_tokens",
                schema: "identity",
                table: "api_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_ai_agents",
                schema: "automation",
                table: "ai_agents");

            migrationBuilder.DropPrimaryKey(
                name: "pk_ai_agent_runs",
                schema: "automation",
                table: "ai_agent_runs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_activity_logs",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.RenameColumn(
                name: "max_retries",
                schema: "integration",
                table: "webhook_deliveries",
                newName: "MaxRetries");

            migrationBuilder.RenameColumn(
                name: "failure_reason",
                schema: "integration",
                table: "webhook_deliveries",
                newName: "FailureReason");

            migrationBuilder.RenameColumn(
                name: "failed_at",
                schema: "integration",
                table: "webhook_deliveries",
                newName: "FailedAt");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                schema: "identity",
                table: "user_sessions",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "expired_at",
                schema: "identity",
                table: "user_sessions",
                newName: "ExpiredAt");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                schema: "identity",
                table: "user_profiles",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "restored_by",
                schema: "identity",
                table: "user_profiles",
                newName: "RestoredBy");

            migrationBuilder.RenameColumn(
                name: "restored_at",
                schema: "identity",
                table: "user_profiles",
                newName: "RestoredAt");

            migrationBuilder.RenameColumn(
                name: "preferences_json",
                schema: "identity",
                table: "user_profiles",
                newName: "preferences");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                schema: "identity",
                table: "user_profiles",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                schema: "identity",
                table: "user_profiles",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "delete_reason",
                schema: "identity",
                table: "user_profiles",
                newName: "DeleteReason");

            migrationBuilder.RenameColumn(
                name: "created_by",
                schema: "identity",
                table: "user_profiles",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "identity",
                table: "user_profiles",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_time_tracking_entries_board_id",
                schema: "work",
                table: "time_tracking_entries",
                newName: "IX_time_tracking_entries_board_id");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "workspace",
                table: "team_members",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "workspace_member_id",
                schema: "workspace",
                table: "team_members",
                newName: "WorkspaceMemberId");

            migrationBuilder.RenameColumn(
                name: "workspace_id",
                schema: "workspace",
                table: "team_members",
                newName: "WorkspaceId");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                schema: "workspace",
                table: "team_members",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "workspace",
                table: "team_members",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                schema: "workspace",
                table: "team_members",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "workspace",
                table: "team_members",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "space_type",
                schema: "workspace",
                table: "spaces",
                newName: "SpaceType");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                schema: "governance",
                table: "security_events",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "governance",
                table: "security_events",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "restored_by",
                schema: "governance",
                table: "security_events",
                newName: "RestoredBy");

            migrationBuilder.RenameColumn(
                name: "restored_at",
                schema: "governance",
                table: "security_events",
                newName: "RestoredAt");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                schema: "governance",
                table: "security_events",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                schema: "governance",
                table: "security_events",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "delete_reason",
                schema: "governance",
                table: "security_events",
                newName: "DeleteReason");

            migrationBuilder.RenameColumn(
                name: "created_by",
                schema: "governance",
                table: "security_events",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "governance",
                table: "security_events",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_saved_filters_view_id",
                schema: "work",
                table: "saved_filters",
                newName: "IX_saved_filters_view_id");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "work",
                table: "saved_filter_sort_rules",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "ix_saved_filter_sort_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_sort_rules",
                newName: "IX_saved_filter_sort_rules_saved_filter_id");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "work",
                table: "saved_filter_rules",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "ix_saved_filter_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules",
                newName: "IX_saved_filter_rules_saved_filter_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                schema: "collab",
                table: "reactions",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "collab",
                table: "reactions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "restored_by",
                schema: "collab",
                table: "reactions",
                newName: "RestoredBy");

            migrationBuilder.RenameColumn(
                name: "restored_at",
                schema: "collab",
                table: "reactions",
                newName: "RestoredAt");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                schema: "collab",
                table: "reactions",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                schema: "collab",
                table: "reactions",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "delete_reason",
                schema: "collab",
                table: "reactions",
                newName: "DeleteReason");

            migrationBuilder.RenameColumn(
                name: "created_by",
                schema: "collab",
                table: "reactions",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "collab",
                table: "reactions",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_form_submissions_board_id",
                schema: "work",
                table: "form_submissions",
                newName: "IX_form_submissions_board_id");

            migrationBuilder.RenameColumn(
                name: "config",
                schema: "work",
                table: "form_questions",
                newName: "Config");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "governance",
                table: "custom_roles",
                newName: "Status");

            migrationBuilder.RenameIndex(
                name: "ix_comments_parent_id",
                schema: "collab",
                table: "comments",
                newName: "IX_comments_parent_id");

            migrationBuilder.RenameColumn(
                name: "space_id",
                schema: "work",
                table: "boards",
                newName: "SpaceId");

            migrationBuilder.RenameColumn(
                name: "item_sequence",
                schema: "work",
                table: "boards",
                newName: "ItemSequence");

            migrationBuilder.RenameColumn(
                name: "item_key_prefix",
                schema: "work",
                table: "boards",
                newName: "ItemKeyPrefix");

            migrationBuilder.RenameColumn(
                name: "default_item_group_id",
                schema: "work",
                table: "boards",
                newName: "DefaultItemGroupId");

            migrationBuilder.RenameColumn(
                name: "board_type",
                schema: "work",
                table: "boards",
                newName: "BoardType");

            migrationBuilder.RenameColumn(
                name: "board_family",
                schema: "work",
                table: "boards",
                newName: "BoardFamily");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "work",
                table: "board_view_sort_rules",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "ix_board_view_sort_rules_preference_id",
                schema: "work",
                table: "board_view_sort_rules",
                newName: "IX_board_view_sort_rules_preference_id");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "work",
                table: "board_view_filter_rules",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "ix_board_view_filter_rules_preference_id",
                schema: "work",
                table: "board_view_filter_rules",
                newName: "IX_board_view_filter_rules_preference_id");

            migrationBuilder.RenameColumn(
                name: "started_at",
                schema: "work",
                table: "board_items",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "parent_item_id",
                schema: "work",
                table: "board_items",
                newName: "ParentItemId");

            migrationBuilder.RenameColumn(
                name: "item_sequence",
                schema: "work",
                table: "board_items",
                newName: "ItemSequence");

            migrationBuilder.RenameColumn(
                name: "item_level",
                schema: "work",
                table: "board_items",
                newName: "ItemLevel");

            migrationBuilder.RenameColumn(
                name: "item_key",
                schema: "work",
                table: "board_items",
                newName: "ItemKey");

            migrationBuilder.RenameColumn(
                name: "due_at",
                schema: "work",
                table: "board_items",
                newName: "DueAt");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                schema: "work",
                table: "board_items",
                newName: "CompletedAt");

            migrationBuilder.RenameIndex(
                name: "ix_board_item_values_field_id",
                schema: "work",
                table: "board_item_values",
                newName: "IX_board_item_values_field_id");

            migrationBuilder.RenameColumn(
                name: "workspace_id",
                schema: "work",
                table: "board_item_members",
                newName: "WorkspaceId");

            migrationBuilder.RenameColumn(
                name: "board_id",
                schema: "work",
                table: "board_item_members",
                newName: "BoardId");

            migrationBuilder.RenameColumn(
                name: "assigned_by_user_id",
                schema: "work",
                table: "board_item_members",
                newName: "AssignedByUserId");

            migrationBuilder.RenameColumn(
                name: "workspace_id",
                schema: "work",
                table: "board_item_links",
                newName: "WorkspaceId");

            migrationBuilder.RenameColumn(
                name: "created_by",
                schema: "work",
                table: "board_item_links",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "work",
                table: "board_item_links",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "board_id",
                schema: "work",
                table: "board_item_links",
                newName: "BoardId");

            migrationBuilder.RenameColumn(
                name: "created_by",
                schema: "work",
                table: "board_item_labels",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "work",
                table: "board_item_labels",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_board_item_labels_label_id",
                schema: "work",
                table: "board_item_labels",
                newName: "IX_board_item_labels_label_id");

            migrationBuilder.RenameColumn(
                name: "mirror_source_json",
                schema: "work",
                table: "board_fields",
                newName: "MirrorSourceJson");

            migrationBuilder.RenameColumn(
                name: "is_sensitive",
                schema: "work",
                table: "board_fields",
                newName: "IsSensitive");

            migrationBuilder.RenameColumn(
                name: "is_formula",
                schema: "work",
                table: "board_fields",
                newName: "IsFormula");

            migrationBuilder.RenameColumn(
                name: "formula_expression",
                schema: "work",
                table: "board_fields",
                newName: "FormulaExpression");

            migrationBuilder.RenameColumn(
                name: "data_classification",
                schema: "work",
                table: "board_fields",
                newName: "DataClassification");

            migrationBuilder.AlterColumn<bool>(
                name: "is_system",
                schema: "governance",
                table: "custom_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspaces",
                schema: "workspace",
                table: "workspaces",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_policies",
                schema: "governance",
                table: "workspace_policies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_members",
                schema: "workspace",
                table: "workspace_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_invitations",
                schema: "workspace",
                table: "workspace_invitations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_feature_usages",
                schema: "billing",
                table: "workspace_feature_usages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workload_allocations",
                schema: "work",
                table: "workload_allocations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_webhook_subscriptions",
                schema: "integration",
                table: "webhook_subscriptions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_webhook_deliveries",
                schema: "integration",
                table: "webhook_deliveries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                schema: "identity",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_sessions",
                schema: "identity",
                table: "user_sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_security_settings",
                schema: "identity",
                table: "user_security_settings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_profiles",
                schema: "identity",
                table: "user_profiles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_mfa_methods",
                schema: "identity",
                table: "user_mfa_methods",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_login_attempts",
                schema: "identity",
                table: "user_login_attempts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usage_metrics",
                schema: "billing",
                table: "usage_metrics",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usage_metric_history",
                schema: "billing",
                table: "usage_metric_history",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_unread_counters",
                schema: "collab",
                table: "unread_counters",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_time_tracking_entries",
                schema: "work",
                table: "time_tracking_entries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_teams",
                schema: "workspace",
                table: "teams",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_team_members",
                schema: "workspace",
                table: "team_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_subscriptions",
                schema: "billing",
                table: "subscriptions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sso_providers",
                schema: "identity",
                table: "sso_providers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_spaces",
                schema: "workspace",
                table: "spaces",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_share_links",
                schema: "governance",
                table: "share_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_security_events",
                schema: "governance",
                table: "security_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_search_index_jobs",
                schema: "search",
                table: "search_index_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_search_documents",
                schema: "search",
                table: "search_documents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_scim_directory_syncs",
                schema: "identity",
                table: "scim_directory_syncs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_scheduled_jobs",
                schema: "automation",
                table: "scheduled_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_saved_filters",
                schema: "work",
                table: "saved_filters",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_saved_filter_sort_rules",
                schema: "work",
                table: "saved_filter_sort_rules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_saved_filter_rules",
                schema: "work",
                table: "saved_filter_rules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_rollup_snapshots",
                schema: "work",
                table: "rollup_snapshots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_resource_watchers",
                schema: "collab",
                table: "resource_watchers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_resource_permissions",
                schema: "governance",
                table: "resource_permissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_resource_permission_inheritance_cache",
                schema: "governance",
                table: "resource_permission_inheritance_cache",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_resource_links",
                schema: "docs",
                table: "resource_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reporting_snapshots",
                schema: "reporting",
                table: "reporting_snapshots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_relation_field_configs",
                schema: "work",
                table: "relation_field_configs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reactions",
                schema: "collab",
                table: "reactions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_processed_events",
                schema: "ops",
                table: "processed_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_presence_sessions",
                schema: "collab",
                table: "presence_sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_plans",
                schema: "billing",
                table: "plans",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_plan_limits",
                schema: "billing",
                table: "plan_limits",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permission_templates",
                schema: "governance",
                table: "permission_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permission_rules",
                schema: "governance",
                table: "permission_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payment_methods",
                schema: "billing",
                table: "payment_methods",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_password_reset_tokens",
                schema: "identity",
                table: "password_reset_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_pages",
                schema: "docs",
                table: "pages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_page_templates",
                schema: "docs",
                table: "page_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_outbox_messages",
                schema: "ops",
                table: "outbox_messages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_oauth_accounts",
                schema: "identity",
                table: "oauth_accounts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notifications",
                schema: "collab",
                table: "notifications",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notification_preferences",
                schema: "collab",
                table: "notification_preferences",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notification_deliveries",
                schema: "collab",
                table: "notification_deliveries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_mirror_value_snapshots",
                schema: "work",
                table: "mirror_value_snapshots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_mentions",
                schema: "collab",
                table: "mentions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_member_role_assignments",
                schema: "governance",
                table: "member_role_assignments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_labels",
                schema: "work",
                table: "labels",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_job_locks",
                schema: "ops",
                table: "job_locks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_item_templates",
                schema: "work",
                table: "item_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_item_dependencies",
                schema: "work",
                table: "item_dependencies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_invoices",
                schema: "billing",
                table: "invoices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_integration_sync_cursors",
                schema: "integration",
                table: "integration_sync_cursors",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_integration_secret_versions",
                schema: "integration",
                table: "integration_secret_versions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_integration_scopes",
                schema: "integration",
                table: "integration_scopes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_integration_connections",
                schema: "integration",
                table: "integration_connections",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbound_webhook_events",
                schema: "integration",
                table: "inbound_webhook_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_import_jobs",
                schema: "ops",
                table: "import_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_idempotency_keys",
                schema: "ops",
                table: "idempotency_keys",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_formula_dependencies",
                schema: "work",
                table: "formula_dependencies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_forms",
                schema: "work",
                table: "forms",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_form_submissions",
                schema: "work",
                table: "form_submissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_form_questions",
                schema: "work",
                table: "form_questions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_field_permissions",
                schema: "governance",
                table: "field_permissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_field_options",
                schema: "work",
                table: "field_options",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_feature_usage_ledger",
                schema: "billing",
                table: "feature_usage_ledger",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_export_jobs",
                schema: "ops",
                table: "export_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_entitlements",
                schema: "billing",
                table: "entitlements",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_email_verification_tokens",
                schema: "identity",
                table: "email_verification_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_document_versions",
                schema: "docs",
                table: "document_versions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dashboards",
                schema: "reporting",
                table: "dashboards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dashboard_widgets",
                schema: "reporting",
                table: "dashboard_widgets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dashboard_sources",
                schema: "reporting",
                table: "dashboard_sources",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_custom_roles",
                schema: "governance",
                table: "custom_roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_custom_role_permissions",
                schema: "governance",
                table: "custom_role_permissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_comments",
                schema: "collab",
                table: "comments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_checklists",
                schema: "work",
                table: "checklists",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_checklist_items",
                schema: "work",
                table: "checklist_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_integrations",
                schema: "integration",
                table: "calendar_integrations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_events",
                schema: "integration",
                table: "calendar_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_event_links",
                schema: "integration",
                table: "calendar_event_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_boards",
                schema: "work",
                table: "boards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_views",
                schema: "work",
                table: "board_views",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_view_user_preferences",
                schema: "work",
                table: "board_view_user_preferences",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_view_pins",
                schema: "work",
                table: "board_view_pins",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_templates",
                schema: "work",
                table: "board_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_subscribers",
                schema: "work",
                table: "board_subscribers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_relations",
                schema: "work",
                table: "board_relations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_members",
                schema: "work",
                table: "board_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_items",
                schema: "work",
                table: "board_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_values",
                schema: "work",
                table: "board_item_values",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_members",
                schema: "work",
                table: "board_item_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_links",
                schema: "work",
                table: "board_item_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_labels",
                schema: "work",
                table: "board_item_labels",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_connections",
                schema: "work",
                table: "board_item_connections",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_groups",
                schema: "work",
                table: "board_groups",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_fields",
                schema: "work",
                table: "board_fields",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_blocks",
                schema: "docs",
                table: "blocks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_billing_events",
                schema: "billing",
                table: "billing_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_templates",
                schema: "automation",
                table: "automation_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_rules",
                schema: "automation",
                table: "automation_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_executions",
                schema: "automation",
                table: "automation_executions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_execution_steps",
                schema: "automation",
                table: "automation_execution_steps",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_retention_policies",
                schema: "governance",
                table: "audit_retention_policies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_logs",
                schema: "governance",
                table: "audit_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_attachments",
                schema: "collab",
                table: "attachments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_approval_steps",
                schema: "work",
                table: "approval_steps",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_approval_requests",
                schema: "work",
                table: "approval_requests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_api_tokens",
                schema: "identity",
                table: "api_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ai_agents",
                schema: "automation",
                table: "ai_agents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ai_agent_runs",
                schema: "automation",
                table: "ai_agent_runs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_activity_logs",
                schema: "collab",
                table: "activity_logs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_approval_steps_approval_requests_approval_request_id",
                schema: "work",
                table: "approval_steps",
                column: "approval_request_id",
                principalSchema: "work",
                principalTable: "approval_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_automation_execution_steps_automation_executions_execution_~",
                schema: "automation",
                table: "automation_execution_steps",
                column: "execution_id",
                principalSchema: "automation",
                principalTable: "automation_executions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_automation_executions_automation_rules_rule_id",
                schema: "automation",
                table: "automation_executions",
                column: "rule_id",
                principalSchema: "automation",
                principalTable: "automation_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_blocks_blocks_parent_id",
                schema: "docs",
                table: "blocks",
                column: "parent_id",
                principalSchema: "docs",
                principalTable: "blocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_blocks_pages_page_id",
                schema: "docs",
                table: "blocks",
                column: "page_id",
                principalSchema: "docs",
                principalTable: "pages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_fields_boards_board_id",
                schema: "work",
                table: "board_fields",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_groups_boards_board_id",
                schema: "work",
                table: "board_groups",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_connections_board_relations_relation_id",
                schema: "work",
                table: "board_item_connections",
                column: "relation_id",
                principalSchema: "work",
                principalTable: "board_relations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_labels_board_items_item_id",
                schema: "work",
                table: "board_item_labels",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_labels_labels_label_id",
                schema: "work",
                table: "board_item_labels",
                column: "label_id",
                principalSchema: "work",
                principalTable: "labels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_links_board_items_source_item_id",
                schema: "work",
                table: "board_item_links",
                column: "source_item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_members_board_items_item_id",
                schema: "work",
                table: "board_item_members",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_values_board_fields_field_id",
                schema: "work",
                table: "board_item_values",
                column: "field_id",
                principalSchema: "work",
                principalTable: "board_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_values_board_items_item_id",
                schema: "work",
                table: "board_item_values",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_items_board_groups_group_id",
                schema: "work",
                table: "board_items",
                column: "group_id",
                principalSchema: "work",
                principalTable: "board_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_board_items_boards_board_id",
                schema: "work",
                table: "board_items",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_members_boards_board_id",
                schema: "work",
                table: "board_members",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_relations_boards_source_board_id",
                schema: "work",
                table: "board_relations",
                column: "source_board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_board_relations_boards_target_board_id",
                schema: "work",
                table: "board_relations",
                column: "target_board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_board_subscribers_boards_board_id",
                schema: "work",
                table: "board_subscribers",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_view_filter_rules_board_view_user_preferences_prefere~",
                schema: "work",
                table: "board_view_filter_rules",
                column: "preference_id",
                principalSchema: "work",
                principalTable: "board_view_user_preferences",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_view_pins_board_views_board_view_id",
                schema: "work",
                table: "board_view_pins",
                column: "board_view_id",
                principalSchema: "work",
                principalTable: "board_views",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_view_pins_boards_board_id",
                schema: "work",
                table: "board_view_pins",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_view_sort_rules_board_view_user_preferences_preferenc~",
                schema: "work",
                table: "board_view_sort_rules",
                column: "preference_id",
                principalSchema: "work",
                principalTable: "board_view_user_preferences",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_view_user_preferences_board_views_view_id",
                schema: "work",
                table: "board_view_user_preferences",
                column: "view_id",
                principalSchema: "work",
                principalTable: "board_views",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_views_boards_board_id",
                schema: "work",
                table: "board_views",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_event_links_calendar_integrations_integration_id",
                schema: "integration",
                table: "calendar_event_links",
                column: "integration_id",
                principalSchema: "integration",
                principalTable: "calendar_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_events_calendar_integrations_integration_id",
                schema: "integration",
                table: "calendar_events",
                column: "integration_id",
                principalSchema: "integration",
                principalTable: "calendar_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_checklist_items_checklists_checklist_id",
                schema: "work",
                table: "checklist_items",
                column: "checklist_id",
                principalSchema: "work",
                principalTable: "checklists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_checklists_board_items_item_id",
                schema: "work",
                table: "checklists",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comments_comments_parent_id",
                schema: "collab",
                table: "comments",
                column: "parent_id",
                principalSchema: "collab",
                principalTable: "comments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_custom_role_permissions_custom_roles_custom_role_id",
                schema: "governance",
                table: "custom_role_permissions",
                column: "custom_role_id",
                principalSchema: "governance",
                principalTable: "custom_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dashboard_widgets_dashboards_dashboard_id",
                schema: "reporting",
                table: "dashboard_widgets",
                column: "dashboard_id",
                principalSchema: "reporting",
                principalTable: "dashboards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_field_options_board_fields_field_id",
                schema: "work",
                table: "field_options",
                column: "field_id",
                principalSchema: "work",
                principalTable: "board_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_form_questions_forms_form_id",
                schema: "work",
                table: "form_questions",
                column: "form_id",
                principalSchema: "work",
                principalTable: "forms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_form_submissions_boards_board_id",
                schema: "work",
                table: "form_submissions",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_form_submissions_forms_form_id",
                schema: "work",
                table: "form_submissions",
                column: "form_id",
                principalSchema: "work",
                principalTable: "forms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_integration_scopes_integration_connections_connection_id",
                schema: "integration",
                table: "integration_scopes",
                column: "connection_id",
                principalSchema: "integration",
                principalTable: "integration_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_integration_secret_versions_integration_connections_connect~",
                schema: "integration",
                table: "integration_secret_versions",
                column: "connection_id",
                principalSchema: "integration",
                principalTable: "integration_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_item_dependencies_board_items_predecessor_item_id",
                schema: "work",
                table: "item_dependencies",
                column: "predecessor_item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_item_dependencies_board_items_successor_item_id",
                schema: "work",
                table: "item_dependencies",
                column: "successor_item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_item_templates_boards_board_id",
                schema: "work",
                table: "item_templates",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_labels_boards_board_id",
                schema: "work",
                table: "labels",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_mirror_value_snapshots_board_item_connections_connection_id",
                schema: "work",
                table: "mirror_value_snapshots",
                column: "connection_id",
                principalSchema: "work",
                principalTable: "board_item_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_accounts_users_user_id",
                schema: "identity",
                table: "oauth_accounts",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pages_pages_parent_id",
                schema: "docs",
                table: "pages",
                column: "parent_id",
                principalSchema: "docs",
                principalTable: "pages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_plan_limits_plans_plan_id",
                schema: "billing",
                table: "plan_limits",
                column: "plan_id",
                principalSchema: "billing",
                principalTable: "plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rollup_snapshots_board_items_item_id",
                schema: "work",
                table: "rollup_snapshots",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_filter_rules_saved_filters_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules",
                column: "saved_filter_id",
                principalSchema: "work",
                principalTable: "saved_filters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_filter_sort_rules_saved_filters_saved_filter_id",
                schema: "work",
                table: "saved_filter_sort_rules",
                column: "saved_filter_id",
                principalSchema: "work",
                principalTable: "saved_filters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_filters_board_views_view_id",
                schema: "work",
                table: "saved_filters",
                column: "view_id",
                principalSchema: "work",
                principalTable: "board_views",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_team_members_teams_team_id",
                schema: "workspace",
                table: "team_members",
                column: "team_id",
                principalSchema: "workspace",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_time_tracking_entries_board_items_item_id",
                schema: "work",
                table: "time_tracking_entries",
                column: "item_id",
                principalSchema: "work",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_time_tracking_entries_boards_board_id",
                schema: "work",
                table: "time_tracking_entries",
                column: "board_id",
                principalSchema: "work",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_usage_metric_history_usage_metrics_metric_id",
                schema: "billing",
                table: "usage_metric_history",
                column: "metric_id",
                principalSchema: "billing",
                principalTable: "usage_metrics",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_profiles_users_user_id",
                schema: "identity",
                table: "user_profiles",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

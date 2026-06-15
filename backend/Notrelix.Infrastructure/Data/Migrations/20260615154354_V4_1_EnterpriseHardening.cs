using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Notrelix.Domain.Governance.Roles;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class V4_1_EnterpriseHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_approval_step_approval_request_approval_request_id",
                schema: "work",
                table: "approval_step");

            migrationBuilder.DropForeignKey(
                name: "FK_automation_execution_step_automation_execution_automation_e~",
                schema: "automation",
                table: "automation_execution_step");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_value_board_item_board_item_id",
                schema: "work",
                table: "board_item_value");

            migrationBuilder.DropForeignKey(
                name: "FK_calendar_event_link_calendar_integration_calendar_integrati~",
                schema: "integration",
                table: "calendar_event_link");

            migrationBuilder.DropForeignKey(
                name: "FK_checklist_item_checklist_checklist_id",
                schema: "work",
                table: "checklist_item");

            migrationBuilder.DropForeignKey(
                name: "FK_custom_role_permission_custom_role_custom_role_id",
                schema: "governance",
                table: "custom_role_permission");

            migrationBuilder.DropForeignKey(
                name: "FK_dashboard_widget_dashboard_dashboard_id",
                schema: "reporting",
                table: "dashboard_widget");

            migrationBuilder.DropForeignKey(
                name: "FK_field_option_board_field_board_field_id",
                schema: "work",
                table: "field_option");

            migrationBuilder.DropForeignKey(
                name: "FK_form_question_form_form_id",
                schema: "work",
                table: "form_question");

            migrationBuilder.DropForeignKey(
                name: "FK_integration_scope_integration_connection_integration_connec~",
                schema: "integration",
                table: "integration_scope");

            migrationBuilder.DropForeignKey(
                name: "FK_integration_secret_version_integration_connection_integrati~",
                schema: "integration",
                table: "integration_secret_version");

            migrationBuilder.DropForeignKey(
                name: "FK_o_auth_account_user_user_id",
                schema: "identity",
                table: "o_auth_account");

            migrationBuilder.DropForeignKey(
                name: "FK_plan_limit_plan_plan_id",
                schema: "billing",
                table: "plan_limit");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_filter_Rules_saved_filter_SavedFilterId",
                schema: "work",
                table: "saved_filter_Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_team_member_team_team_id",
                schema: "workspace",
                table: "team_member");

            migrationBuilder.DropForeignKey(
                name: "FK_usage_metric_history_usage_metric_usage_metric_id",
                schema: "billing",
                table: "usage_metric_history");

            migrationBuilder.DropForeignKey(
                name: "FK_user_profile_user_user_id",
                schema: "identity",
                table: "user_profile");

            migrationBuilder.DropTable(
                name: "automation_action",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "automation_condition",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "automation_trigger",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "board_view_user_preference_FilterRules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_view_user_preference_SortRules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "item_subscriber",
                schema: "work");

            migrationBuilder.DropTable(
                name: "saved_filter_SortRules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "unread_counter",
                schema: "collab");

            migrationBuilder.DropIndex(
                name: "IX_usage_metric_history_usage_metric_id",
                schema: "billing",
                table: "usage_metric_history");

            migrationBuilder.DropPrimaryKey(
                name: "PK_saved_filter_Rules",
                schema: "work",
                table: "saved_filter_Rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_policy",
                schema: "governance",
                table: "workspace_policy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_member",
                schema: "workspace",
                table: "workspace_member");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_invitation",
                schema: "workspace",
                table: "workspace_invitation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_feature_usage",
                schema: "billing",
                table: "workspace_feature_usage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace",
                schema: "workspace",
                table: "workspace");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workload_allocation",
                schema: "work",
                table: "workload_allocation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_webhook_subscription",
                schema: "integration",
                table: "webhook_subscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_webhook_delivery",
                schema: "integration",
                table: "webhook_delivery");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_session",
                schema: "identity",
                table: "user_session");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_profile",
                schema: "identity",
                table: "user_profile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_mfa_method",
                schema: "identity",
                table: "user_mfa_method");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_login_attempt",
                schema: "identity",
                table: "user_login_attempt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user",
                schema: "identity",
                table: "user");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usage_metric",
                schema: "billing",
                table: "usage_metric");

            migrationBuilder.DropPrimaryKey(
                name: "PK_time_tracking_entry",
                schema: "work",
                table: "time_tracking_entry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_team_member",
                schema: "workspace",
                table: "team_member");

            migrationBuilder.DropIndex(
                name: "IX_team_member_team_id",
                schema: "workspace",
                table: "team_member");

            migrationBuilder.DropPrimaryKey(
                name: "PK_team",
                schema: "workspace",
                table: "team");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subscription",
                schema: "billing",
                table: "subscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sso_provider",
                schema: "identity",
                table: "sso_provider");

            migrationBuilder.DropPrimaryKey(
                name: "PK_space",
                schema: "workspace",
                table: "space");

            migrationBuilder.DropPrimaryKey(
                name: "PK_share_link",
                schema: "governance",
                table: "share_link");

            migrationBuilder.DropPrimaryKey(
                name: "PK_security_event",
                schema: "governance",
                table: "security_event");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scim_directory_sync",
                schema: "identity",
                table: "scim_directory_sync");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scheduled_job",
                schema: "automation",
                table: "scheduled_job");

            migrationBuilder.DropPrimaryKey(
                name: "PK_saved_filter",
                schema: "work",
                table: "saved_filter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rollup_snapshot",
                schema: "work",
                table: "rollup_snapshot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_watcher",
                schema: "collab",
                table: "resource_watcher");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_permission",
                schema: "governance",
                table: "resource_permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_link",
                schema: "docs",
                table: "resource_link");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reporting_snapshot",
                schema: "reporting",
                table: "reporting_snapshot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_relation_field_config",
                schema: "work",
                table: "relation_field_config");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reaction",
                schema: "collab",
                table: "reaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_presence_session",
                schema: "collab",
                table: "presence_session");

            migrationBuilder.DropPrimaryKey(
                name: "PK_plan_limit",
                schema: "billing",
                table: "plan_limit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_plan",
                schema: "billing",
                table: "plan");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permission_template",
                schema: "governance",
                table: "permission_template");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permission_rule",
                schema: "governance",
                table: "permission_rule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payment_method",
                schema: "billing",
                table: "payment_method");

            migrationBuilder.DropPrimaryKey(
                name: "PK_password_reset_token",
                schema: "identity",
                table: "password_reset_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_page_template",
                schema: "docs",
                table: "page_template");

            migrationBuilder.DropPrimaryKey(
                name: "PK_page",
                schema: "docs",
                table: "page");

            migrationBuilder.DropPrimaryKey(
                name: "PK_o_auth_account",
                schema: "identity",
                table: "o_auth_account");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notification_preference",
                schema: "collab",
                table: "notification_preference");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notification_delivery",
                schema: "collab",
                table: "notification_delivery");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notification",
                schema: "collab",
                table: "notification");

            migrationBuilder.DropPrimaryKey(
                name: "PK_mirror_value_snapshot",
                schema: "work",
                table: "mirror_value_snapshot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_mention",
                schema: "collab",
                table: "mention");

            migrationBuilder.DropPrimaryKey(
                name: "PK_member_role_assignment",
                schema: "governance",
                table: "member_role_assignment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_label",
                schema: "work",
                table: "label");

            migrationBuilder.DropPrimaryKey(
                name: "PK_item_template",
                schema: "work",
                table: "item_template");

            migrationBuilder.DropPrimaryKey(
                name: "PK_item_dependency",
                schema: "work",
                table: "item_dependency");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invoice",
                schema: "billing",
                table: "invoice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_sync_cursor",
                schema: "integration",
                table: "integration_sync_cursor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_secret_version",
                schema: "integration",
                table: "integration_secret_version");

            migrationBuilder.DropIndex(
                name: "IX_integration_secret_version_integration_connection_id",
                schema: "integration",
                table: "integration_secret_version");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_scope",
                schema: "integration",
                table: "integration_scope");

            migrationBuilder.DropIndex(
                name: "IX_integration_scope_integration_connection_id",
                schema: "integration",
                table: "integration_scope");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_connection",
                schema: "integration",
                table: "integration_connection");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbound_webhook_event",
                schema: "integration",
                table: "inbound_webhook_event");

            migrationBuilder.DropPrimaryKey(
                name: "PK_formula_dependency",
                schema: "work",
                table: "formula_dependency");

            migrationBuilder.DropPrimaryKey(
                name: "PK_form_submission",
                schema: "work",
                table: "form_submission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_form_question",
                schema: "work",
                table: "form_question");

            migrationBuilder.DropIndex(
                name: "IX_form_question_form_id",
                schema: "work",
                table: "form_question");

            migrationBuilder.DropPrimaryKey(
                name: "PK_form",
                schema: "work",
                table: "form");

            migrationBuilder.DropPrimaryKey(
                name: "PK_field_permission",
                schema: "governance",
                table: "field_permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_field_option",
                schema: "work",
                table: "field_option");

            migrationBuilder.DropIndex(
                name: "IX_field_option_board_field_id",
                schema: "work",
                table: "field_option");

            migrationBuilder.DropPrimaryKey(
                name: "PK_entitlement",
                schema: "billing",
                table: "entitlement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_email_verification_token",
                schema: "identity",
                table: "email_verification_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_document_version",
                schema: "docs",
                table: "document_version");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dashboard_widget",
                schema: "reporting",
                table: "dashboard_widget");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dashboard_source",
                schema: "reporting",
                table: "dashboard_source");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dashboard",
                schema: "reporting",
                table: "dashboard");

            migrationBuilder.DropPrimaryKey(
                name: "PK_custom_role_permission",
                schema: "governance",
                table: "custom_role_permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_custom_role",
                schema: "governance",
                table: "custom_role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_comment",
                schema: "collab",
                table: "comment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_checklist_item",
                schema: "work",
                table: "checklist_item");

            migrationBuilder.DropIndex(
                name: "IX_checklist_item_checklist_id",
                schema: "work",
                table: "checklist_item");

            migrationBuilder.DropPrimaryKey(
                name: "PK_checklist",
                schema: "work",
                table: "checklist");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_integration",
                schema: "integration",
                table: "calendar_integration");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_event_link",
                schema: "integration",
                table: "calendar_event_link");

            migrationBuilder.DropIndex(
                name: "IX_calendar_event_link_calendar_integration_id",
                schema: "integration",
                table: "calendar_event_link");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_event",
                schema: "integration",
                table: "calendar_event");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_view_user_preference",
                schema: "work",
                table: "board_view_user_preference");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_view_pin",
                schema: "work",
                table: "board_view_pin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_view",
                schema: "work",
                table: "board_view");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_template",
                schema: "work",
                table: "board_template");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_subscriber",
                schema: "work",
                table: "board_subscriber");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_relation",
                schema: "work",
                table: "board_relation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_member",
                schema: "work",
                table: "board_member");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_value",
                schema: "work",
                table: "board_item_value");

            migrationBuilder.DropIndex(
                name: "IX_board_item_value_board_item_id",
                schema: "work",
                table: "board_item_value");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_member",
                schema: "work",
                table: "board_item_member");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_link",
                schema: "work",
                table: "board_item_link");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_label",
                schema: "work",
                table: "board_item_label");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_connection",
                schema: "work",
                table: "board_item_connection");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item",
                schema: "work",
                table: "board_item");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_group",
                schema: "work",
                table: "board_group");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_field",
                schema: "work",
                table: "board_field");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board",
                schema: "work",
                table: "board");

            migrationBuilder.DropPrimaryKey(
                name: "PK_block",
                schema: "docs",
                table: "block");

            migrationBuilder.DropPrimaryKey(
                name: "PK_billing_event",
                schema: "billing",
                table: "billing_event");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_template",
                schema: "automation",
                table: "automation_template");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_rule",
                schema: "automation",
                table: "automation_rule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_execution_step",
                schema: "automation",
                table: "automation_execution_step");

            migrationBuilder.DropIndex(
                name: "IX_automation_execution_step_automation_execution_id",
                schema: "automation",
                table: "automation_execution_step");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_execution",
                schema: "automation",
                table: "automation_execution");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_retention_policy",
                schema: "governance",
                table: "audit_retention_policy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_log",
                schema: "governance",
                table: "audit_log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_attachment",
                schema: "collab",
                table: "attachment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_approval_step",
                schema: "work",
                table: "approval_step");

            migrationBuilder.DropPrimaryKey(
                name: "PK_approval_request",
                schema: "work",
                table: "approval_request");

            migrationBuilder.DropPrimaryKey(
                name: "PK_api_token",
                schema: "identity",
                table: "api_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_agent_run",
                schema: "automation",
                table: "ai_agent_run");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_agent",
                schema: "automation",
                table: "ai_agent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_activity_log",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "user_security_settings");

            migrationBuilder.DropColumn(
                name: "usage_metric_id",
                schema: "billing",
                table: "usage_metric_history");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "workspace",
                table: "workspace_member");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "workspace",
                table: "workspace_invitation");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "billing",
                table: "workspace_feature_usage");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "workspace",
                table: "workspace");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "integration",
                table: "webhook_subscription");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "integration",
                table: "webhook_delivery");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "user_session");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "user_mfa_method");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "user_login_attempt");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "user");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "billing",
                table: "usage_metric");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "time_tracking_entry");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "workspace",
                table: "team");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "billing",
                table: "subscription");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "sso_provider");

            migrationBuilder.DropColumn(
                name: "metadata_json",
                schema: "identity",
                table: "sso_provider");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "workspace",
                table: "space");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "governance",
                table: "security_event");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "scim_directory_sync");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "automation",
                table: "scheduled_job");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "saved_filter");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "collab",
                table: "resource_watcher");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "governance",
                table: "resource_permission");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "docs",
                table: "resource_link");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "collab",
                table: "reaction");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "billing",
                table: "plan");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "governance",
                table: "permission_template");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "governance",
                table: "permission_rule");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "billing",
                table: "payment_method");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "password_reset_token");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "docs",
                table: "page_template");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "docs",
                table: "page");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "collab",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "label");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "item_template");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "item_dependency");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "billing",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "integration_connection_id",
                schema: "integration",
                table: "integration_secret_version");

            migrationBuilder.DropColumn(
                name: "integration_connection_id",
                schema: "integration",
                table: "integration_scope");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "integration",
                table: "integration_connection");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "integration",
                table: "inbound_webhook_event");

            migrationBuilder.DropColumn(
                name: "config_json",
                schema: "work",
                table: "form_question");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "form");

            migrationBuilder.DropColumn(
                name: "board_field_id",
                schema: "work",
                table: "field_option");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "billing",
                table: "entitlement");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "email_verification_token");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "docs",
                table: "document_version");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "reporting",
                table: "dashboard_source");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "reporting",
                table: "dashboard");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "governance",
                table: "custom_role");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "collab",
                table: "comment");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "checklist");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "integration",
                table: "calendar_integration");

            migrationBuilder.DropColumn(
                name: "calendar_integration_id",
                schema: "integration",
                table: "calendar_event_link");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "board_view_user_preference");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "board_view");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "board_template");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "board_relation");

            migrationBuilder.DropColumn(
                name: "board_item_id",
                schema: "work",
                table: "board_item_value");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "board_item");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "board_group");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "board_field");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "board");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "docs",
                table: "block");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "billing",
                table: "billing_event");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "automation",
                table: "automation_template");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "automation",
                table: "automation_rule");

            migrationBuilder.DropColumn(
                name: "last_run_at",
                schema: "automation",
                table: "automation_rule");

            migrationBuilder.DropColumn(
                name: "automation_execution_id",
                schema: "automation",
                table: "automation_execution_step");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "automation",
                table: "automation_execution");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "collab",
                table: "attachment");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "work",
                table: "approval_request");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "api_token");

            migrationBuilder.DropColumn(
                name: "scopes_json",
                schema: "identity",
                table: "api_token");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "automation",
                table: "ai_agent_run");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "automation",
                table: "ai_agent");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "delete_reason",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "restored_at",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "restored_by",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "collab",
                table: "activity_log");

            migrationBuilder.RenameTable(
                name: "saved_filter_Rules",
                schema: "work",
                newName: "saved_filter_rules",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "workspace_policy",
                schema: "governance",
                newName: "workspace_policies",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "workspace_member",
                schema: "workspace",
                newName: "workspace_members",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "workspace_invitation",
                schema: "workspace",
                newName: "workspace_invitations",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "workspace_feature_usage",
                schema: "billing",
                newName: "workspace_feature_usages",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "workspace",
                schema: "workspace",
                newName: "workspaces",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "workload_allocation",
                schema: "work",
                newName: "workload_allocations",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "webhook_subscription",
                schema: "integration",
                newName: "webhook_subscriptions",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "webhook_delivery",
                schema: "integration",
                newName: "webhook_deliveries",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "user_session",
                schema: "identity",
                newName: "user_sessions",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_profile",
                schema: "identity",
                newName: "user_profiles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_mfa_method",
                schema: "identity",
                newName: "user_mfa_methods",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_login_attempt",
                schema: "identity",
                newName: "user_login_attempts",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user",
                schema: "identity",
                newName: "users",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "usage_metric",
                schema: "billing",
                newName: "usage_metrics",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "time_tracking_entry",
                schema: "work",
                newName: "time_tracking_entries",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "team_member",
                schema: "workspace",
                newName: "team_members",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "team",
                schema: "workspace",
                newName: "teams",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "subscription",
                schema: "billing",
                newName: "subscriptions",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "sso_provider",
                schema: "identity",
                newName: "sso_providers",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "space",
                schema: "workspace",
                newName: "spaces",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "share_link",
                schema: "governance",
                newName: "share_links",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "security_event",
                schema: "governance",
                newName: "security_events",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "scim_directory_sync",
                schema: "identity",
                newName: "scim_directory_syncs",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "scheduled_job",
                schema: "automation",
                newName: "scheduled_jobs",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "saved_filter",
                schema: "work",
                newName: "saved_filters",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "rollup_snapshot",
                schema: "work",
                newName: "rollup_snapshots",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "resource_watcher",
                schema: "collab",
                newName: "resource_watchers",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "resource_permission",
                schema: "governance",
                newName: "resource_permissions",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "resource_link",
                schema: "docs",
                newName: "resource_links",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "reporting_snapshot",
                schema: "reporting",
                newName: "reporting_snapshots",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "relation_field_config",
                schema: "work",
                newName: "relation_field_configs",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "reaction",
                schema: "collab",
                newName: "reactions",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "presence_session",
                schema: "collab",
                newName: "presence_sessions",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "plan_limit",
                schema: "billing",
                newName: "plan_limits",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "plan",
                schema: "billing",
                newName: "plans",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "permission_template",
                schema: "governance",
                newName: "permission_templates",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "permission_rule",
                schema: "governance",
                newName: "permission_rules",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "payment_method",
                schema: "billing",
                newName: "payment_methods",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "password_reset_token",
                schema: "identity",
                newName: "password_reset_tokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "page_template",
                schema: "docs",
                newName: "page_templates",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "page",
                schema: "docs",
                newName: "pages",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "o_auth_account",
                schema: "identity",
                newName: "oauth_accounts",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "notification_preference",
                schema: "collab",
                newName: "notification_preferences",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "notification_delivery",
                schema: "collab",
                newName: "notification_deliveries",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "notification",
                schema: "collab",
                newName: "notifications",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "mirror_value_snapshot",
                schema: "work",
                newName: "mirror_value_snapshots",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "mention",
                schema: "collab",
                newName: "mentions",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "member_role_assignment",
                schema: "governance",
                newName: "member_role_assignments",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "label",
                schema: "work",
                newName: "labels",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "item_template",
                schema: "work",
                newName: "item_templates",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "item_dependency",
                schema: "work",
                newName: "item_dependencies",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "invoice",
                schema: "billing",
                newName: "invoices",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "integration_sync_cursor",
                schema: "integration",
                newName: "integration_sync_cursors",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "integration_secret_version",
                schema: "integration",
                newName: "integration_secret_versions",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "integration_scope",
                schema: "integration",
                newName: "integration_scopes",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "integration_connection",
                schema: "integration",
                newName: "integration_connections",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "inbound_webhook_event",
                schema: "integration",
                newName: "inbound_webhook_events",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "formula_dependency",
                schema: "work",
                newName: "formula_dependencies",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "form_submission",
                schema: "work",
                newName: "form_submissions",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "form_question",
                schema: "work",
                newName: "form_questions",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "form",
                schema: "work",
                newName: "forms",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "field_permission",
                schema: "governance",
                newName: "field_permissions",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "field_option",
                schema: "work",
                newName: "field_options",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "entitlement",
                schema: "billing",
                newName: "entitlements",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "email_verification_token",
                schema: "identity",
                newName: "email_verification_tokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "document_version",
                schema: "docs",
                newName: "document_versions",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "dashboard_widget",
                schema: "reporting",
                newName: "dashboard_widgets",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "dashboard_source",
                schema: "reporting",
                newName: "dashboard_sources",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "dashboard",
                schema: "reporting",
                newName: "dashboards",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "custom_role_permission",
                schema: "governance",
                newName: "custom_role_permissions",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "custom_role",
                schema: "governance",
                newName: "custom_roles",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "comment",
                schema: "collab",
                newName: "comments",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "checklist_item",
                schema: "work",
                newName: "checklist_items",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "checklist",
                schema: "work",
                newName: "checklists",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "calendar_integration",
                schema: "integration",
                newName: "calendar_integrations",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "calendar_event_link",
                schema: "integration",
                newName: "calendar_event_links",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "calendar_event",
                schema: "integration",
                newName: "calendar_events",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "board_view_user_preference",
                schema: "work",
                newName: "board_view_user_preferences",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_view_pin",
                schema: "work",
                newName: "board_view_pins",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_view",
                schema: "work",
                newName: "board_views",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_template",
                schema: "work",
                newName: "board_templates",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_subscriber",
                schema: "work",
                newName: "board_subscribers",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_relation",
                schema: "work",
                newName: "board_relations",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_member",
                schema: "work",
                newName: "board_members",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_value",
                schema: "work",
                newName: "board_item_values",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_member",
                schema: "work",
                newName: "board_item_members",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_link",
                schema: "work",
                newName: "board_item_links",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_label",
                schema: "work",
                newName: "board_item_labels",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_connection",
                schema: "work",
                newName: "board_item_connections",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item",
                schema: "work",
                newName: "board_items",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_group",
                schema: "work",
                newName: "board_groups",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_field",
                schema: "work",
                newName: "board_fields",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board",
                schema: "work",
                newName: "boards",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "block",
                schema: "docs",
                newName: "blocks",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "billing_event",
                schema: "billing",
                newName: "billing_events",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "automation_template",
                schema: "automation",
                newName: "automation_templates",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "automation_rule",
                schema: "automation",
                newName: "automation_rules",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "automation_execution_step",
                schema: "automation",
                newName: "automation_execution_steps",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "automation_execution",
                schema: "automation",
                newName: "automation_executions",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "audit_retention_policy",
                schema: "governance",
                newName: "audit_retention_policies",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "audit_log",
                schema: "governance",
                newName: "audit_logs",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "attachment",
                schema: "collab",
                newName: "attachments",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "approval_step",
                schema: "work",
                newName: "approval_steps",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "approval_request",
                schema: "work",
                newName: "approval_requests",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "api_token",
                schema: "identity",
                newName: "api_tokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "ai_agent_run",
                schema: "automation",
                newName: "ai_agent_runs",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "ai_agent",
                schema: "automation",
                newName: "ai_agents",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "activity_log",
                schema: "collab",
                newName: "activity_logs",
                newSchema: "collab");

            migrationBuilder.RenameColumn(
                name: "settings_json",
                schema: "identity",
                table: "user_security_settings",
                newName: "settings");

            migrationBuilder.RenameColumn(
                name: "SavedFilterId",
                schema: "work",
                table: "saved_filter_rules",
                newName: "saved_filter_id");

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
                name: "IX_user_profile_user_id",
                schema: "identity",
                table: "user_profiles",
                newName: "idx_user_profiles_user_id");

            migrationBuilder.RenameColumn(
                name: "key",
                schema: "billing",
                table: "usage_metrics",
                newName: "metric_key");

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
                name: "type",
                schema: "governance",
                table: "security_events",
                newName: "event_type");

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

            migrationBuilder.RenameColumn(
                name: "cursor_json",
                schema: "identity",
                table: "scim_directory_syncs",
                newName: "cursor");

            migrationBuilder.RenameColumn(
                name: "config_json",
                schema: "identity",
                table: "scim_directory_syncs",
                newName: "config");

            migrationBuilder.RenameColumn(
                name: "group_rule",
                schema: "work",
                table: "saved_filters",
                newName: "group_rule_id");

            migrationBuilder.RenameColumn(
                name: "level",
                schema: "collab",
                table: "resource_watchers",
                newName: "watch_level");

            migrationBuilder.RenameColumn(
                name: "level",
                schema: "governance",
                table: "resource_permissions",
                newName: "permission_level");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "docs",
                table: "resource_links",
                newName: "link_type");

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

            migrationBuilder.RenameColumn(
                name: "limit",
                schema: "billing",
                table: "plan_limits",
                newName: "limit_value");

            migrationBuilder.RenameIndex(
                name: "IX_plan_limit_plan_id",
                schema: "billing",
                table: "plan_limits",
                newName: "idx_plan_limits_plan_id");

            migrationBuilder.RenameIndex(
                name: "IX_o_auth_account_user_id",
                schema: "identity",
                table: "oauth_accounts",
                newName: "idx_oauth_accounts_user_id");

            migrationBuilder.RenameColumn(
                name: "mentioned_id",
                schema: "collab",
                table: "mentions",
                newName: "mentioned_user_id");

            migrationBuilder.RenameColumn(
                name: "cursor",
                schema: "integration",
                table: "integration_sync_cursors",
                newName: "cursor_value");

            migrationBuilder.RenameColumn(
                name: "limit",
                schema: "billing",
                table: "entitlements",
                newName: "limit_value");

            migrationBuilder.RenameIndex(
                name: "IX_dashboard_widget_dashboard_id",
                schema: "reporting",
                table: "dashboard_widgets",
                newName: "idx_dashboard_widgets_dashboard_id");

            migrationBuilder.RenameIndex(
                name: "IX_custom_role_permission_custom_role_id",
                schema: "governance",
                table: "custom_role_permissions",
                newName: "idx_custom_role_permissions_role_id");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "governance",
                table: "custom_roles",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "comment_status",
                schema: "collab",
                table: "comments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "e_tag",
                schema: "integration",
                table: "calendar_event_links",
                newName: "etag");

            migrationBuilder.RenameColumn(
                name: "SyncHash_Value",
                schema: "integration",
                table: "calendar_events",
                newName: "sync_hash");

            migrationBuilder.RenameColumn(
                name: "group_rule",
                schema: "work",
                table: "board_view_user_preferences",
                newName: "group_rule_id");

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

            migrationBuilder.RenameIndex(
                name: "IX_approval_step_approval_request_id",
                schema: "work",
                table: "approval_steps",
                newName: "idx_approval_steps_request_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                schema: "collab",
                table: "activity_logs",
                newName: "target_workspace_id");

            migrationBuilder.AlterColumn<string>(
                name: "preferred_mfa_method",
                schema: "identity",
                table: "user_security_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "settings",
                schema: "identity",
                table: "user_security_settings",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "work",
                table: "saved_filter_rules",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<Guid>(
                name: "field_id",
                schema: "work",
                table: "saved_filter_rules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "operator",
                schema: "work",
                table: "saved_filter_rules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "value",
                schema: "work",
                table: "saved_filter_rules",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reference_resource",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "note",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "feature_code",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "delta",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<bool>(
                name: "guest_allow_invites",
                schema: "governance",
                table: "workspace_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "resource_allow_public_sharing",
                schema: "governance",
                table: "workspace_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "sharing_allow_external_invite",
                schema: "governance",
                table: "workspace_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "sharing_allow_public",
                schema: "governance",
                table: "workspace_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "workspace",
                table: "workspace_invitations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "token",
                schema: "workspace",
                table: "workspace_invitations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "soft_limit",
                schema: "billing",
                table: "workspace_feature_usages",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reset_period",
                schema: "billing",
                table: "workspace_feature_usages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "hard_limit",
                schema: "billing",
                table: "workspace_feature_usages",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "current_usage",
                schema: "billing",
                table: "workspace_feature_usages",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "feature_code",
                schema: "billing",
                table: "workspace_feature_usages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                schema: "workspace",
                table: "workspaces",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workspace",
                table: "workspaces",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "workspace",
                table: "workspaces",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "settings_allow_public_sharing",
                schema: "workspace",
                table: "workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "settings_enforce_mfa",
                schema: "workspace",
                table: "workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "allocation_date",
                schema: "work",
                table: "workload_allocations",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "secret_hash",
                schema: "integration",
                table: "webhook_subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "url",
                schema: "integration",
                table: "webhook_subscriptions",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "payload",
                schema: "integration",
                table: "webhook_deliveries",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                schema: "integration",
                table: "webhook_deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "integration",
                table: "webhook_deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                schema: "integration",
                table: "webhook_deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "user_agent",
                schema: "identity",
                table: "user_sessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "user_sessions",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token_hash",
                schema: "identity",
                table: "user_sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "timezone",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "UTC",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "theme",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "system",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "preferences",
                schema: "identity",
                table: "user_profiles",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "vi",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "destination_masked",
                schema: "identity",
                table: "user_mfa_methods",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_agent",
                schema: "identity",
                table: "user_login_attempts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "user_login_attempts",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attempted_email",
                schema: "identity",
                table: "user_login_attempts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "identity",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "metric_key",
                schema: "billing",
                table: "usage_metrics",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "period_end",
                schema: "billing",
                table: "usage_metrics",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "period_start",
                schema: "billing",
                table: "usage_metrics",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workspace",
                table: "teams",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "workspace",
                table: "teams",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "sso_providers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "domain",
                schema: "identity",
                table: "sso_providers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "certificate_ref",
                schema: "identity",
                table: "sso_providers",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "entity_id",
                schema: "identity",
                table: "sso_providers",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "redirect_uri",
                schema: "identity",
                table: "sso_providers",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sso_url",
                schema: "identity",
                table: "sso_providers",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workspace",
                table: "spaces",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "workspace",
                table: "spaces",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delete_reason",
                schema: "governance",
                table: "share_links",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "governance",
                table: "share_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                schema: "governance",
                table: "share_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "restored_at",
                schema: "governance",
                table: "share_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                schema: "governance",
                table: "share_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "token_hash",
                schema: "governance",
                table: "share_links",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "governance",
                table: "share_links",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "governance",
                table: "security_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "governance",
                table: "security_events",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata",
                schema: "governance",
                table: "security_events",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "provider_name",
                schema: "identity",
                table: "scim_directory_syncs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "cursor",
                schema: "identity",
                table: "scim_directory_syncs",
                type: "text",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "config",
                schema: "identity",
                table: "scim_directory_syncs",
                type: "text",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "cron_expression",
                schema: "automation",
                table: "scheduled_jobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                schema: "automation",
                table: "scheduled_jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "UTC");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "saved_filters",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "value",
                schema: "work",
                table: "rollup_snapshots",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                schema: "collab",
                table: "resource_watchers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "target_type",
                schema: "collab",
                table: "resource_watchers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                schema: "governance",
                table: "resource_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "condition_json",
                schema: "governance",
                table: "resource_permissions",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                schema: "docs",
                table: "resource_links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                schema: "docs",
                table: "resource_links",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                schema: "docs",
                table: "resource_links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "target_type",
                schema: "docs",
                table: "resource_links",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "docs",
                table: "resource_links",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "report_type",
                schema: "reporting",
                table: "reporting_snapshots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "data",
                schema: "reporting",
                table: "reporting_snapshots",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "emoji",
                schema: "collab",
                table: "reactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "resource_id",
                schema: "collab",
                table: "reactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "resource_type",
                schema: "collab",
                table: "reactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "target_workspace_id",
                schema: "collab",
                table: "reactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "connection_id",
                schema: "collab",
                table: "presence_sessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "feature_code",
                schema: "billing",
                table: "plan_limits",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "billing",
                table: "plans",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "billing",
                table: "plans",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_amount",
                schema: "billing",
                table: "plans",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "price_currency",
                schema: "billing",
                table: "plans",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AlterColumn<string>(
                name: "target_resource_type",
                schema: "governance",
                table: "permission_templates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "permissions_json",
                schema: "governance",
                table: "permission_templates",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "governance",
                table: "permission_templates",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "governance",
                table: "permission_templates",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "subject_type",
                schema: "governance",
                table: "permission_rules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "subject_key",
                schema: "governance",
                table: "permission_rules",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "governance",
                table: "permission_rules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "scope_type",
                schema: "governance",
                table: "permission_rules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "resource_type",
                schema: "governance",
                table: "permission_rules",
                type: "integer",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                schema: "governance",
                table: "permission_rules",
                type: "integer",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "condition_json",
                schema: "governance",
                table: "permission_rules",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "governance",
                table: "permission_rules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "provider_method_id",
                schema: "billing",
                table: "payment_methods",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "last4",
                schema: "billing",
                table: "payment_methods",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "brand",
                schema: "billing",
                table: "payment_methods",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "page_snapshot",
                schema: "docs",
                table: "page_templates",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "docs",
                table: "page_templates",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "docs",
                table: "page_templates",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                schema: "docs",
                table: "page_templates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "blocks_snapshot",
                schema: "docs",
                table: "page_templates",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "docs",
                table: "pages",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "icon",
                schema: "docs",
                table: "pages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "📄",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "docs",
                table: "pages",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "raw_profile",
                schema: "identity",
                table: "oauth_accounts",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "provider_id",
                schema: "identity",
                table: "oauth_accounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "access_token_ref",
                schema: "identity",
                table: "oauth_accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token_ref",
                schema: "identity",
                table: "oauth_accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "token_expires_at",
                schema: "identity",
                table: "oauth_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider_message_id",
                schema: "collab",
                table: "notification_deliveries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "error_message",
                schema: "collab",
                table: "notification_deliveries",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "collab",
                table: "notifications",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                schema: "collab",
                table: "notifications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                schema: "collab",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_type",
                schema: "collab",
                table: "notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                schema: "collab",
                table: "mentions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                schema: "collab",
                table: "mentions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "source_workspace_id",
                schema: "collab",
                table: "mentions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "labels",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "color",
                schema: "work",
                table: "labels",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "values",
                schema: "work",
                table: "item_templates",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "item_templates",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "number",
                schema: "billing",
                table: "invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "amount_currency",
                schema: "billing",
                table: "invoices",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "amount_value",
                schema: "billing",
                table: "invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "resource_type",
                schema: "integration",
                table: "integration_sync_cursors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "cursor_value",
                schema: "integration",
                table: "integration_sync_cursors",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "version",
                schema: "integration",
                table: "integration_secret_versions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "secret_reference",
                schema: "integration",
                table: "integration_secret_versions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "scope",
                schema: "integration",
                table: "integration_scopes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "provider_account_id",
                schema: "integration",
                table: "integration_connections",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                schema: "integration",
                table: "inbound_webhook_events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "payload",
                schema: "integration",
                table: "inbound_webhook_events",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "external_event_id",
                schema: "integration",
                table: "inbound_webhook_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                schema: "integration",
                table: "inbound_webhook_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "submitter_email",
                schema: "work",
                table: "form_submissions",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "source_ip",
                schema: "work",
                table: "form_submissions",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "question_type",
                schema: "work",
                table: "form_questions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "question_key",
                schema: "work",
                table: "form_questions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "form_questions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "work",
                table: "form_questions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Config",
                schema: "work",
                table: "form_questions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                schema: "work",
                table: "forms",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "forms",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<long>(
                name: "version",
                schema: "governance",
                table: "field_permissions",
                type: "bigint",
                nullable: false,
                defaultValue: 1L,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "condition_json",
                schema: "governance",
                table: "field_permissions",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "field_options",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "field_options",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "color",
                schema: "work",
                table: "field_options",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "feature_code",
                schema: "billing",
                table: "entitlements",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "change_summary",
                schema: "docs",
                table: "document_versions",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "reporting",
                table: "dashboard_widgets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "reporting",
                table: "dashboard_widgets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "config",
                schema: "reporting",
                table: "dashboard_widgets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "pos_h",
                schema: "reporting",
                table: "dashboard_widgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "pos_w",
                schema: "reporting",
                table: "dashboard_widgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "pos_x",
                schema: "reporting",
                table: "dashboard_widgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "pos_y",
                schema: "reporting",
                table: "dashboard_widgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "filter",
                schema: "reporting",
                table: "dashboard_sources",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "reporting",
                table: "dashboards",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "reporting",
                table: "dashboards",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "conditions",
                schema: "governance",
                table: "custom_role_permissions",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "governance",
                table: "custom_role_permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "governance",
                table: "custom_roles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "governance",
                table: "custom_roles",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<IReadOnlyCollection<CustomRolePermission>>(
                name: "permissions",
                schema: "governance",
                table: "custom_roles",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "governance",
                table: "custom_roles",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                schema: "collab",
                table: "comments",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "anchor_offset",
                schema: "collab",
                table: "comments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "anchor_selector",
                schema: "collab",
                table: "comments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resource_id",
                schema: "collab",
                table: "comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "resource_type",
                schema: "collab",
                table: "comments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "target_workspace_id",
                schema: "collab",
                table: "comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "collab",
                table: "comments",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "checklist_items",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "checklist_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "checklists",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "checklists",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "external_event_id",
                schema: "integration",
                table: "calendar_event_links",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "etag",
                schema: "integration",
                table: "calendar_event_links",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "external_event_id",
                schema: "integration",
                table: "calendar_events",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "sync_hash",
                schema: "integration",
                table: "calendar_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "resource_id",
                schema: "integration",
                table: "calendar_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "resource_type",
                schema: "integration",
                table: "calendar_events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "target_workspace_id",
                schema: "integration",
                table: "calendar_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "board_views",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "config",
                schema: "work",
                table: "board_views",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "structure",
                schema: "work",
                table: "board_templates",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "board_templates",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<long>(
                name: "version",
                schema: "work",
                table: "board_relations",
                type: "bigint",
                nullable: false,
                defaultValue: 1L,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "value",
                schema: "work",
                table: "board_item_values",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                schema: "work",
                table: "board_item_links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "target_type",
                schema: "work",
                table: "board_item_links",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "board_items",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "board_groups",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "board_groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "color",
                schema: "work",
                table: "board_groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "board_fields",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "settings",
                schema: "work",
                table: "board_fields",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "visibility",
                schema: "work",
                table: "boards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Workspace",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "boards",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "work",
                table: "boards",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "background",
                schema: "work",
                table: "boards",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"type\":\"color\",\"value\":\"#0079BF\"}",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "docs",
                table: "blocks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "content",
                schema: "docs",
                table: "blocks",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "properties",
                schema: "docs",
                table: "blocks",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "raw_data",
                schema: "billing",
                table: "billing_events",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "provider_event_id",
                schema: "billing",
                table: "billing_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "automation",
                table: "automation_templates",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "automation",
                table: "automation_templates",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "definition",
                schema: "automation",
                table: "automation_templates",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "category",
                schema: "automation",
                table: "automation_templates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "trigger_event",
                schema: "automation",
                table: "automation_rules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "automation",
                table: "automation_rules",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "automation",
                table: "automation_rules",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "configuration",
                schema: "automation",
                table: "automation_rules",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "action_type",
                schema: "automation",
                table: "automation_rules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "payload",
                schema: "automation",
                table: "automation_executions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "retention_days",
                schema: "governance",
                table: "audit_retention_policies",
                type: "integer",
                nullable: false,
                defaultValue: 365,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "policy_json",
                schema: "governance",
                table: "audit_retention_policies",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "user_agent",
                schema: "governance",
                table: "audit_logs",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "resource_type",
                schema: "governance",
                table: "audit_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "governance",
                table: "audit_logs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "governance",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "metadata_ip_address",
                schema: "governance",
                table: "audit_logs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_trace_id",
                schema: "governance",
                table: "audit_logs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_user_agent",
                schema: "governance",
                table: "audit_logs",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_workspace_id",
                schema: "governance",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                schema: "collab",
                table: "attachments",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "file_size",
                schema: "collab",
                table: "attachments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "mime_type",
                schema: "collab",
                table: "attachments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "resource_id",
                schema: "collab",
                table: "attachments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "resource_type",
                schema: "collab",
                table: "attachments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                schema: "collab",
                table: "attachments",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_workspace_id",
                schema: "collab",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "url",
                schema: "collab",
                table: "attachments",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "collab",
                table: "attachments",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "approval_requests",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                schema: "work",
                table: "approval_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "target_type",
                schema: "work",
                table: "approval_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "api_tokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "scopes",
                schema: "identity",
                table: "api_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "trigger_type",
                schema: "automation",
                table: "ai_agent_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "trigger_resource_type",
                schema: "automation",
                table: "ai_agent_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "output",
                schema: "automation",
                table: "ai_agent_runs",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "input",
                schema: "automation",
                table: "ai_agent_runs",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "error",
                schema: "automation",
                table: "ai_agent_runs",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tool_permissions",
                schema: "automation",
                table: "ai_agents",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "automation",
                table: "ai_agents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "model_policy",
                schema: "automation",
                table: "ai_agents",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "instruction",
                schema: "automation",
                table: "ai_agents",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "automation",
                table: "ai_agents",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata",
                schema: "collab",
                table: "activity_logs",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "resource_id",
                schema: "collab",
                table: "activity_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "resource_type",
                schema: "collab",
                table: "activity_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_saved_filter_rules",
                schema: "work",
                table: "saved_filter_rules",
                column: "Id");

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
                name: "PK_workspaces",
                schema: "workspace",
                table: "workspaces",
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
                name: "PK_user_sessions",
                schema: "identity",
                table: "user_sessions",
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
                name: "PK_users",
                schema: "identity",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usage_metrics",
                schema: "billing",
                table: "usage_metrics",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_time_tracking_entries",
                schema: "work",
                table: "time_tracking_entries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_team_members",
                schema: "workspace",
                table: "team_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_teams",
                schema: "workspace",
                table: "teams",
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
                name: "PK_presence_sessions",
                schema: "collab",
                table: "presence_sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_plan_limits",
                schema: "billing",
                table: "plan_limits",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_plans",
                schema: "billing",
                table: "plans",
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
                name: "PK_page_templates",
                schema: "docs",
                table: "page_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_pages",
                schema: "docs",
                table: "pages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_oauth_accounts",
                schema: "identity",
                table: "oauth_accounts",
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
                name: "PK_notifications",
                schema: "collab",
                table: "notifications",
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
                name: "PK_formula_dependencies",
                schema: "work",
                table: "formula_dependencies",
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
                name: "PK_forms",
                schema: "work",
                table: "forms",
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
                name: "PK_dashboards",
                schema: "reporting",
                table: "dashboards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_custom_role_permissions",
                schema: "governance",
                table: "custom_role_permissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_custom_roles",
                schema: "governance",
                table: "custom_roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_comments",
                schema: "collab",
                table: "comments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_checklist_items",
                schema: "work",
                table: "checklist_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_checklists",
                schema: "work",
                table: "checklists",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_integrations",
                schema: "integration",
                table: "calendar_integrations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_event_links",
                schema: "integration",
                table: "calendar_event_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_events",
                schema: "integration",
                table: "calendar_events",
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
                name: "PK_board_views",
                schema: "work",
                table: "board_views",
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
                name: "PK_board_items",
                schema: "work",
                table: "board_items",
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
                name: "PK_boards",
                schema: "work",
                table: "boards",
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
                name: "PK_automation_execution_steps",
                schema: "automation",
                table: "automation_execution_steps",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_executions",
                schema: "automation",
                table: "automation_executions",
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
                name: "PK_ai_agent_runs",
                schema: "automation",
                table: "ai_agent_runs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ai_agents",
                schema: "automation",
                table: "ai_agents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_activity_logs",
                schema: "collab",
                table: "activity_logs",
                column: "id");

            migrationBuilder.CreateTable(
                name: "board_view_filter_rules",
                schema: "work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    preference_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_view_filter_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_board_view_filter_rules_board_view_user_preferences_prefere~",
                        column: x => x.preference_id,
                        principalSchema: "work",
                        principalTable: "board_view_user_preferences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_view_sort_rules",
                schema: "work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preference_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_view_sort_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_board_view_sort_rules_board_view_user_preferences_preferenc~",
                        column: x => x.preference_id,
                        principalSchema: "work",
                        principalTable: "board_view_user_preferences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_filter_sort_rules",
                schema: "work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    saved_filter_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_filter_sort_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_filter_sort_rules_saved_filters_saved_filter_id",
                        column: x => x.saved_filter_id,
                        principalSchema: "work",
                        principalTable: "saved_filters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_security_settings_user_id",
                schema: "identity",
                table: "user_security_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_usage_metric_history_metric_id",
                schema: "billing",
                table: "usage_metric_history",
                column: "metric_id");

            migrationBuilder.CreateIndex(
                name: "IX_saved_filter_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules",
                column: "saved_filter_id");

            migrationBuilder.CreateIndex(
                name: "idx_feature_usage_ledger_workspace_id",
                schema: "billing",
                table: "feature_usage_ledger",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_workspace_policies_workspace_id",
                schema: "governance",
                table: "workspace_policies",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_workspace_members_user_id",
                schema: "workspace",
                table: "workspace_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_workspace_members_workspace_user",
                schema: "workspace",
                table: "workspace_members",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_workspace_invitations_email",
                schema: "workspace",
                table: "workspace_invitations",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_workspace_invitations_workspace_id",
                schema: "workspace",
                table: "workspace_invitations",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_workspace_feature_usages_workspace_id",
                schema: "billing",
                table: "workspace_feature_usages",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_workspaces_name",
                schema: "workspace",
                table: "workspaces",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_workspaces_slug",
                schema: "workspace",
                table: "workspaces",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_workload_allocations_item_id",
                schema: "work",
                table: "workload_allocations",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_workload_allocations_user_date",
                schema: "work",
                table: "workload_allocations",
                columns: new[] { "user_id", "allocation_date" });

            migrationBuilder.CreateIndex(
                name: "idx_webhook_subscriptions_workspace_id",
                schema: "integration",
                table: "webhook_subscriptions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_webhook_deliveries_subscription_id",
                schema: "integration",
                table: "webhook_deliveries",
                column: "webhook_subscription_id");

            migrationBuilder.CreateIndex(
                name: "idx_webhook_deliveries_workspace_id",
                schema: "integration",
                table: "webhook_deliveries",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_sessions_expires",
                schema: "identity",
                table: "user_sessions",
                column: "expires_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_user_sessions_user_id",
                schema: "identity",
                table: "user_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_mfa_methods_user_id",
                schema: "identity",
                table: "user_mfa_methods",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_login_attempts_occurred_at",
                schema: "identity",
                table: "user_login_attempts",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_login_attempts_user_id",
                schema: "identity",
                table: "user_login_attempts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_usage_metrics_workspace_id",
                schema: "billing",
                table: "usage_metrics",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_time_tracking_item_user",
                schema: "work",
                table: "time_tracking_entries",
                columns: new[] { "item_id", "user_id" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_time_tracking_status",
                schema: "work",
                table: "time_tracking_entries",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_time_tracking_entries_board_id",
                schema: "work",
                table: "time_tracking_entries",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "idx_team_members_team_user",
                schema: "workspace",
                table: "team_members",
                columns: new[] { "team_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_teams_workspace_id",
                schema: "workspace",
                table: "teams",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_subscriptions_plan_id",
                schema: "billing",
                table: "subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "idx_subscriptions_workspace_id",
                schema: "billing",
                table: "subscriptions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_sso_providers_workspace_id",
                schema: "identity",
                table: "sso_providers",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_spaces_workspace_id",
                schema: "workspace",
                table: "spaces",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_share_links_resource_id",
                schema: "governance",
                table: "share_links",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "idx_security_events_occurred_at",
                schema: "governance",
                table: "security_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "idx_security_events_workspace_id",
                schema: "governance",
                table: "security_events",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_scim_directory_syncs_workspace_id",
                schema: "identity",
                table: "scim_directory_syncs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_scheduled_jobs_rule_id",
                schema: "automation",
                table: "scheduled_jobs",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "idx_scheduled_jobs_status",
                schema: "automation",
                table: "scheduled_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_scheduled_jobs_workspace_id",
                schema: "automation",
                table: "scheduled_jobs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_saved_filters_board_name",
                schema: "work",
                table: "saved_filters",
                columns: new[] { "board_id", "name" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_saved_filters_view_id",
                schema: "work",
                table: "saved_filters",
                column: "view_id");

            migrationBuilder.CreateIndex(
                name: "idx_rollup_snapshots_item_field",
                schema: "work",
                table: "rollup_snapshots",
                columns: new[] { "item_id", "field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_resource_watchers_target",
                schema: "collab",
                table: "resource_watchers",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "idx_resource_watchers_user_workspace",
                schema: "collab",
                table: "resource_watchers",
                columns: new[] { "user_id", "workspace_id" });

            migrationBuilder.CreateIndex(
                name: "idx_resource_permissions_resource",
                schema: "governance",
                table: "resource_permissions",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "idx_resource_permissions_subject_id",
                schema: "governance",
                table: "resource_permissions",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "idx_resource_links_source",
                schema: "docs",
                table: "resource_links",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "idx_resource_links_target",
                schema: "docs",
                table: "resource_links",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "idx_resource_links_workspace_id",
                schema: "docs",
                table: "resource_links",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_reporting_snapshots_workspace_id",
                schema: "reporting",
                table: "reporting_snapshots",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_relation_field_configs_field_id",
                schema: "work",
                table: "relation_field_configs",
                column: "field_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_reactions_resource",
                schema: "collab",
                table: "reactions",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "idx_reactions_target_resource_id",
                schema: "collab",
                table: "reactions",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "idx_reactions_user_id",
                schema: "collab",
                table: "reactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_presence_sessions_last_seen_at",
                schema: "collab",
                table: "presence_sessions",
                column: "last_seen_at");

            migrationBuilder.CreateIndex(
                name: "idx_presence_sessions_workspace_user",
                schema: "collab",
                table: "presence_sessions",
                columns: new[] { "workspace_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "idx_permission_templates_name",
                schema: "governance",
                table: "permission_templates",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_permission_templates_workspace_id",
                schema: "governance",
                table: "permission_templates",
                column: "workspace_id",
                filter: "workspace_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_permission_rules_scope_action",
                schema: "governance",
                table: "permission_rules",
                columns: new[] { "scope_type", "action" });

            migrationBuilder.CreateIndex(
                name: "idx_permission_rules_status",
                schema: "governance",
                table: "permission_rules",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_permission_rules_workspace_id",
                schema: "governance",
                table: "permission_rules",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_payment_methods_workspace_id",
                schema: "billing",
                table: "payment_methods",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_password_reset_tokens_expires",
                schema: "identity",
                table: "password_reset_tokens",
                column: "expires_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_password_reset_tokens_user_id",
                schema: "identity",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_page_templates_category",
                schema: "docs",
                table: "page_templates",
                column: "category",
                filter: "category IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_page_templates_workspace_id",
                schema: "docs",
                table: "page_templates",
                column: "workspace_id",
                filter: "workspace_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_pages_parent_id",
                schema: "docs",
                table: "pages",
                column: "parent_id",
                filter: "parent_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_pages_workspace_id",
                schema: "docs",
                table: "pages",
                column: "workspace_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_oauth_accounts_provider",
                schema: "identity",
                table: "oauth_accounts",
                columns: new[] { "provider", "provider_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notification_preferences_user_id",
                schema: "collab",
                table: "notification_preferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_notification_preferences_user_workspace",
                schema: "collab",
                table: "notification_preferences",
                columns: new[] { "user_id", "workspace_id" });

            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_notification_id",
                schema: "collab",
                table: "notification_deliveries",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_recipient",
                schema: "collab",
                table: "notification_deliveries",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_status",
                schema: "collab",
                table: "notification_deliveries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_is_archived",
                schema: "collab",
                table: "notifications",
                column: "is_archived");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_is_read",
                schema: "collab",
                table: "notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user_id",
                schema: "collab",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_workspace_id",
                schema: "collab",
                table: "notifications",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_mirror_snapshots_connection_field",
                schema: "work",
                table: "mirror_value_snapshots",
                columns: new[] { "connection_id", "source_field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_mirror_snapshots_stale",
                schema: "work",
                table: "mirror_value_snapshots",
                column: "is_stale");

            migrationBuilder.CreateIndex(
                name: "idx_mentions_mentioned_user_id",
                schema: "collab",
                table: "mentions",
                column: "mentioned_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_mentions_source",
                schema: "collab",
                table: "mentions",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "idx_member_role_assignments_member_id",
                schema: "governance",
                table: "member_role_assignments",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "idx_member_role_assignments_role_id",
                schema: "governance",
                table: "member_role_assignments",
                column: "custom_role_id");

            migrationBuilder.CreateIndex(
                name: "idx_member_role_assignments_unique",
                schema: "governance",
                table: "member_role_assignments",
                columns: new[] { "member_id", "custom_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_labels_board_id",
                schema: "work",
                table: "labels",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "idx_item_templates_board_name",
                schema: "work",
                table: "item_templates",
                columns: new[] { "board_id", "name" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_item_dependencies_pair",
                schema: "work",
                table: "item_dependencies",
                columns: new[] { "predecessor_item_id", "successor_item_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_item_dependencies_successor",
                schema: "work",
                table: "item_dependencies",
                column: "successor_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_subscription_id",
                schema: "billing",
                table: "invoices",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_workspace_id",
                schema: "billing",
                table: "invoices",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_integration_sync_cursors_connection_resource",
                schema: "integration",
                table: "integration_sync_cursors",
                columns: new[] { "connection_id", "resource_type" });

            migrationBuilder.CreateIndex(
                name: "idx_integration_secret_versions_connection_id",
                schema: "integration",
                table: "integration_secret_versions",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "idx_integration_scopes_connection_id",
                schema: "integration",
                table: "integration_scopes",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "idx_integration_connections_workspace_id",
                schema: "integration",
                table: "integration_connections",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbound_webhook_events_workspace_id",
                schema: "integration",
                table: "inbound_webhook_events",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_formula_dependencies_depends_on",
                schema: "work",
                table: "formula_dependencies",
                column: "depends_on_field_id");

            migrationBuilder.CreateIndex(
                name: "idx_formula_dependencies_pair",
                schema: "work",
                table: "formula_dependencies",
                columns: new[] { "formula_field_id", "depends_on_field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_form_submissions_form_id",
                schema: "work",
                table: "form_submissions",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "idx_form_submissions_submitter_email",
                schema: "work",
                table: "form_submissions",
                column: "submitter_email");

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_board_id",
                schema: "work",
                table: "form_submissions",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "idx_form_questions_form_key",
                schema: "work",
                table: "form_questions",
                columns: new[] { "form_id", "question_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_form_questions_form_position",
                schema: "work",
                table: "form_questions",
                columns: new[] { "form_id", "position" });

            migrationBuilder.CreateIndex(
                name: "idx_forms_board_id",
                schema: "work",
                table: "forms",
                column: "board_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_forms_slug",
                schema: "work",
                table: "forms",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_field_permissions_board_field",
                schema: "governance",
                table: "field_permissions",
                columns: new[] { "board_id", "field_id" });

            migrationBuilder.CreateIndex(
                name: "idx_field_permissions_subject",
                schema: "governance",
                table: "field_permissions",
                columns: new[] { "subject_type", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "idx_field_options_field_id",
                schema: "work",
                table: "field_options",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "idx_entitlements_workspace_id",
                schema: "billing",
                table: "entitlements",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_email_verification_tokens_expires",
                schema: "identity",
                table: "email_verification_tokens",
                column: "expires_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_email_verification_tokens_user_id",
                schema: "identity",
                table: "email_verification_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_document_versions_page_version",
                schema: "docs",
                table: "document_versions",
                columns: new[] { "page_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_document_versions_workspace_id",
                schema: "docs",
                table: "document_versions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_dashboard_sources_dashboard_id",
                schema: "reporting",
                table: "dashboard_sources",
                column: "dashboard_id");

            migrationBuilder.CreateIndex(
                name: "idx_dashboard_sources_workspace_id",
                schema: "reporting",
                table: "dashboard_sources",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_dashboards_workspace_id",
                schema: "reporting",
                table: "dashboards",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_custom_roles_workspace_id",
                schema: "governance",
                table: "custom_roles",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_comments_resource",
                schema: "collab",
                table: "comments",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "IX_comments_parent_id",
                schema: "collab",
                table: "comments",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_checklist_items_checklist_position",
                schema: "work",
                table: "checklist_items",
                columns: new[] { "checklist_id", "position" });

            migrationBuilder.CreateIndex(
                name: "idx_checklists_item_id",
                schema: "work",
                table: "checklists",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_calendar_integrations_workspace_id",
                schema: "integration",
                table: "calendar_integrations",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_calendar_event_links_integration_id",
                schema: "integration",
                table: "calendar_event_links",
                column: "integration_id");

            migrationBuilder.CreateIndex(
                name: "idx_calendar_events_external",
                schema: "integration",
                table: "calendar_events",
                columns: new[] { "integration_id", "external_event_id" },
                unique: true,
                filter: "external_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_calendar_events_resource",
                schema: "integration",
                table: "calendar_events",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "idx_board_view_user_prefs_view_user",
                schema: "work",
                table: "board_view_user_preferences",
                columns: new[] { "view_id", "user_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_view_pins_board_user_scope",
                schema: "work",
                table: "board_view_pins",
                columns: new[] { "board_id", "user_id", "pin_scope" });

            migrationBuilder.CreateIndex(
                name: "idx_board_view_pins_view_id",
                schema: "work",
                table: "board_view_pins",
                column: "board_view_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_views_board_id",
                schema: "work",
                table: "board_views",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_subscribers_board_user",
                schema: "work",
                table: "board_subscribers",
                columns: new[] { "board_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_board_relations_source_board",
                schema: "work",
                table: "board_relations",
                column: "source_board_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_relations_target_board",
                schema: "work",
                table: "board_relations",
                column: "target_board_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_members_board_user",
                schema: "work",
                table: "board_members",
                columns: new[] { "board_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_board_members_user_id",
                schema: "work",
                table: "board_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_values_item_field",
                schema: "work",
                table: "board_item_values",
                columns: new[] { "item_id", "field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_board_item_values_field_id",
                schema: "work",
                table: "board_item_values",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_members_item_user",
                schema: "work",
                table: "board_item_members",
                columns: new[] { "item_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_board_item_members_user_id",
                schema: "work",
                table: "board_item_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_links_source_item",
                schema: "work",
                table: "board_item_links",
                column: "source_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_labels_item_label",
                schema: "work",
                table: "board_item_labels",
                columns: new[] { "item_id", "label_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_board_item_labels_label_id",
                schema: "work",
                table: "board_item_labels",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_connections_relation_id",
                schema: "work",
                table: "board_item_connections",
                column: "relation_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_connections_source_item",
                schema: "work",
                table: "board_item_connections",
                column: "source_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_connections_target_item",
                schema: "work",
                table: "board_item_connections",
                column: "target_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_items_board_id",
                schema: "work",
                table: "board_items",
                column: "board_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_items_group_position",
                schema: "work",
                table: "board_items",
                columns: new[] { "group_id", "position" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_groups_board_position",
                schema: "work",
                table: "board_groups",
                columns: new[] { "board_id", "position" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_fields_board_position",
                schema: "work",
                table: "board_fields",
                columns: new[] { "board_id", "position" });

            migrationBuilder.CreateIndex(
                name: "idx_boards_title",
                schema: "work",
                table: "boards",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "idx_boards_workspace_id",
                schema: "work",
                table: "boards",
                column: "workspace_id",
                filter: "deleted_at IS NULL AND is_archived = false");

            migrationBuilder.CreateIndex(
                name: "idx_blocks_page_position",
                schema: "docs",
                table: "blocks",
                columns: new[] { "page_id", "position" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_blocks_parent_id",
                schema: "docs",
                table: "blocks",
                column: "parent_id",
                filter: "parent_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_billing_events_provider_event_id",
                schema: "billing",
                table: "billing_events",
                column: "provider_event_id");

            migrationBuilder.CreateIndex(
                name: "idx_billing_events_type",
                schema: "billing",
                table: "billing_events",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "idx_automation_templates_category",
                schema: "automation",
                table: "automation_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "idx_automation_templates_status",
                schema: "automation",
                table: "automation_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_automation_rules_trigger_event",
                schema: "automation",
                table: "automation_rules",
                column: "trigger_event");

            migrationBuilder.CreateIndex(
                name: "idx_automation_rules_workspace_id",
                schema: "automation",
                table: "automation_rules",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_automation_execution_steps_execution_id",
                schema: "automation",
                table: "automation_execution_steps",
                column: "execution_id");

            migrationBuilder.CreateIndex(
                name: "idx_automation_executions_rule_id",
                schema: "automation",
                table: "automation_executions",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "idx_automation_executions_status",
                schema: "automation",
                table: "automation_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_audit_retention_policies_workspace_id",
                schema: "governance",
                table: "audit_retention_policies",
                column: "workspace_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_resource",
                schema: "governance",
                table: "audit_logs",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_timestamp",
                schema: "governance",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_workspace_id",
                schema: "governance",
                table: "audit_logs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_attachments_resource",
                schema: "collab",
                table: "attachments",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "idx_api_tokens_user_id",
                schema: "identity",
                table: "api_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_api_tokens_workspace_id",
                schema: "identity",
                table: "api_tokens",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_agent_runs_agent_id",
                schema: "automation",
                table: "ai_agent_runs",
                column: "ai_agent_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_agent_runs_status",
                schema: "automation",
                table: "ai_agent_runs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_ai_agent_runs_workspace_id",
                schema: "automation",
                table: "ai_agent_runs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_agents_status",
                schema: "automation",
                table: "ai_agents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_ai_agents_workspace_id",
                schema: "automation",
                table: "ai_agents",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_activity_logs_resource",
                schema: "collab",
                table: "activity_logs",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "idx_activity_logs_timestamp",
                schema: "collab",
                table: "activity_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_activity_logs_workspace_id",
                schema: "collab",
                table: "activity_logs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_board_view_filter_rules_preference_id",
                schema: "work",
                table: "board_view_filter_rules",
                column: "preference_id");

            migrationBuilder.CreateIndex(
                name: "IX_board_view_sort_rules_preference_id",
                schema: "work",
                table: "board_view_sort_rules",
                column: "preference_id");

            migrationBuilder.CreateIndex(
                name: "IX_saved_filter_sort_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_sort_rules",
                column: "saved_filter_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "FK_board_view_pins_board_views_board_view_id",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropForeignKey(
                name: "FK_board_view_pins_boards_board_id",
                schema: "work",
                table: "board_view_pins");

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

            migrationBuilder.DropTable(
                name: "board_view_filter_rules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_view_sort_rules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "saved_filter_sort_rules",
                schema: "work");

            migrationBuilder.DropIndex(
                name: "idx_user_security_settings_user_id",
                schema: "identity",
                table: "user_security_settings");

            migrationBuilder.DropIndex(
                name: "idx_usage_metric_history_metric_id",
                schema: "billing",
                table: "usage_metric_history");

            migrationBuilder.DropPrimaryKey(
                name: "PK_saved_filter_rules",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropIndex(
                name: "IX_saved_filter_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropIndex(
                name: "idx_feature_usage_ledger_workspace_id",
                schema: "billing",
                table: "feature_usage_ledger");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspaces",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropIndex(
                name: "idx_workspaces_name",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropIndex(
                name: "idx_workspaces_slug",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_policies",
                schema: "governance",
                table: "workspace_policies");

            migrationBuilder.DropIndex(
                name: "idx_workspace_policies_workspace_id",
                schema: "governance",
                table: "workspace_policies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_members",
                schema: "workspace",
                table: "workspace_members");

            migrationBuilder.DropIndex(
                name: "idx_workspace_members_user_id",
                schema: "workspace",
                table: "workspace_members");

            migrationBuilder.DropIndex(
                name: "idx_workspace_members_workspace_user",
                schema: "workspace",
                table: "workspace_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_invitations",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropIndex(
                name: "idx_workspace_invitations_email",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropIndex(
                name: "idx_workspace_invitations_workspace_id",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_feature_usages",
                schema: "billing",
                table: "workspace_feature_usages");

            migrationBuilder.DropIndex(
                name: "idx_workspace_feature_usages_workspace_id",
                schema: "billing",
                table: "workspace_feature_usages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workload_allocations",
                schema: "work",
                table: "workload_allocations");

            migrationBuilder.DropIndex(
                name: "idx_workload_allocations_item_id",
                schema: "work",
                table: "workload_allocations");

            migrationBuilder.DropIndex(
                name: "idx_workload_allocations_user_date",
                schema: "work",
                table: "workload_allocations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_webhook_subscriptions",
                schema: "integration",
                table: "webhook_subscriptions");

            migrationBuilder.DropIndex(
                name: "idx_webhook_subscriptions_workspace_id",
                schema: "integration",
                table: "webhook_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_webhook_deliveries",
                schema: "integration",
                table: "webhook_deliveries");

            migrationBuilder.DropIndex(
                name: "idx_webhook_deliveries_subscription_id",
                schema: "integration",
                table: "webhook_deliveries");

            migrationBuilder.DropIndex(
                name: "idx_webhook_deliveries_workspace_id",
                schema: "integration",
                table: "webhook_deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_users_email",
                schema: "identity",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_sessions",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropIndex(
                name: "idx_user_sessions_expires",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropIndex(
                name: "idx_user_sessions_user_id",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_profiles",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_mfa_methods",
                schema: "identity",
                table: "user_mfa_methods");

            migrationBuilder.DropIndex(
                name: "idx_user_mfa_methods_user_id",
                schema: "identity",
                table: "user_mfa_methods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_login_attempts",
                schema: "identity",
                table: "user_login_attempts");

            migrationBuilder.DropIndex(
                name: "idx_user_login_attempts_occurred_at",
                schema: "identity",
                table: "user_login_attempts");

            migrationBuilder.DropIndex(
                name: "idx_user_login_attempts_user_id",
                schema: "identity",
                table: "user_login_attempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usage_metrics",
                schema: "billing",
                table: "usage_metrics");

            migrationBuilder.DropIndex(
                name: "idx_usage_metrics_workspace_id",
                schema: "billing",
                table: "usage_metrics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_time_tracking_entries",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropIndex(
                name: "idx_time_tracking_item_user",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropIndex(
                name: "idx_time_tracking_status",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropIndex(
                name: "IX_time_tracking_entries_board_id",
                schema: "work",
                table: "time_tracking_entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_teams",
                schema: "workspace",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "idx_teams_workspace_id",
                schema: "workspace",
                table: "teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_team_members",
                schema: "workspace",
                table: "team_members");

            migrationBuilder.DropIndex(
                name: "idx_team_members_team_user",
                schema: "workspace",
                table: "team_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subscriptions",
                schema: "billing",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "idx_subscriptions_plan_id",
                schema: "billing",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "idx_subscriptions_workspace_id",
                schema: "billing",
                table: "subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sso_providers",
                schema: "identity",
                table: "sso_providers");

            migrationBuilder.DropIndex(
                name: "idx_sso_providers_workspace_id",
                schema: "identity",
                table: "sso_providers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_spaces",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropIndex(
                name: "idx_spaces_workspace_id",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_share_links",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropIndex(
                name: "idx_share_links_resource_id",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_security_events",
                schema: "governance",
                table: "security_events");

            migrationBuilder.DropIndex(
                name: "idx_security_events_occurred_at",
                schema: "governance",
                table: "security_events");

            migrationBuilder.DropIndex(
                name: "idx_security_events_workspace_id",
                schema: "governance",
                table: "security_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scim_directory_syncs",
                schema: "identity",
                table: "scim_directory_syncs");

            migrationBuilder.DropIndex(
                name: "idx_scim_directory_syncs_workspace_id",
                schema: "identity",
                table: "scim_directory_syncs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scheduled_jobs",
                schema: "automation",
                table: "scheduled_jobs");

            migrationBuilder.DropIndex(
                name: "idx_scheduled_jobs_rule_id",
                schema: "automation",
                table: "scheduled_jobs");

            migrationBuilder.DropIndex(
                name: "idx_scheduled_jobs_status",
                schema: "automation",
                table: "scheduled_jobs");

            migrationBuilder.DropIndex(
                name: "idx_scheduled_jobs_workspace_id",
                schema: "automation",
                table: "scheduled_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_saved_filters",
                schema: "work",
                table: "saved_filters");

            migrationBuilder.DropIndex(
                name: "idx_saved_filters_board_name",
                schema: "work",
                table: "saved_filters");

            migrationBuilder.DropIndex(
                name: "IX_saved_filters_view_id",
                schema: "work",
                table: "saved_filters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rollup_snapshots",
                schema: "work",
                table: "rollup_snapshots");

            migrationBuilder.DropIndex(
                name: "idx_rollup_snapshots_item_field",
                schema: "work",
                table: "rollup_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_watchers",
                schema: "collab",
                table: "resource_watchers");

            migrationBuilder.DropIndex(
                name: "idx_resource_watchers_target",
                schema: "collab",
                table: "resource_watchers");

            migrationBuilder.DropIndex(
                name: "idx_resource_watchers_user_workspace",
                schema: "collab",
                table: "resource_watchers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_permissions",
                schema: "governance",
                table: "resource_permissions");

            migrationBuilder.DropIndex(
                name: "idx_resource_permissions_resource",
                schema: "governance",
                table: "resource_permissions");

            migrationBuilder.DropIndex(
                name: "idx_resource_permissions_subject_id",
                schema: "governance",
                table: "resource_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_links",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropIndex(
                name: "idx_resource_links_source",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropIndex(
                name: "idx_resource_links_target",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropIndex(
                name: "idx_resource_links_workspace_id",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reporting_snapshots",
                schema: "reporting",
                table: "reporting_snapshots");

            migrationBuilder.DropIndex(
                name: "idx_reporting_snapshots_workspace_id",
                schema: "reporting",
                table: "reporting_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_relation_field_configs",
                schema: "work",
                table: "relation_field_configs");

            migrationBuilder.DropIndex(
                name: "idx_relation_field_configs_field_id",
                schema: "work",
                table: "relation_field_configs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reactions",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropIndex(
                name: "idx_reactions_resource",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropIndex(
                name: "idx_reactions_target_resource_id",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropIndex(
                name: "idx_reactions_user_id",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_presence_sessions",
                schema: "collab",
                table: "presence_sessions");

            migrationBuilder.DropIndex(
                name: "idx_presence_sessions_last_seen_at",
                schema: "collab",
                table: "presence_sessions");

            migrationBuilder.DropIndex(
                name: "idx_presence_sessions_workspace_user",
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

            migrationBuilder.DropIndex(
                name: "idx_permission_templates_name",
                schema: "governance",
                table: "permission_templates");

            migrationBuilder.DropIndex(
                name: "idx_permission_templates_workspace_id",
                schema: "governance",
                table: "permission_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permission_rules",
                schema: "governance",
                table: "permission_rules");

            migrationBuilder.DropIndex(
                name: "idx_permission_rules_scope_action",
                schema: "governance",
                table: "permission_rules");

            migrationBuilder.DropIndex(
                name: "idx_permission_rules_status",
                schema: "governance",
                table: "permission_rules");

            migrationBuilder.DropIndex(
                name: "idx_permission_rules_workspace_id",
                schema: "governance",
                table: "permission_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payment_methods",
                schema: "billing",
                table: "payment_methods");

            migrationBuilder.DropIndex(
                name: "idx_payment_methods_workspace_id",
                schema: "billing",
                table: "payment_methods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_password_reset_tokens",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "idx_password_reset_tokens_expires",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "idx_password_reset_tokens_user_id",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_pages",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropIndex(
                name: "idx_pages_parent_id",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropIndex(
                name: "idx_pages_workspace_id",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_page_templates",
                schema: "docs",
                table: "page_templates");

            migrationBuilder.DropIndex(
                name: "idx_page_templates_category",
                schema: "docs",
                table: "page_templates");

            migrationBuilder.DropIndex(
                name: "idx_page_templates_workspace_id",
                schema: "docs",
                table: "page_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_oauth_accounts",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropIndex(
                name: "idx_oauth_accounts_provider",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notifications",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "idx_notifications_is_archived",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "idx_notifications_is_read",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "idx_notifications_user_id",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "idx_notifications_workspace_id",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notification_preferences",
                schema: "collab",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "idx_notification_preferences_user_id",
                schema: "collab",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "idx_notification_preferences_user_workspace",
                schema: "collab",
                table: "notification_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notification_deliveries",
                schema: "collab",
                table: "notification_deliveries");

            migrationBuilder.DropIndex(
                name: "idx_notification_deliveries_notification_id",
                schema: "collab",
                table: "notification_deliveries");

            migrationBuilder.DropIndex(
                name: "idx_notification_deliveries_recipient",
                schema: "collab",
                table: "notification_deliveries");

            migrationBuilder.DropIndex(
                name: "idx_notification_deliveries_status",
                schema: "collab",
                table: "notification_deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_mirror_value_snapshots",
                schema: "work",
                table: "mirror_value_snapshots");

            migrationBuilder.DropIndex(
                name: "idx_mirror_snapshots_connection_field",
                schema: "work",
                table: "mirror_value_snapshots");

            migrationBuilder.DropIndex(
                name: "idx_mirror_snapshots_stale",
                schema: "work",
                table: "mirror_value_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_mentions",
                schema: "collab",
                table: "mentions");

            migrationBuilder.DropIndex(
                name: "idx_mentions_mentioned_user_id",
                schema: "collab",
                table: "mentions");

            migrationBuilder.DropIndex(
                name: "idx_mentions_source",
                schema: "collab",
                table: "mentions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_member_role_assignments",
                schema: "governance",
                table: "member_role_assignments");

            migrationBuilder.DropIndex(
                name: "idx_member_role_assignments_member_id",
                schema: "governance",
                table: "member_role_assignments");

            migrationBuilder.DropIndex(
                name: "idx_member_role_assignments_role_id",
                schema: "governance",
                table: "member_role_assignments");

            migrationBuilder.DropIndex(
                name: "idx_member_role_assignments_unique",
                schema: "governance",
                table: "member_role_assignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_labels",
                schema: "work",
                table: "labels");

            migrationBuilder.DropIndex(
                name: "idx_labels_board_id",
                schema: "work",
                table: "labels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_item_templates",
                schema: "work",
                table: "item_templates");

            migrationBuilder.DropIndex(
                name: "idx_item_templates_board_name",
                schema: "work",
                table: "item_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_item_dependencies",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropIndex(
                name: "idx_item_dependencies_pair",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropIndex(
                name: "idx_item_dependencies_successor",
                schema: "work",
                table: "item_dependencies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invoices",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "idx_invoices_subscription_id",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "idx_invoices_workspace_id",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_sync_cursors",
                schema: "integration",
                table: "integration_sync_cursors");

            migrationBuilder.DropIndex(
                name: "idx_integration_sync_cursors_connection_resource",
                schema: "integration",
                table: "integration_sync_cursors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_secret_versions",
                schema: "integration",
                table: "integration_secret_versions");

            migrationBuilder.DropIndex(
                name: "idx_integration_secret_versions_connection_id",
                schema: "integration",
                table: "integration_secret_versions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_scopes",
                schema: "integration",
                table: "integration_scopes");

            migrationBuilder.DropIndex(
                name: "idx_integration_scopes_connection_id",
                schema: "integration",
                table: "integration_scopes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_integration_connections",
                schema: "integration",
                table: "integration_connections");

            migrationBuilder.DropIndex(
                name: "idx_integration_connections_workspace_id",
                schema: "integration",
                table: "integration_connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbound_webhook_events",
                schema: "integration",
                table: "inbound_webhook_events");

            migrationBuilder.DropIndex(
                name: "idx_inbound_webhook_events_workspace_id",
                schema: "integration",
                table: "inbound_webhook_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_formula_dependencies",
                schema: "work",
                table: "formula_dependencies");

            migrationBuilder.DropIndex(
                name: "idx_formula_dependencies_depends_on",
                schema: "work",
                table: "formula_dependencies");

            migrationBuilder.DropIndex(
                name: "idx_formula_dependencies_pair",
                schema: "work",
                table: "formula_dependencies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_forms",
                schema: "work",
                table: "forms");

            migrationBuilder.DropIndex(
                name: "idx_forms_board_id",
                schema: "work",
                table: "forms");

            migrationBuilder.DropIndex(
                name: "idx_forms_slug",
                schema: "work",
                table: "forms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_form_submissions",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropIndex(
                name: "idx_form_submissions_form_id",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropIndex(
                name: "idx_form_submissions_submitter_email",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropIndex(
                name: "IX_form_submissions_board_id",
                schema: "work",
                table: "form_submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_form_questions",
                schema: "work",
                table: "form_questions");

            migrationBuilder.DropIndex(
                name: "idx_form_questions_form_key",
                schema: "work",
                table: "form_questions");

            migrationBuilder.DropIndex(
                name: "idx_form_questions_form_position",
                schema: "work",
                table: "form_questions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_field_permissions",
                schema: "governance",
                table: "field_permissions");

            migrationBuilder.DropIndex(
                name: "idx_field_permissions_board_field",
                schema: "governance",
                table: "field_permissions");

            migrationBuilder.DropIndex(
                name: "idx_field_permissions_subject",
                schema: "governance",
                table: "field_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_field_options",
                schema: "work",
                table: "field_options");

            migrationBuilder.DropIndex(
                name: "idx_field_options_field_id",
                schema: "work",
                table: "field_options");

            migrationBuilder.DropPrimaryKey(
                name: "PK_entitlements",
                schema: "billing",
                table: "entitlements");

            migrationBuilder.DropIndex(
                name: "idx_entitlements_workspace_id",
                schema: "billing",
                table: "entitlements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_email_verification_tokens",
                schema: "identity",
                table: "email_verification_tokens");

            migrationBuilder.DropIndex(
                name: "idx_email_verification_tokens_expires",
                schema: "identity",
                table: "email_verification_tokens");

            migrationBuilder.DropIndex(
                name: "idx_email_verification_tokens_user_id",
                schema: "identity",
                table: "email_verification_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_document_versions",
                schema: "docs",
                table: "document_versions");

            migrationBuilder.DropIndex(
                name: "idx_document_versions_page_version",
                schema: "docs",
                table: "document_versions");

            migrationBuilder.DropIndex(
                name: "idx_document_versions_workspace_id",
                schema: "docs",
                table: "document_versions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dashboards",
                schema: "reporting",
                table: "dashboards");

            migrationBuilder.DropIndex(
                name: "idx_dashboards_workspace_id",
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

            migrationBuilder.DropIndex(
                name: "idx_dashboard_sources_dashboard_id",
                schema: "reporting",
                table: "dashboard_sources");

            migrationBuilder.DropIndex(
                name: "idx_dashboard_sources_workspace_id",
                schema: "reporting",
                table: "dashboard_sources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_custom_roles",
                schema: "governance",
                table: "custom_roles");

            migrationBuilder.DropIndex(
                name: "idx_custom_roles_workspace_id",
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

            migrationBuilder.DropIndex(
                name: "idx_comments_resource",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_parent_id",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_checklists",
                schema: "work",
                table: "checklists");

            migrationBuilder.DropIndex(
                name: "idx_checklists_item_id",
                schema: "work",
                table: "checklists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_checklist_items",
                schema: "work",
                table: "checklist_items");

            migrationBuilder.DropIndex(
                name: "idx_checklist_items_checklist_position",
                schema: "work",
                table: "checklist_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_integrations",
                schema: "integration",
                table: "calendar_integrations");

            migrationBuilder.DropIndex(
                name: "idx_calendar_integrations_workspace_id",
                schema: "integration",
                table: "calendar_integrations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_events",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropIndex(
                name: "idx_calendar_events_external",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropIndex(
                name: "idx_calendar_events_resource",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_calendar_event_links",
                schema: "integration",
                table: "calendar_event_links");

            migrationBuilder.DropIndex(
                name: "idx_calendar_event_links_integration_id",
                schema: "integration",
                table: "calendar_event_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_boards",
                schema: "work",
                table: "boards");

            migrationBuilder.DropIndex(
                name: "idx_boards_title",
                schema: "work",
                table: "boards");

            migrationBuilder.DropIndex(
                name: "idx_boards_workspace_id",
                schema: "work",
                table: "boards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_views",
                schema: "work",
                table: "board_views");

            migrationBuilder.DropIndex(
                name: "idx_board_views_board_id",
                schema: "work",
                table: "board_views");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_view_user_preferences",
                schema: "work",
                table: "board_view_user_preferences");

            migrationBuilder.DropIndex(
                name: "idx_board_view_user_prefs_view_user",
                schema: "work",
                table: "board_view_user_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_view_pins",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropIndex(
                name: "idx_board_view_pins_board_user_scope",
                schema: "work",
                table: "board_view_pins");

            migrationBuilder.DropIndex(
                name: "idx_board_view_pins_view_id",
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

            migrationBuilder.DropIndex(
                name: "idx_board_subscribers_board_user",
                schema: "work",
                table: "board_subscribers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_relations",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropIndex(
                name: "idx_board_relations_source_board",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropIndex(
                name: "idx_board_relations_target_board",
                schema: "work",
                table: "board_relations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_members",
                schema: "work",
                table: "board_members");

            migrationBuilder.DropIndex(
                name: "idx_board_members_board_user",
                schema: "work",
                table: "board_members");

            migrationBuilder.DropIndex(
                name: "idx_board_members_user_id",
                schema: "work",
                table: "board_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_items",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropIndex(
                name: "idx_board_items_board_id",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropIndex(
                name: "idx_board_items_group_position",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_values",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropIndex(
                name: "idx_board_item_values_item_field",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropIndex(
                name: "IX_board_item_values_field_id",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_members",
                schema: "work",
                table: "board_item_members");

            migrationBuilder.DropIndex(
                name: "idx_board_item_members_item_user",
                schema: "work",
                table: "board_item_members");

            migrationBuilder.DropIndex(
                name: "idx_board_item_members_user_id",
                schema: "work",
                table: "board_item_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_links",
                schema: "work",
                table: "board_item_links");

            migrationBuilder.DropIndex(
                name: "idx_board_item_links_source_item",
                schema: "work",
                table: "board_item_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_labels",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropIndex(
                name: "idx_board_item_labels_item_label",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropIndex(
                name: "IX_board_item_labels_label_id",
                schema: "work",
                table: "board_item_labels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_connections",
                schema: "work",
                table: "board_item_connections");

            migrationBuilder.DropIndex(
                name: "idx_board_item_connections_relation_id",
                schema: "work",
                table: "board_item_connections");

            migrationBuilder.DropIndex(
                name: "idx_board_item_connections_source_item",
                schema: "work",
                table: "board_item_connections");

            migrationBuilder.DropIndex(
                name: "idx_board_item_connections_target_item",
                schema: "work",
                table: "board_item_connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_groups",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropIndex(
                name: "idx_board_groups_board_position",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_fields",
                schema: "work",
                table: "board_fields");

            migrationBuilder.DropIndex(
                name: "idx_board_fields_board_position",
                schema: "work",
                table: "board_fields");

            migrationBuilder.DropPrimaryKey(
                name: "PK_blocks",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropIndex(
                name: "idx_blocks_page_position",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropIndex(
                name: "idx_blocks_parent_id",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_billing_events",
                schema: "billing",
                table: "billing_events");

            migrationBuilder.DropIndex(
                name: "idx_billing_events_provider_event_id",
                schema: "billing",
                table: "billing_events");

            migrationBuilder.DropIndex(
                name: "idx_billing_events_type",
                schema: "billing",
                table: "billing_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_templates",
                schema: "automation",
                table: "automation_templates");

            migrationBuilder.DropIndex(
                name: "idx_automation_templates_category",
                schema: "automation",
                table: "automation_templates");

            migrationBuilder.DropIndex(
                name: "idx_automation_templates_status",
                schema: "automation",
                table: "automation_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_rules",
                schema: "automation",
                table: "automation_rules");

            migrationBuilder.DropIndex(
                name: "idx_automation_rules_trigger_event",
                schema: "automation",
                table: "automation_rules");

            migrationBuilder.DropIndex(
                name: "idx_automation_rules_workspace_id",
                schema: "automation",
                table: "automation_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_executions",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropIndex(
                name: "idx_automation_executions_rule_id",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropIndex(
                name: "idx_automation_executions_status",
                schema: "automation",
                table: "automation_executions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_automation_execution_steps",
                schema: "automation",
                table: "automation_execution_steps");

            migrationBuilder.DropIndex(
                name: "idx_automation_execution_steps_execution_id",
                schema: "automation",
                table: "automation_execution_steps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_retention_policies",
                schema: "governance",
                table: "audit_retention_policies");

            migrationBuilder.DropIndex(
                name: "idx_audit_retention_policies_workspace_id",
                schema: "governance",
                table: "audit_retention_policies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "idx_audit_logs_resource",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "idx_audit_logs_timestamp",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "idx_audit_logs_workspace_id",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_attachments",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "idx_attachments_resource",
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

            migrationBuilder.DropIndex(
                name: "idx_api_tokens_user_id",
                schema: "identity",
                table: "api_tokens");

            migrationBuilder.DropIndex(
                name: "idx_api_tokens_workspace_id",
                schema: "identity",
                table: "api_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_agents",
                schema: "automation",
                table: "ai_agents");

            migrationBuilder.DropIndex(
                name: "idx_ai_agents_status",
                schema: "automation",
                table: "ai_agents");

            migrationBuilder.DropIndex(
                name: "idx_ai_agents_workspace_id",
                schema: "automation",
                table: "ai_agents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_agent_runs",
                schema: "automation",
                table: "ai_agent_runs");

            migrationBuilder.DropIndex(
                name: "idx_ai_agent_runs_agent_id",
                schema: "automation",
                table: "ai_agent_runs");

            migrationBuilder.DropIndex(
                name: "idx_ai_agent_runs_status",
                schema: "automation",
                table: "ai_agent_runs");

            migrationBuilder.DropIndex(
                name: "idx_ai_agent_runs_workspace_id",
                schema: "automation",
                table: "ai_agent_runs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_activity_logs",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.DropIndex(
                name: "idx_activity_logs_resource",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.DropIndex(
                name: "idx_activity_logs_timestamp",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.DropIndex(
                name: "idx_activity_logs_workspace_id",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "field_id",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropColumn(
                name: "operator",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropColumn(
                name: "value",
                schema: "work",
                table: "saved_filter_rules");

            migrationBuilder.DropColumn(
                name: "settings_allow_public_sharing",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "settings_enforce_mfa",
                schema: "workspace",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "guest_allow_invites",
                schema: "governance",
                table: "workspace_policies");

            migrationBuilder.DropColumn(
                name: "resource_allow_public_sharing",
                schema: "governance",
                table: "workspace_policies");

            migrationBuilder.DropColumn(
                name: "sharing_allow_external_invite",
                schema: "governance",
                table: "workspace_policies");

            migrationBuilder.DropColumn(
                name: "sharing_allow_public",
                schema: "governance",
                table: "workspace_policies");

            migrationBuilder.DropColumn(
                name: "token",
                schema: "workspace",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "feature_code",
                schema: "billing",
                table: "workspace_feature_usages");

            migrationBuilder.DropColumn(
                name: "secret_hash",
                schema: "integration",
                table: "webhook_subscriptions");

            migrationBuilder.DropColumn(
                name: "url",
                schema: "integration",
                table: "webhook_subscriptions");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                schema: "integration",
                table: "webhook_deliveries");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "integration",
                table: "webhook_deliveries");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                schema: "integration",
                table: "webhook_deliveries");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "refresh_token_hash",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "period_end",
                schema: "billing",
                table: "usage_metrics");

            migrationBuilder.DropColumn(
                name: "period_start",
                schema: "billing",
                table: "usage_metrics");

            migrationBuilder.DropColumn(
                name: "certificate_ref",
                schema: "identity",
                table: "sso_providers");

            migrationBuilder.DropColumn(
                name: "entity_id",
                schema: "identity",
                table: "sso_providers");

            migrationBuilder.DropColumn(
                name: "redirect_uri",
                schema: "identity",
                table: "sso_providers");

            migrationBuilder.DropColumn(
                name: "sso_url",
                schema: "identity",
                table: "sso_providers");

            migrationBuilder.DropColumn(
                name: "delete_reason",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "restored_at",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "restored_by",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "token_hash",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "governance",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "metadata",
                schema: "governance",
                table: "security_events");

            migrationBuilder.DropColumn(
                name: "cron_expression",
                schema: "automation",
                table: "scheduled_jobs");

            migrationBuilder.DropColumn(
                name: "timezone",
                schema: "automation",
                table: "scheduled_jobs");

            migrationBuilder.DropColumn(
                name: "target_id",
                schema: "collab",
                table: "resource_watchers");

            migrationBuilder.DropColumn(
                name: "target_type",
                schema: "collab",
                table: "resource_watchers");

            migrationBuilder.DropColumn(
                name: "source_id",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropColumn(
                name: "source_type",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropColumn(
                name: "target_id",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropColumn(
                name: "target_type",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "docs",
                table: "resource_links");

            migrationBuilder.DropColumn(
                name: "emoji",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropColumn(
                name: "resource_id",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropColumn(
                name: "resource_type",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropColumn(
                name: "target_workspace_id",
                schema: "collab",
                table: "reactions");

            migrationBuilder.DropColumn(
                name: "price_amount",
                schema: "billing",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "price_currency",
                schema: "billing",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "feature_code",
                schema: "billing",
                table: "plan_limits");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropColumn(
                name: "access_token_ref",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropColumn(
                name: "refresh_token_ref",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropColumn(
                name: "token_expires_at",
                schema: "identity",
                table: "oauth_accounts");

            migrationBuilder.DropColumn(
                name: "target_id",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "target_type",
                schema: "collab",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "source_id",
                schema: "collab",
                table: "mentions");

            migrationBuilder.DropColumn(
                name: "source_type",
                schema: "collab",
                table: "mentions");

            migrationBuilder.DropColumn(
                name: "source_workspace_id",
                schema: "collab",
                table: "mentions");

            migrationBuilder.DropColumn(
                name: "color",
                schema: "work",
                table: "labels");

            migrationBuilder.DropColumn(
                name: "amount_currency",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "amount_value",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "Config",
                schema: "work",
                table: "form_questions");

            migrationBuilder.DropColumn(
                name: "color",
                schema: "work",
                table: "field_options");

            migrationBuilder.DropColumn(
                name: "feature_code",
                schema: "billing",
                table: "entitlements");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "reporting",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "pos_h",
                schema: "reporting",
                table: "dashboard_widgets");

            migrationBuilder.DropColumn(
                name: "pos_w",
                schema: "reporting",
                table: "dashboard_widgets");

            migrationBuilder.DropColumn(
                name: "pos_x",
                schema: "reporting",
                table: "dashboard_widgets");

            migrationBuilder.DropColumn(
                name: "pos_y",
                schema: "reporting",
                table: "dashboard_widgets");

            migrationBuilder.DropColumn(
                name: "permissions",
                schema: "governance",
                table: "custom_roles");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "governance",
                table: "custom_roles");

            migrationBuilder.DropColumn(
                name: "anchor_offset",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "anchor_selector",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "resource_id",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "resource_type",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "target_workspace_id",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "collab",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "resource_id",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "resource_type",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "target_workspace_id",
                schema: "integration",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "config",
                schema: "work",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "value",
                schema: "work",
                table: "board_item_values");

            migrationBuilder.DropColumn(
                name: "target_id",
                schema: "work",
                table: "board_item_links");

            migrationBuilder.DropColumn(
                name: "target_type",
                schema: "work",
                table: "board_item_links");

            migrationBuilder.DropColumn(
                name: "color",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropColumn(
                name: "settings",
                schema: "work",
                table: "board_fields");

            migrationBuilder.DropColumn(
                name: "content",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropColumn(
                name: "properties",
                schema: "docs",
                table: "blocks");

            migrationBuilder.DropColumn(
                name: "metadata_ip_address",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "metadata_trace_id",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "metadata_user_agent",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "target_workspace_id",
                schema: "governance",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "file_name",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "file_size",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "mime_type",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "resource_id",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "resource_type",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "storage_key",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "target_workspace_id",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "url",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "target_id",
                schema: "work",
                table: "approval_requests");

            migrationBuilder.DropColumn(
                name: "target_type",
                schema: "work",
                table: "approval_requests");

            migrationBuilder.DropColumn(
                name: "scopes",
                schema: "identity",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "metadata",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "resource_id",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "resource_type",
                schema: "collab",
                table: "activity_logs");

            migrationBuilder.RenameTable(
                name: "saved_filter_rules",
                schema: "work",
                newName: "saved_filter_Rules",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "workspaces",
                schema: "workspace",
                newName: "workspace",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "workspace_policies",
                schema: "governance",
                newName: "workspace_policy",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "workspace_members",
                schema: "workspace",
                newName: "workspace_member",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "workspace_invitations",
                schema: "workspace",
                newName: "workspace_invitation",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "workspace_feature_usages",
                schema: "billing",
                newName: "workspace_feature_usage",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "workload_allocations",
                schema: "work",
                newName: "workload_allocation",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "webhook_subscriptions",
                schema: "integration",
                newName: "webhook_subscription",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "webhook_deliveries",
                schema: "integration",
                newName: "webhook_delivery",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "users",
                schema: "identity",
                newName: "user",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_sessions",
                schema: "identity",
                newName: "user_session",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_profiles",
                schema: "identity",
                newName: "user_profile",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_mfa_methods",
                schema: "identity",
                newName: "user_mfa_method",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_login_attempts",
                schema: "identity",
                newName: "user_login_attempt",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "usage_metrics",
                schema: "billing",
                newName: "usage_metric",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "time_tracking_entries",
                schema: "work",
                newName: "time_tracking_entry",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "teams",
                schema: "workspace",
                newName: "team",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "team_members",
                schema: "workspace",
                newName: "team_member",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "subscriptions",
                schema: "billing",
                newName: "subscription",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "sso_providers",
                schema: "identity",
                newName: "sso_provider",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "spaces",
                schema: "workspace",
                newName: "space",
                newSchema: "workspace");

            migrationBuilder.RenameTable(
                name: "share_links",
                schema: "governance",
                newName: "share_link",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "security_events",
                schema: "governance",
                newName: "security_event",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "scim_directory_syncs",
                schema: "identity",
                newName: "scim_directory_sync",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "scheduled_jobs",
                schema: "automation",
                newName: "scheduled_job",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "saved_filters",
                schema: "work",
                newName: "saved_filter",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "rollup_snapshots",
                schema: "work",
                newName: "rollup_snapshot",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "resource_watchers",
                schema: "collab",
                newName: "resource_watcher",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "resource_permissions",
                schema: "governance",
                newName: "resource_permission",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "resource_links",
                schema: "docs",
                newName: "resource_link",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "reporting_snapshots",
                schema: "reporting",
                newName: "reporting_snapshot",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "relation_field_configs",
                schema: "work",
                newName: "relation_field_config",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "reactions",
                schema: "collab",
                newName: "reaction",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "presence_sessions",
                schema: "collab",
                newName: "presence_session",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "plans",
                schema: "billing",
                newName: "plan",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "plan_limits",
                schema: "billing",
                newName: "plan_limit",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "permission_templates",
                schema: "governance",
                newName: "permission_template",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "permission_rules",
                schema: "governance",
                newName: "permission_rule",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "payment_methods",
                schema: "billing",
                newName: "payment_method",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "password_reset_tokens",
                schema: "identity",
                newName: "password_reset_token",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "pages",
                schema: "docs",
                newName: "page",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "page_templates",
                schema: "docs",
                newName: "page_template",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "oauth_accounts",
                schema: "identity",
                newName: "o_auth_account",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "notifications",
                schema: "collab",
                newName: "notification",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "notification_preferences",
                schema: "collab",
                newName: "notification_preference",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "notification_deliveries",
                schema: "collab",
                newName: "notification_delivery",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "mirror_value_snapshots",
                schema: "work",
                newName: "mirror_value_snapshot",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "mentions",
                schema: "collab",
                newName: "mention",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "member_role_assignments",
                schema: "governance",
                newName: "member_role_assignment",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "labels",
                schema: "work",
                newName: "label",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "item_templates",
                schema: "work",
                newName: "item_template",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "item_dependencies",
                schema: "work",
                newName: "item_dependency",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "invoices",
                schema: "billing",
                newName: "invoice",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "integration_sync_cursors",
                schema: "integration",
                newName: "integration_sync_cursor",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "integration_secret_versions",
                schema: "integration",
                newName: "integration_secret_version",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "integration_scopes",
                schema: "integration",
                newName: "integration_scope",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "integration_connections",
                schema: "integration",
                newName: "integration_connection",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "inbound_webhook_events",
                schema: "integration",
                newName: "inbound_webhook_event",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "formula_dependencies",
                schema: "work",
                newName: "formula_dependency",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "forms",
                schema: "work",
                newName: "form",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "form_submissions",
                schema: "work",
                newName: "form_submission",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "form_questions",
                schema: "work",
                newName: "form_question",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "field_permissions",
                schema: "governance",
                newName: "field_permission",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "field_options",
                schema: "work",
                newName: "field_option",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "entitlements",
                schema: "billing",
                newName: "entitlement",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "email_verification_tokens",
                schema: "identity",
                newName: "email_verification_token",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "document_versions",
                schema: "docs",
                newName: "document_version",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "dashboards",
                schema: "reporting",
                newName: "dashboard",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "dashboard_widgets",
                schema: "reporting",
                newName: "dashboard_widget",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "dashboard_sources",
                schema: "reporting",
                newName: "dashboard_source",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "custom_roles",
                schema: "governance",
                newName: "custom_role",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "custom_role_permissions",
                schema: "governance",
                newName: "custom_role_permission",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "comments",
                schema: "collab",
                newName: "comment",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "checklists",
                schema: "work",
                newName: "checklist",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "checklist_items",
                schema: "work",
                newName: "checklist_item",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "calendar_integrations",
                schema: "integration",
                newName: "calendar_integration",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "calendar_events",
                schema: "integration",
                newName: "calendar_event",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "calendar_event_links",
                schema: "integration",
                newName: "calendar_event_link",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "boards",
                schema: "work",
                newName: "board",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_views",
                schema: "work",
                newName: "board_view",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_view_user_preferences",
                schema: "work",
                newName: "board_view_user_preference",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_view_pins",
                schema: "work",
                newName: "board_view_pin",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_templates",
                schema: "work",
                newName: "board_template",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_subscribers",
                schema: "work",
                newName: "board_subscriber",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_relations",
                schema: "work",
                newName: "board_relation",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_members",
                schema: "work",
                newName: "board_member",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_items",
                schema: "work",
                newName: "board_item",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_values",
                schema: "work",
                newName: "board_item_value",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_members",
                schema: "work",
                newName: "board_item_member",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_links",
                schema: "work",
                newName: "board_item_link",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_labels",
                schema: "work",
                newName: "board_item_label",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_item_connections",
                schema: "work",
                newName: "board_item_connection",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_groups",
                schema: "work",
                newName: "board_group",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "board_fields",
                schema: "work",
                newName: "board_field",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "blocks",
                schema: "docs",
                newName: "block",
                newSchema: "docs");

            migrationBuilder.RenameTable(
                name: "billing_events",
                schema: "billing",
                newName: "billing_event",
                newSchema: "billing");

            migrationBuilder.RenameTable(
                name: "automation_templates",
                schema: "automation",
                newName: "automation_template",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "automation_rules",
                schema: "automation",
                newName: "automation_rule",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "automation_executions",
                schema: "automation",
                newName: "automation_execution",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "automation_execution_steps",
                schema: "automation",
                newName: "automation_execution_step",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "audit_retention_policies",
                schema: "governance",
                newName: "audit_retention_policy",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                schema: "governance",
                newName: "audit_log",
                newSchema: "governance");

            migrationBuilder.RenameTable(
                name: "attachments",
                schema: "collab",
                newName: "attachment",
                newSchema: "collab");

            migrationBuilder.RenameTable(
                name: "approval_steps",
                schema: "work",
                newName: "approval_step",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "approval_requests",
                schema: "work",
                newName: "approval_request",
                newSchema: "work");

            migrationBuilder.RenameTable(
                name: "api_tokens",
                schema: "identity",
                newName: "api_token",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "ai_agents",
                schema: "automation",
                newName: "ai_agent",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "ai_agent_runs",
                schema: "automation",
                newName: "ai_agent_run",
                newSchema: "automation");

            migrationBuilder.RenameTable(
                name: "activity_logs",
                schema: "collab",
                newName: "activity_log",
                newSchema: "collab");

            migrationBuilder.RenameColumn(
                name: "settings",
                schema: "identity",
                table: "user_security_settings",
                newName: "settings_json");

            migrationBuilder.RenameColumn(
                name: "saved_filter_id",
                schema: "work",
                table: "saved_filter_Rules",
                newName: "SavedFilterId");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                schema: "identity",
                table: "user_session",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "ExpiredAt",
                schema: "identity",
                table: "user_session",
                newName: "expired_at");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "user_profile",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "RestoredBy",
                schema: "identity",
                table: "user_profile",
                newName: "restored_by");

            migrationBuilder.RenameColumn(
                name: "RestoredAt",
                schema: "identity",
                table: "user_profile",
                newName: "restored_at");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                schema: "identity",
                table: "user_profile",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "user_profile",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DeleteReason",
                schema: "identity",
                table: "user_profile",
                newName: "delete_reason");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "user_profile",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "user_profile",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "idx_user_profiles_user_id",
                schema: "identity",
                table: "user_profile",
                newName: "IX_user_profile_user_id");

            migrationBuilder.RenameColumn(
                name: "metric_key",
                schema: "billing",
                table: "usage_metric",
                newName: "key");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "workspace",
                table: "team_member",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "WorkspaceMemberId",
                schema: "workspace",
                table: "team_member",
                newName: "workspace_member_id");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                schema: "workspace",
                table: "team_member",
                newName: "workspace_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "workspace",
                table: "team_member",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "workspace",
                table: "team_member",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "workspace",
                table: "team_member",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "workspace",
                table: "team_member",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "SpaceType",
                schema: "workspace",
                table: "space",
                newName: "space_type");

            migrationBuilder.RenameColumn(
                name: "event_type",
                schema: "governance",
                table: "security_event",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "governance",
                table: "security_event",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "governance",
                table: "security_event",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RestoredBy",
                schema: "governance",
                table: "security_event",
                newName: "restored_by");

            migrationBuilder.RenameColumn(
                name: "RestoredAt",
                schema: "governance",
                table: "security_event",
                newName: "restored_at");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                schema: "governance",
                table: "security_event",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                schema: "governance",
                table: "security_event",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DeleteReason",
                schema: "governance",
                table: "security_event",
                newName: "delete_reason");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "governance",
                table: "security_event",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "governance",
                table: "security_event",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "cursor",
                schema: "identity",
                table: "scim_directory_sync",
                newName: "cursor_json");

            migrationBuilder.RenameColumn(
                name: "config",
                schema: "identity",
                table: "scim_directory_sync",
                newName: "config_json");

            migrationBuilder.RenameColumn(
                name: "group_rule_id",
                schema: "work",
                table: "saved_filter",
                newName: "group_rule");

            migrationBuilder.RenameColumn(
                name: "watch_level",
                schema: "collab",
                table: "resource_watcher",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "permission_level",
                schema: "governance",
                table: "resource_permission",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "link_type",
                schema: "docs",
                table: "resource_link",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "collab",
                table: "reaction",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "collab",
                table: "reaction",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RestoredBy",
                schema: "collab",
                table: "reaction",
                newName: "restored_by");

            migrationBuilder.RenameColumn(
                name: "RestoredAt",
                schema: "collab",
                table: "reaction",
                newName: "restored_at");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                schema: "collab",
                table: "reaction",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                schema: "collab",
                table: "reaction",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DeleteReason",
                schema: "collab",
                table: "reaction",
                newName: "delete_reason");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "collab",
                table: "reaction",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "collab",
                table: "reaction",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "limit_value",
                schema: "billing",
                table: "plan_limit",
                newName: "limit");

            migrationBuilder.RenameIndex(
                name: "idx_plan_limits_plan_id",
                schema: "billing",
                table: "plan_limit",
                newName: "IX_plan_limit_plan_id");

            migrationBuilder.RenameIndex(
                name: "idx_oauth_accounts_user_id",
                schema: "identity",
                table: "o_auth_account",
                newName: "IX_o_auth_account_user_id");

            migrationBuilder.RenameColumn(
                name: "mentioned_user_id",
                schema: "collab",
                table: "mention",
                newName: "mentioned_id");

            migrationBuilder.RenameColumn(
                name: "cursor_value",
                schema: "integration",
                table: "integration_sync_cursor",
                newName: "cursor");

            migrationBuilder.RenameColumn(
                name: "limit_value",
                schema: "billing",
                table: "entitlement",
                newName: "limit");

            migrationBuilder.RenameIndex(
                name: "idx_dashboard_widgets_dashboard_id",
                schema: "reporting",
                table: "dashboard_widget",
                newName: "IX_dashboard_widget_dashboard_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "governance",
                table: "custom_role",
                newName: "status");

            migrationBuilder.RenameIndex(
                name: "idx_custom_role_permissions_role_id",
                schema: "governance",
                table: "custom_role_permission",
                newName: "IX_custom_role_permission_custom_role_id");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "collab",
                table: "comment",
                newName: "comment_status");

            migrationBuilder.RenameColumn(
                name: "sync_hash",
                schema: "integration",
                table: "calendar_event",
                newName: "SyncHash_Value");

            migrationBuilder.RenameColumn(
                name: "etag",
                schema: "integration",
                table: "calendar_event_link",
                newName: "e_tag");

            migrationBuilder.RenameColumn(
                name: "SpaceId",
                schema: "work",
                table: "board",
                newName: "space_id");

            migrationBuilder.RenameColumn(
                name: "ItemSequence",
                schema: "work",
                table: "board",
                newName: "item_sequence");

            migrationBuilder.RenameColumn(
                name: "ItemKeyPrefix",
                schema: "work",
                table: "board",
                newName: "item_key_prefix");

            migrationBuilder.RenameColumn(
                name: "DefaultItemGroupId",
                schema: "work",
                table: "board",
                newName: "default_item_group_id");

            migrationBuilder.RenameColumn(
                name: "BoardType",
                schema: "work",
                table: "board",
                newName: "board_type");

            migrationBuilder.RenameColumn(
                name: "BoardFamily",
                schema: "work",
                table: "board",
                newName: "board_family");

            migrationBuilder.RenameColumn(
                name: "group_rule_id",
                schema: "work",
                table: "board_view_user_preference",
                newName: "group_rule");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                schema: "work",
                table: "board_item",
                newName: "started_at");

            migrationBuilder.RenameColumn(
                name: "ParentItemId",
                schema: "work",
                table: "board_item",
                newName: "parent_item_id");

            migrationBuilder.RenameColumn(
                name: "ItemSequence",
                schema: "work",
                table: "board_item",
                newName: "item_sequence");

            migrationBuilder.RenameColumn(
                name: "ItemLevel",
                schema: "work",
                table: "board_item",
                newName: "item_level");

            migrationBuilder.RenameColumn(
                name: "ItemKey",
                schema: "work",
                table: "board_item",
                newName: "item_key");

            migrationBuilder.RenameColumn(
                name: "DueAt",
                schema: "work",
                table: "board_item",
                newName: "due_at");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                schema: "work",
                table: "board_item",
                newName: "completed_at");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                schema: "work",
                table: "board_item_member",
                newName: "workspace_id");

            migrationBuilder.RenameColumn(
                name: "BoardId",
                schema: "work",
                table: "board_item_member",
                newName: "board_id");

            migrationBuilder.RenameColumn(
                name: "AssignedByUserId",
                schema: "work",
                table: "board_item_member",
                newName: "assigned_by_user_id");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                schema: "work",
                table: "board_item_link",
                newName: "workspace_id");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "work",
                table: "board_item_link",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "work",
                table: "board_item_link",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BoardId",
                schema: "work",
                table: "board_item_link",
                newName: "board_id");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "work",
                table: "board_item_label",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "work",
                table: "board_item_label",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "MirrorSourceJson",
                schema: "work",
                table: "board_field",
                newName: "mirror_source_json");

            migrationBuilder.RenameColumn(
                name: "IsSensitive",
                schema: "work",
                table: "board_field",
                newName: "is_sensitive");

            migrationBuilder.RenameColumn(
                name: "IsFormula",
                schema: "work",
                table: "board_field",
                newName: "is_formula");

            migrationBuilder.RenameColumn(
                name: "FormulaExpression",
                schema: "work",
                table: "board_field",
                newName: "formula_expression");

            migrationBuilder.RenameColumn(
                name: "DataClassification",
                schema: "work",
                table: "board_field",
                newName: "data_classification");

            migrationBuilder.RenameIndex(
                name: "idx_approval_steps_request_id",
                schema: "work",
                table: "approval_step",
                newName: "IX_approval_step_approval_request_id");

            migrationBuilder.RenameColumn(
                name: "target_workspace_id",
                schema: "collab",
                table: "activity_log",
                newName: "updated_by");

            migrationBuilder.AlterColumn<int>(
                name: "preferred_mfa_method",
                schema: "identity",
                table: "user_security_settings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "settings_json",
                schema: "identity",
                table: "user_security_settings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "user_security_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "usage_metric_id",
                schema: "billing",
                table: "usage_metric_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "work",
                table: "saved_filter_Rules",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "reference_resource",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "note",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "feature_code",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<decimal>(
                name: "delta",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                schema: "workspace",
                table: "workspace",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workspace",
                table: "workspace",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "workspace",
                table: "workspace",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "workspace",
                table: "workspace",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "workspace",
                table: "workspace_member",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "workspace",
                table: "workspace_invitation",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "workspace",
                table: "workspace_invitation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "soft_limit",
                schema: "billing",
                table: "workspace_feature_usage",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reset_period",
                schema: "billing",
                table: "workspace_feature_usage",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "hard_limit",
                schema: "billing",
                table: "workspace_feature_usage",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "current_usage",
                schema: "billing",
                table: "workspace_feature_usage",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "billing",
                table: "workspace_feature_usage",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "allocation_date",
                schema: "work",
                table: "workload_allocation",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "integration",
                table: "webhook_subscription",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "payload",
                schema: "integration",
                table: "webhook_delivery",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "integration",
                table: "webhook_delivery",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "user",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "user_agent",
                schema: "identity",
                table: "user_session",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "user_session",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "user_session",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "timezone",
                schema: "identity",
                table: "user_profile",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "UTC");

            migrationBuilder.AlterColumn<string>(
                name: "theme",
                schema: "identity",
                table: "user_profile",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "system");

            migrationBuilder.AlterColumn<string>(
                name: "preferences",
                schema: "identity",
                table: "user_profile",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                schema: "identity",
                table: "user_profile",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldDefaultValue: "vi");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "destination_masked",
                schema: "identity",
                table: "user_mfa_method",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "user_mfa_method",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "user_agent",
                schema: "identity",
                table: "user_login_attempt",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "user_login_attempt",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attempted_email",
                schema: "identity",
                table: "user_login_attempt",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "user_login_attempt",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                schema: "billing",
                table: "usage_metric",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "billing",
                table: "usage_metric",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "time_tracking_entry",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workspace",
                table: "team",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "workspace",
                table: "team",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "workspace",
                table: "team",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "billing",
                table: "subscription",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "sso_provider",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "domain",
                schema: "identity",
                table: "sso_provider",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "sso_provider",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                schema: "identity",
                table: "sso_provider",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workspace",
                table: "space",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "workspace",
                table: "space",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "workspace",
                table: "space",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "governance",
                table: "security_event",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "governance",
                table: "security_event",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "governance",
                table: "security_event",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "provider_name",
                schema: "identity",
                table: "scim_directory_sync",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "cursor_json",
                schema: "identity",
                table: "scim_directory_sync",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "config_json",
                schema: "identity",
                table: "scim_directory_sync",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "{}");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "scim_directory_sync",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "automation",
                table: "scheduled_job",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "saved_filter",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "saved_filter",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "value",
                schema: "work",
                table: "rollup_snapshot",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "collab",
                table: "resource_watcher",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                schema: "governance",
                table: "resource_permission",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<string>(
                name: "condition_json",
                schema: "governance",
                table: "resource_permission",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "governance",
                table: "resource_permission",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "docs",
                table: "resource_link",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "report_type",
                schema: "reporting",
                table: "reporting_snapshot",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "data",
                schema: "reporting",
                table: "reporting_snapshot",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "collab",
                table: "reaction",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "connection_id",
                schema: "collab",
                table: "presence_session",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "billing",
                table: "plan",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "billing",
                table: "plan",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "billing",
                table: "plan",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "target_resource_type",
                schema: "governance",
                table: "permission_template",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "permissions_json",
                schema: "governance",
                table: "permission_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "governance",
                table: "permission_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "governance",
                table: "permission_template",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "governance",
                table: "permission_template",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "subject_type",
                schema: "governance",
                table: "permission_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "subject_key",
                schema: "governance",
                table: "permission_rule",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "governance",
                table: "permission_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "scope_type",
                schema: "governance",
                table: "permission_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "resource_type",
                schema: "governance",
                table: "permission_rule",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                schema: "governance",
                table: "permission_rule",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<string>(
                name: "condition_json",
                schema: "governance",
                table: "permission_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "governance",
                table: "permission_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "governance",
                table: "permission_rule",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "provider_method_id",
                schema: "billing",
                table: "payment_method",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "last4",
                schema: "billing",
                table: "payment_method",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4)",
                oldMaxLength: 4);

            migrationBuilder.AlterColumn<string>(
                name: "brand",
                schema: "billing",
                table: "payment_method",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "billing",
                table: "payment_method",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "password_reset_token",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "docs",
                table: "page",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "icon",
                schema: "docs",
                table: "page",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "📄");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "docs",
                table: "page",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "page_snapshot",
                schema: "docs",
                table: "page_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "docs",
                table: "page_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "docs",
                table: "page_template",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                schema: "docs",
                table: "page_template",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "blocks_snapshot",
                schema: "docs",
                table: "page_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "docs",
                table: "page_template",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "raw_profile",
                schema: "identity",
                table: "o_auth_account",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "provider_id",
                schema: "identity",
                table: "o_auth_account",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "collab",
                table: "notification",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                schema: "collab",
                table: "notification",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "collab",
                table: "notification",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "provider_message_id",
                schema: "collab",
                table: "notification_delivery",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "error_message",
                schema: "collab",
                table: "notification_delivery",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "label",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "label",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "values",
                schema: "work",
                table: "item_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "item_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "item_template",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "item_dependency",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "number",
                schema: "billing",
                table: "invoice",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "billing",
                table: "invoice",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "resource_type",
                schema: "integration",
                table: "integration_sync_cursor",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "cursor",
                schema: "integration",
                table: "integration_sync_cursor",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "version",
                schema: "integration",
                table: "integration_secret_version",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "secret_reference",
                schema: "integration",
                table: "integration_secret_version",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<Guid>(
                name: "integration_connection_id",
                schema: "integration",
                table: "integration_secret_version",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "scope",
                schema: "integration",
                table: "integration_scope",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<Guid>(
                name: "integration_connection_id",
                schema: "integration",
                table: "integration_scope",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider_account_id",
                schema: "integration",
                table: "integration_connection",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "integration",
                table: "integration_connection",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                schema: "integration",
                table: "inbound_webhook_event",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "payload",
                schema: "integration",
                table: "inbound_webhook_event",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "external_event_id",
                schema: "integration",
                table: "inbound_webhook_event",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                schema: "integration",
                table: "inbound_webhook_event",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "integration",
                table: "inbound_webhook_event",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                schema: "work",
                table: "form",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "form",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "form",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "submitter_email",
                schema: "work",
                table: "form_submission",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "source_ip",
                schema: "work",
                table: "form_submission",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "question_type",
                schema: "work",
                table: "form_question",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "question_key",
                schema: "work",
                table: "form_question",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "form_question",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "work",
                table: "form_question",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<string>(
                name: "config_json",
                schema: "work",
                table: "form_question",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "version",
                schema: "governance",
                table: "field_permission",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "condition_json",
                schema: "governance",
                table: "field_permission",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "field_option",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "field_option",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<Guid>(
                name: "board_field_id",
                schema: "work",
                table: "field_option",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "billing",
                table: "entitlement",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "email_verification_token",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "change_summary",
                schema: "docs",
                table: "document_version",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "docs",
                table: "document_version",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "reporting",
                table: "dashboard",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "reporting",
                table: "dashboard",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "reporting",
                table: "dashboard_widget",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "reporting",
                table: "dashboard_widget",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "config",
                schema: "reporting",
                table: "dashboard_widget",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "filter",
                schema: "reporting",
                table: "dashboard_source",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "reporting",
                table: "dashboard_source",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "governance",
                table: "custom_role",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "governance",
                table: "custom_role",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "governance",
                table: "custom_role",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "conditions",
                schema: "governance",
                table: "custom_role_permission",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "governance",
                table: "custom_role_permission",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                schema: "collab",
                table: "comment",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "collab",
                table: "comment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "checklist",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "checklist",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "checklist",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "checklist_item",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "checklist_item",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "integration",
                table: "calendar_integration",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "external_event_id",
                schema: "integration",
                table: "calendar_event",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "SyncHash_Value",
                schema: "integration",
                table: "calendar_event",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "external_event_id",
                schema: "integration",
                table: "calendar_event_link",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "e_tag",
                schema: "integration",
                table: "calendar_event_link",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "calendar_integration_id",
                schema: "integration",
                table: "calendar_event_link",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "visibility",
                schema: "work",
                table: "board",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Workspace");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "board",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "work",
                table: "board",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "background",
                schema: "work",
                table: "board",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"type\":\"color\",\"value\":\"#0079BF\"}");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "board",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "board_view",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "board_view",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "board_view_user_preference",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "structure",
                schema: "work",
                table: "board_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "board_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "board_template",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "version",
                schema: "work",
                table: "board_relation",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 1L);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "board_relation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "board_item",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "board_item",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "board_item_id",
                schema: "work",
                table: "board_item_value",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "board_group",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "board_group",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "board_group",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "work",
                table: "board_field",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "board_field",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "docs",
                table: "block",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "docs",
                table: "block",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "raw_data",
                schema: "billing",
                table: "billing_event",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "provider_event_id",
                schema: "billing",
                table: "billing_event",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "billing",
                table: "billing_event",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "automation",
                table: "automation_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "automation",
                table: "automation_template",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "definition",
                schema: "automation",
                table: "automation_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "category",
                schema: "automation",
                table: "automation_template",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "automation",
                table: "automation_template",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "trigger_event",
                schema: "automation",
                table: "automation_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "automation",
                table: "automation_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "automation",
                table: "automation_rule",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "configuration",
                schema: "automation",
                table: "automation_rule",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "action_type",
                schema: "automation",
                table: "automation_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "automation",
                table: "automation_rule",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_run_at",
                schema: "automation",
                table: "automation_rule",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payload",
                schema: "automation",
                table: "automation_execution",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "automation",
                table: "automation_execution",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "automation_execution_id",
                schema: "automation",
                table: "automation_execution_step",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "retention_days",
                schema: "governance",
                table: "audit_retention_policy",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 365);

            migrationBuilder.AlterColumn<string>(
                name: "policy_json",
                schema: "governance",
                table: "audit_retention_policy",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "user_agent",
                schema: "governance",
                table: "audit_log",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "resource_type",
                schema: "governance",
                table: "audit_log",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "governance",
                table: "audit_log",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45);

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "governance",
                table: "audit_log",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "collab",
                table: "attachment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "work",
                table: "approval_request",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "work",
                table: "approval_request",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "api_token",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "api_token",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "scopes_json",
                schema: "identity",
                table: "api_token",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "tool_permissions",
                schema: "automation",
                table: "ai_agent",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "automation",
                table: "ai_agent",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "model_policy",
                schema: "automation",
                table: "ai_agent",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "instruction",
                schema: "automation",
                table: "ai_agent",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "automation",
                table: "ai_agent",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "automation",
                table: "ai_agent",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "trigger_type",
                schema: "automation",
                table: "ai_agent_run",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "trigger_resource_type",
                schema: "automation",
                table: "ai_agent_run",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "output",
                schema: "automation",
                table: "ai_agent_run",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "input",
                schema: "automation",
                table: "ai_agent_run",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "error",
                schema: "automation",
                table: "ai_agent_run",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "automation",
                table: "ai_agent_run",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "collab",
                table: "activity_log",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "collab",
                table: "activity_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delete_reason",
                schema: "collab",
                table: "activity_log",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "collab",
                table: "activity_log",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                schema: "collab",
                table: "activity_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "collab",
                table: "activity_log",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "restored_at",
                schema: "collab",
                table: "activity_log",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                schema: "collab",
                table: "activity_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "collab",
                table: "activity_log",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "collab",
                table: "activity_log",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_saved_filter_Rules",
                schema: "work",
                table: "saved_filter_Rules",
                columns: new[] { "SavedFilterId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace",
                schema: "workspace",
                table: "workspace",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_policy",
                schema: "governance",
                table: "workspace_policy",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_member",
                schema: "workspace",
                table: "workspace_member",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_invitation",
                schema: "workspace",
                table: "workspace_invitation",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_feature_usage",
                schema: "billing",
                table: "workspace_feature_usage",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workload_allocation",
                schema: "work",
                table: "workload_allocation",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_webhook_subscription",
                schema: "integration",
                table: "webhook_subscription",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_webhook_delivery",
                schema: "integration",
                table: "webhook_delivery",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user",
                schema: "identity",
                table: "user",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_session",
                schema: "identity",
                table: "user_session",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_profile",
                schema: "identity",
                table: "user_profile",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_mfa_method",
                schema: "identity",
                table: "user_mfa_method",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_login_attempt",
                schema: "identity",
                table: "user_login_attempt",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usage_metric",
                schema: "billing",
                table: "usage_metric",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_time_tracking_entry",
                schema: "work",
                table: "time_tracking_entry",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_team",
                schema: "workspace",
                table: "team",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_team_member",
                schema: "workspace",
                table: "team_member",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_subscription",
                schema: "billing",
                table: "subscription",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sso_provider",
                schema: "identity",
                table: "sso_provider",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_space",
                schema: "workspace",
                table: "space",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_share_link",
                schema: "governance",
                table: "share_link",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_security_event",
                schema: "governance",
                table: "security_event",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_scim_directory_sync",
                schema: "identity",
                table: "scim_directory_sync",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_scheduled_job",
                schema: "automation",
                table: "scheduled_job",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_saved_filter",
                schema: "work",
                table: "saved_filter",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_rollup_snapshot",
                schema: "work",
                table: "rollup_snapshot",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_resource_watcher",
                schema: "collab",
                table: "resource_watcher",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_resource_permission",
                schema: "governance",
                table: "resource_permission",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_resource_link",
                schema: "docs",
                table: "resource_link",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reporting_snapshot",
                schema: "reporting",
                table: "reporting_snapshot",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_relation_field_config",
                schema: "work",
                table: "relation_field_config",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reaction",
                schema: "collab",
                table: "reaction",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_presence_session",
                schema: "collab",
                table: "presence_session",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_plan",
                schema: "billing",
                table: "plan",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_plan_limit",
                schema: "billing",
                table: "plan_limit",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permission_template",
                schema: "governance",
                table: "permission_template",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permission_rule",
                schema: "governance",
                table: "permission_rule",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payment_method",
                schema: "billing",
                table: "payment_method",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_password_reset_token",
                schema: "identity",
                table: "password_reset_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_page",
                schema: "docs",
                table: "page",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_page_template",
                schema: "docs",
                table: "page_template",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_o_auth_account",
                schema: "identity",
                table: "o_auth_account",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notification",
                schema: "collab",
                table: "notification",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notification_preference",
                schema: "collab",
                table: "notification_preference",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notification_delivery",
                schema: "collab",
                table: "notification_delivery",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_mirror_value_snapshot",
                schema: "work",
                table: "mirror_value_snapshot",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_mention",
                schema: "collab",
                table: "mention",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_member_role_assignment",
                schema: "governance",
                table: "member_role_assignment",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_label",
                schema: "work",
                table: "label",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_item_template",
                schema: "work",
                table: "item_template",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_item_dependency",
                schema: "work",
                table: "item_dependency",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_invoice",
                schema: "billing",
                table: "invoice",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_integration_sync_cursor",
                schema: "integration",
                table: "integration_sync_cursor",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_integration_secret_version",
                schema: "integration",
                table: "integration_secret_version",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_integration_scope",
                schema: "integration",
                table: "integration_scope",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_integration_connection",
                schema: "integration",
                table: "integration_connection",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbound_webhook_event",
                schema: "integration",
                table: "inbound_webhook_event",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_formula_dependency",
                schema: "work",
                table: "formula_dependency",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_form",
                schema: "work",
                table: "form",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_form_submission",
                schema: "work",
                table: "form_submission",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_form_question",
                schema: "work",
                table: "form_question",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_field_permission",
                schema: "governance",
                table: "field_permission",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_field_option",
                schema: "work",
                table: "field_option",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_entitlement",
                schema: "billing",
                table: "entitlement",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_email_verification_token",
                schema: "identity",
                table: "email_verification_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_document_version",
                schema: "docs",
                table: "document_version",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dashboard",
                schema: "reporting",
                table: "dashboard",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dashboard_widget",
                schema: "reporting",
                table: "dashboard_widget",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dashboard_source",
                schema: "reporting",
                table: "dashboard_source",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_custom_role",
                schema: "governance",
                table: "custom_role",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_custom_role_permission",
                schema: "governance",
                table: "custom_role_permission",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_comment",
                schema: "collab",
                table: "comment",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_checklist",
                schema: "work",
                table: "checklist",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_checklist_item",
                schema: "work",
                table: "checklist_item",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_integration",
                schema: "integration",
                table: "calendar_integration",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_event",
                schema: "integration",
                table: "calendar_event",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_calendar_event_link",
                schema: "integration",
                table: "calendar_event_link",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board",
                schema: "work",
                table: "board",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_view",
                schema: "work",
                table: "board_view",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_view_user_preference",
                schema: "work",
                table: "board_view_user_preference",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_view_pin",
                schema: "work",
                table: "board_view_pin",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_template",
                schema: "work",
                table: "board_template",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_subscriber",
                schema: "work",
                table: "board_subscriber",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_relation",
                schema: "work",
                table: "board_relation",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_member",
                schema: "work",
                table: "board_member",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item",
                schema: "work",
                table: "board_item",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_value",
                schema: "work",
                table: "board_item_value",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_member",
                schema: "work",
                table: "board_item_member",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_link",
                schema: "work",
                table: "board_item_link",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_label",
                schema: "work",
                table: "board_item_label",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_connection",
                schema: "work",
                table: "board_item_connection",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_group",
                schema: "work",
                table: "board_group",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_field",
                schema: "work",
                table: "board_field",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_block",
                schema: "docs",
                table: "block",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_billing_event",
                schema: "billing",
                table: "billing_event",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_template",
                schema: "automation",
                table: "automation_template",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_rule",
                schema: "automation",
                table: "automation_rule",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_execution",
                schema: "automation",
                table: "automation_execution",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_automation_execution_step",
                schema: "automation",
                table: "automation_execution_step",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_retention_policy",
                schema: "governance",
                table: "audit_retention_policy",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_log",
                schema: "governance",
                table: "audit_log",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_attachment",
                schema: "collab",
                table: "attachment",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_approval_step",
                schema: "work",
                table: "approval_step",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_approval_request",
                schema: "work",
                table: "approval_request",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_api_token",
                schema: "identity",
                table: "api_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ai_agent",
                schema: "automation",
                table: "ai_agent",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ai_agent_run",
                schema: "automation",
                table: "ai_agent_run",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_activity_log",
                schema: "collab",
                table: "activity_log",
                column: "id");

            migrationBuilder.CreateTable(
                name: "automation_action",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_action", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automation_condition",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_condition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automation_trigger",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_trigger", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "board_view_user_preference_FilterRules",
                schema: "work",
                columns: table => new
                {
                    BoardViewUserPreferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_view_user_preference_FilterRules", x => new { x.BoardViewUserPreferenceId, x.Id });
                    table.ForeignKey(
                        name: "FK_board_view_user_preference_FilterRules_board_view_user_pref~",
                        column: x => x.BoardViewUserPreferenceId,
                        principalSchema: "work",
                        principalTable: "board_view_user_preference",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_view_user_preference_SortRules",
                schema: "work",
                columns: table => new
                {
                    BoardViewUserPreferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_view_user_preference_SortRules", x => new { x.BoardViewUserPreferenceId, x.Id });
                    table.ForeignKey(
                        name: "FK_board_view_user_preference_SortRules_board_view_user_prefer~",
                        column: x => x.BoardViewUserPreferenceId,
                        principalSchema: "work",
                        principalTable: "board_view_user_preference",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_subscriber",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscribed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    subscribed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_subscriber", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "saved_filter_SortRules",
                schema: "work",
                columns: table => new
                {
                    SavedFilterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_filter_SortRules", x => new { x.SavedFilterId, x.Id });
                    table.ForeignKey(
                        name: "FK_saved_filter_SortRules_saved_filter_SavedFilterId",
                        column: x => x.SavedFilterId,
                        principalSchema: "work",
                        principalTable: "saved_filter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unread_counter",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    counter_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    counter_value = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unread_counter", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usage_metric_history_usage_metric_id",
                schema: "billing",
                table: "usage_metric_history",
                column: "usage_metric_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_member_team_id",
                schema: "workspace",
                table: "team_member",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_secret_version_integration_connection_id",
                schema: "integration",
                table: "integration_secret_version",
                column: "integration_connection_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_scope_integration_connection_id",
                schema: "integration",
                table: "integration_scope",
                column: "integration_connection_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_question_form_id",
                schema: "work",
                table: "form_question",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_option_board_field_id",
                schema: "work",
                table: "field_option",
                column: "board_field_id");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_item_checklist_id",
                schema: "work",
                table: "checklist_item",
                column: "checklist_id");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_event_link_calendar_integration_id",
                schema: "integration",
                table: "calendar_event_link",
                column: "calendar_integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_board_item_value_board_item_id",
                schema: "work",
                table: "board_item_value",
                column: "board_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_automation_execution_step_automation_execution_id",
                schema: "automation",
                table: "automation_execution_step",
                column: "automation_execution_id");

            migrationBuilder.AddForeignKey(
                name: "FK_approval_step_approval_request_approval_request_id",
                schema: "work",
                table: "approval_step",
                column: "approval_request_id",
                principalSchema: "work",
                principalTable: "approval_request",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_automation_execution_step_automation_execution_automation_e~",
                schema: "automation",
                table: "automation_execution_step",
                column: "automation_execution_id",
                principalSchema: "automation",
                principalTable: "automation_execution",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_value_board_item_board_item_id",
                schema: "work",
                table: "board_item_value",
                column: "board_item_id",
                principalSchema: "work",
                principalTable: "board_item",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_event_link_calendar_integration_calendar_integrati~",
                schema: "integration",
                table: "calendar_event_link",
                column: "calendar_integration_id",
                principalSchema: "integration",
                principalTable: "calendar_integration",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_checklist_item_checklist_checklist_id",
                schema: "work",
                table: "checklist_item",
                column: "checklist_id",
                principalSchema: "work",
                principalTable: "checklist",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_custom_role_permission_custom_role_custom_role_id",
                schema: "governance",
                table: "custom_role_permission",
                column: "custom_role_id",
                principalSchema: "governance",
                principalTable: "custom_role",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dashboard_widget_dashboard_dashboard_id",
                schema: "reporting",
                table: "dashboard_widget",
                column: "dashboard_id",
                principalSchema: "reporting",
                principalTable: "dashboard",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_field_option_board_field_board_field_id",
                schema: "work",
                table: "field_option",
                column: "board_field_id",
                principalSchema: "work",
                principalTable: "board_field",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_form_question_form_form_id",
                schema: "work",
                table: "form_question",
                column: "form_id",
                principalSchema: "work",
                principalTable: "form",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_integration_scope_integration_connection_integration_connec~",
                schema: "integration",
                table: "integration_scope",
                column: "integration_connection_id",
                principalSchema: "integration",
                principalTable: "integration_connection",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_integration_secret_version_integration_connection_integrati~",
                schema: "integration",
                table: "integration_secret_version",
                column: "integration_connection_id",
                principalSchema: "integration",
                principalTable: "integration_connection",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_o_auth_account_user_user_id",
                schema: "identity",
                table: "o_auth_account",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_plan_limit_plan_plan_id",
                schema: "billing",
                table: "plan_limit",
                column: "plan_id",
                principalSchema: "billing",
                principalTable: "plan",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_filter_Rules_saved_filter_SavedFilterId",
                schema: "work",
                table: "saved_filter_Rules",
                column: "SavedFilterId",
                principalSchema: "work",
                principalTable: "saved_filter",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_team_member_team_team_id",
                schema: "workspace",
                table: "team_member",
                column: "team_id",
                principalSchema: "workspace",
                principalTable: "team",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_usage_metric_history_usage_metric_usage_metric_id",
                schema: "billing",
                table: "usage_metric_history",
                column: "usage_metric_id",
                principalSchema: "billing",
                principalTable: "usage_metric",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_profile_user_user_id",
                schema: "identity",
                table: "user_profile",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SchemaV2Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "authz");

            migrationBuilder.EnsureSchema(
                name: "account");

            migrationBuilder.EnsureSchema(
                name: "activity");

            migrationBuilder.EnsureSchema(
                name: "automation");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "work");

            migrationBuilder.EnsureSchema(
                name: "collab");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.EnsureSchema(
                name: "docs");

            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.EnsureSchema(
                name: "governance");

            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.EnsureSchema(
                name: "workspace");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "access_grants",
                schema: "authz",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "Workspace"),
                    membership_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    role_codes = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    permission_codes = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    is_account_admin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_workspace_admin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_grants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_domains",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    verification_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    verification_token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    auto_join_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_domains", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_identity_providers",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    issuer = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    sso_url = table.Column<string>(type: "text", nullable: false),
                    certificate_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    jit_provisioning_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_identity_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_invitations",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_members",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_regions",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    data_residency_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    migration_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_regions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_settings",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    setting_value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    default_region_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    plan_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
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
                name: "ai_agent_runs",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    trigger_resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    trigger_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input = table.Column<string>(type: "jsonb", nullable: false),
                    output = table.Column<string>(type: "jsonb", nullable: false),
                    error = table.Column<string>(type: "jsonb", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_agent_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_agents",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    scope_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model_policy = table.Column<string>(type: "jsonb", nullable: false),
                    instruction = table.Column<string>(type: "jsonb", nullable: false),
                    tool_permissions = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_agents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    scopes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approval_requests",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => x.id);
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
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retention_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automation_rules",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true),
                    configuration = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automation_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automation_templates",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    definition = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automation_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "billing_customers",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "billing_events",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    raw_data = table.Column<string>(type: "jsonb", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "board_templates",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    structure = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "boards",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    background = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{\"type\":\"color\",\"value\":\"#0079BF\"}"),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Workspace"),
                    board_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    board_family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_key_prefix = table.Column<string>(type: "text", nullable: true),
                    item_sequence = table.Column<long>(type: "bigint", nullable: false),
                    default_item_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_boards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_integrations",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sync_direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_integrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    anchor_selector = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    anchor_offset = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_comments_comments_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "collab",
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custom_roles",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_sources",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dashboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: true),
                    board_view_id = table.Column<Guid>(type: "uuid", nullable: true),
                    filter = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dashboard_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dashboards",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dashboards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                schema: "docs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    snapshot = table.Column<string>(type: "text", nullable: false),
                    change_summary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_versions", x => x.id);
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
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
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
                    provider_response_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
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
                    content_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    template_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    subject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    body_html = table.Column<string>(type: "text", nullable: true),
                    body_text = table.Column<string>(type: "text", nullable: true),
                    template_data_json = table.Column<string>(type: "jsonb", nullable: true),
                    headers_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    locked_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    lock_token = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    sensitive_payload_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sensitive_payload_cleared_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_outbox", x => x.id);
                    table.CheckConstraint("ck_email_outbox_content_mode", "(content_mode = 'Rendered' AND subject IS NOT NULL AND (body_html IS NOT NULL OR body_text IS NOT NULL) AND template_data_json IS NULL) OR (content_mode = 'Templated' AND subject IS NULL AND body_html IS NULL AND body_text IS NULL AND template_data_json IS NOT NULL AND template_data_json <> '{}'::jsonb) OR (content_mode = 'Purged' AND subject IS NULL AND body_html IS NULL AND body_text IS NULL AND template_data_json IS NULL AND sensitive_payload_cleared_at IS NOT NULL)");
                    table.CheckConstraint("ck_email_outbox_sensitive_payload_state", "sensitive_payload_cleared_at IS NULL OR template_data_json IS NULL");
                });

            migrationBuilder.CreateTable(
                name: "email_verification_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    hash_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_verification_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entitlements",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Account"),
                    target_workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    feature_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    limit_value = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entitlements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "export_jobs",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    source_resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    source_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: true),
                    options_json = table.Column<string>(type: "jsonb", nullable: false),
                    filters_json = table.Column<string>(type: "jsonb", nullable: false),
                    result_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    download_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_jobs", x => x.id);
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
                    calculated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_watermark_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_usage_daily", x => new { x.workspace_id, x.usage_date, x.feature_code });
                });

            migrationBuilder.CreateTable(
                name: "feature_usage_ledger",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    delta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_resource = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_usage_ledger", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "field_permissions",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    can_view = table.Column<bool>(type: "boolean", nullable: false),
                    can_edit = table.Column<bool>(type: "boolean", nullable: false),
                    effect = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    can_mask = table.Column<bool>(type: "boolean", nullable: false),
                    condition_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_field_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "forms",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    settings_json = table.Column<string>(type: "text", nullable: false),
                    submitter_policy_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    request_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    request_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    response_status_code = table.Column<int>(type: "integer", nullable: true),
                    response_body_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_jobs",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    target_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_file_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    total_records = table.Column<int>(type: "integer", nullable: false),
                    processed_records = table.Column<int>(type: "integer", nullable: false),
                    succeeded_records = table.Column<int>(type: "integer", nullable: false),
                    failed_records = table.Column<int>(type: "integer", nullable: false),
                    options_json = table.Column<string>(type: "jsonb", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_summary = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    error_file_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbound_webhook_events",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_event_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbound_webhook_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_connections",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_account_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    error_detail = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    current_secret_version = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    secret_rotated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_connections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_secret_versions",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    secret_reference = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_secret_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_sync_cursors",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cursor_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_sync_cursors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line_items",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_line_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_locks",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lock_key = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    locked_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fencing_token = table.Column<long>(type: "bigint", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    acquired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    renewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_locks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "member_role_assignments",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_role_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mentions",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mentioned_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_counters",
                schema: "notifications",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counter_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "Notification"),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counter_value = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_counters", x => new { x.workspace_id, x.user_id, x.counter_type });
                });

            migrationBuilder.CreateTable(
                name: "notification_items",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    data_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
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
                    quiet_hours_json = table.Column<string>(type: "jsonb", nullable: false),
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
                    error_detail_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
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
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    headers_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
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
                name: "page_templates",
                schema: "docs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    page_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    blocks_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                schema: "docs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "📄"),
                    cover_image = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pages", x => x.id);
                    table.ForeignKey(
                        name: "fk_pages_pages_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "docs",
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    hash_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_method_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission_rules",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_type = table.Column<int>(type: "integer", maxLength: 50, nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effect = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission_templates",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    target_resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    permissions_json = table.Column<string>(type: "jsonb", nullable: false),
                    scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plan_prices",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    billing_interval = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_prices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "presence_sessions",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_presence_sessions", x => x.id);
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
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Processing"),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reactions",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    emoji = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "relation_field_configs",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allow_multiple = table.Column<bool>(type: "boolean", nullable: false),
                    create_backlink = table.Column<bool>(type: "boolean", nullable: false),
                    backlink_field_id = table.Column<Guid>(type: "uuid", nullable: true),
                    direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relation_field_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reporting_snapshots",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reporting_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_links",
                schema: "docs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    link_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_permission_inheritance_cache",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    parent_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    action = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    effect = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Allow"),
                    permission_level = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    source_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inherited_from_resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    inherited_from_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cache_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    computed_permissions_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_permission_inheritance_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_permissions",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effect = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_read_states",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_read_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unread_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_read_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_watchers",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    watch_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_watchers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_jobs",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "UTC"),
                    schedule_schema_version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    next_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scim_directories",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    base_url = table.Column<string>(type: "text", nullable: true),
                    bearer_token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scim_directories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scim_sync_runs",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    directory_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    users_created = table.Column<int>(type: "integer", nullable: false),
                    users_updated = table.Column<int>(type: "integer", nullable: false),
                    users_disabled = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scim_sync_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "search_documents",
                schema: "search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_search_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "search_index_jobs",
                schema: "search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    locked_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_search_index_jobs", x => x.id);
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
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retention_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "share_links",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    access_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spaces",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    space_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_items",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    current_period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usage_metrics",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    current_value = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usage_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_login_attempts",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempted_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_login_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_mfa_methods",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    secret_ref = table.Column<string>(type: "text", nullable: true),
                    destination_masked = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_mfa_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_security_settings",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_mfa_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    preferred_mfa_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    require_password_change = table.Column<bool>(type: "boolean", nullable: false),
                    password_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_security_review_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    settings = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_security_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    avatar = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    email_confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_deliveries",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    response_status_code = table.Column<int>(type: "integer", nullable: true),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_deliveries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_subscriptions",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    secret_hash = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workload_allocations",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_date = table.Column<DateTime>(type: "date", nullable: false),
                    allocated_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workload_allocations", x => x.id);
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
                    data_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
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
                name: "workspace_feature_usages",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    current_usage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    hard_limit = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    soft_limit = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    overage_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    reset_period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_reset_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_feature_usages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_invitations",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hash_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    token_generation = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_members",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_policies",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_allow_invites = table.Column<bool>(type: "boolean", nullable: false),
                    resource_allow_public_sharing = table.Column<bool>(type: "boolean", nullable: false),
                    sharing_allow_public = table.Column<bool>(type: "boolean", nullable: false),
                    sharing_allow_external_invite = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_routes",
                schema: "account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    route_slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_routes", x => x.id);
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
                    calculated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_watermark_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_usage_daily", x => new { x.workspace_id, x.usage_date });
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    settings_allow_public_sharing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    settings_enforce_mfa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_personal = table.Column<bool>(type: "boolean", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approval_steps",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approver_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_steps_approval_requests_approval_request_id",
                        column: x => x.approval_request_id,
                        principalSchema: "work",
                        principalTable: "approval_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "automation_executions",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_response = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automation_executions", x => x.id);
                    table.ForeignKey(
                        name: "fk_automation_executions_automation_rules_rule_id",
                        column: x => x.rule_id,
                        principalSchema: "automation",
                        principalTable: "automation_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_fields",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    position = table.Column<string>(type: "text", nullable: false),
                    default_value = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    data_classification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_fields_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_groups",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_collapsed = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_groups_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_members",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_members_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_relations",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_field_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_field_id = table.Column<Guid>(type: "uuid", nullable: true),
                    relation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sync_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_relations_boards_source_board_id",
                        column: x => x.source_board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_board_relations_boards_target_board_id",
                        column: x => x.target_board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "board_subscribers",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notification_json = table.Column<string>(type: "text", nullable: false),
                    subscribed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    subscribed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_subscribers", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_subscribers_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_views",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_views", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_views_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_templates",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    values = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_templates_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "labels",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_labels", x => x.id);
                    table.ForeignKey(
                        name: "fk_labels_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calendar_event_links",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    internal_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_event_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    etag = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_event_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_calendar_event_links_calendar_integrations_integration_id",
                        column: x => x.integration_id,
                        principalSchema: "integration",
                        principalTable: "calendar_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calendar_events",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_event_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sync_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_calendar_events_calendar_integrations_integration_id",
                        column: x => x.integration_id,
                        principalSchema: "integration",
                        principalTable: "calendar_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "custom_role_permissions",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_role_permissions_custom_roles_custom_role_id",
                        column: x => x.custom_role_id,
                        principalSchema: "governance",
                        principalTable: "custom_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_widgets",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dashboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: false),
                    pos_x = table.Column<int>(type: "integer", nullable: false),
                    pos_y = table.Column<int>(type: "integer", nullable: false),
                    pos_w = table.Column<int>(type: "integer", nullable: false),
                    pos_h = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dashboard_widgets", x => x.id);
                    table.ForeignKey(
                        name: "fk_dashboard_widgets_dashboards_dashboard_id",
                        column: x => x.dashboard_id,
                        principalSchema: "reporting",
                        principalTable: "dashboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_questions",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_field_id = table.Column<Guid>(type: "uuid", nullable: true),
                    question_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    label = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    question_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_questions_forms_form_id",
                        column: x => x.form_id,
                        principalSchema: "work",
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_submissions",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitter_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitter_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    source_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_submissions_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_form_submissions_forms_form_id",
                        column: x => x.form_id,
                        principalSchema: "work",
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "integration_scopes",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_scopes", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_scopes_integration_connections_connection_id",
                        column: x => x.connection_id,
                        principalSchema: "integration",
                        principalTable: "integration_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_recipients",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_email = table.Column<string>(type: "text", nullable: true),
                    recipient_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    delivery_policy_json = table.Column<string>(type: "jsonb", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "blocks",
                schema: "docs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    properties = table.Column<string>(type: "jsonb", nullable: false),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blocks", x => x.id);
                    table.ForeignKey(
                        name: "fk_blocks_blocks_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "docs",
                        principalTable: "blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_blocks_pages_page_id",
                        column: x => x.page_id,
                        principalSchema: "docs",
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_limits",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    limit_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_limits", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_limits_plans_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "billing",
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    workspace_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_members_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "workspace",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_metric_history",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_id = table.Column<Guid>(type: "uuid", nullable: false),
                    increment = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usage_metric_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_usage_metric_history_usage_metrics_metric_id",
                        column: x => x.metric_id,
                        principalSchema: "billing",
                        principalTable: "usage_metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "oauth_accounts",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    access_token_ref = table.Column<string>(type: "text", nullable: true),
                    refresh_token_ref = table.Column<string>(type: "text", nullable: true),
                    token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    raw_profile = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oauth_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_oauth_accounts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "UTC"),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "vi"),
                    theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "system"),
                    preferences_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "automation_execution_steps",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automation_execution_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_automation_execution_steps_automation_executions_execution_",
                        column: x => x.execution_id,
                        principalSchema: "automation",
                        principalTable: "automation_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_options",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_field_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_field_options_board_fields_field_id",
                        column: x => x.field_id,
                        principalSchema: "work",
                        principalTable: "board_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_items",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    position = table.Column<string>(type: "text", nullable: false),
                    parent_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_key = table.Column<string>(type: "text", nullable: true),
                    item_sequence = table.Column<long>(type: "bigint", nullable: true),
                    item_level = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_items_board_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "work",
                        principalTable: "board_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_board_items_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_item_connections",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sync_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_connections_board_relations_relation_id",
                        column: x => x.relation_id,
                        principalSchema: "work",
                        principalTable: "board_relations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_view_pins",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_view_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pin_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_view_pins", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_view_pins_board_views_board_view_id",
                        column: x => x.board_view_id,
                        principalSchema: "work",
                        principalTable: "board_views",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_board_view_pins_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_view_user_preferences",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    view_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_view_user_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_view_user_preferences_board_views_view_id",
                        column: x => x.view_id,
                        principalSchema: "work",
                        principalTable: "board_views",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_filters",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    view_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    group_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_filters", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_filters_board_views_view_id",
                        column: x => x.view_id,
                        principalSchema: "work",
                        principalTable: "board_views",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "board_item_labels",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_labels", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_labels_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_board_item_labels_labels_label_id",
                        column: x => x.label_id,
                        principalSchema: "work",
                        principalTable: "labels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "board_item_links",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    link_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_links_board_items_source_item_id",
                        column: x => x.source_item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_item_members",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_members_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_item_values",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_values_board_fields_field_id",
                        column: x => x.field_id,
                        principalSchema: "work",
                        principalTable: "board_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_board_item_values_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checklists",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklists", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklists_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_dependencies",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    predecessor_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    successor_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dependency_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lag_minutes = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_dependencies_board_items_predecessor_item_id",
                        column: x => x.predecessor_item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_dependencies_board_items_successor_item_id",
                        column: x => x.successor_item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "time_tracking_entries",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_tracking_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_tracking_entries_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_time_tracking_entries_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "work",
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mirror_value_snapshots",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mirrored_field_id = table.Column<Guid>(type: "uuid", nullable: true),
                    value_json = table.Column<string>(type: "text", nullable: true),
                    value_hash = table.Column<string>(type: "text", nullable: true),
                    is_stale = table.Column<bool>(type: "boolean", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mirror_value_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_mirror_value_snapshots_board_item_connections_connection_id",
                        column: x => x.connection_id,
                        principalSchema: "work",
                        principalTable: "board_item_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_view_filter_rules",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    preference_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_view_filter_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_view_filter_rules_board_view_user_preferences_prefere",
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preference_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_view_sort_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_view_sort_rules_board_view_user_preferences_preferenc",
                        column: x => x.preference_id,
                        principalSchema: "work",
                        principalTable: "board_view_user_preferences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_filter_rules",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    saved_filter_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_filter_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_filter_rules_saved_filters_saved_filter_id",
                        column: x => x.saved_filter_id,
                        principalSchema: "work",
                        principalTable: "saved_filters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_filter_sort_rules",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    saved_filter_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_filter_sort_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_filter_sort_rules_saved_filters_saved_filter_id",
                        column: x => x.saved_filter_id,
                        principalSchema: "work",
                        principalTable: "saved_filters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checklist_items",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assignee_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklist_items_checklists_checklist_id",
                        column: x => x.checklist_id,
                        principalSchema: "work",
                        principalTable: "checklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_grants_user_account_active",
                schema: "authz",
                table: "access_grants",
                columns: new[] { "user_id", "account_id" },
                filter: "\"membership_status\" = 'Active' AND \"revoked_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_access_grants_account_workspace_user",
                schema: "authz",
                table: "access_grants",
                columns: new[] { "account_id", "workspace_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_account_domains_domain",
                schema: "account",
                table: "account_domains",
                column: "domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_account_invitations_account_email",
                schema: "account",
                table: "account_invitations",
                columns: new[] { "account_id", "email" });

            migrationBuilder.CreateIndex(
                name: "idx_account_members_account_user",
                schema: "account",
                table: "account_members",
                columns: new[] { "account_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_account_regions_code",
                schema: "account",
                table: "account_regions",
                columns: new[] { "account_id", "region_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_account_settings_key",
                schema: "account",
                table: "account_settings",
                columns: new[] { "account_id", "setting_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_account_slug",
                schema: "account",
                table: "accounts",
                column: "slug");

            migrationBuilder.CreateIndex(
                name: "ix_activity_read_states_workspace_id_user_id",
                schema: "activity",
                table: "activity_read_states",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);

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
                name: "idx_approval_steps_request_id",
                schema: "work",
                table: "approval_steps",
                column: "approval_request_id");

            migrationBuilder.CreateIndex(
                name: "idx_attachments_resource",
                schema: "collab",
                table: "attachments",
                columns: new[] { "resource_type", "resource_id" });

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
                name: "idx_automation_rules_workspace_id",
                schema: "automation",
                table: "automation_rules",
                column: "workspace_id");

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
                name: "idx_billing_customers_provider",
                schema: "billing",
                table: "billing_customers",
                column: "provider_customer_id");

            migrationBuilder.CreateIndex(
                name: "ux_billing_customers_account_id",
                schema: "billing",
                table: "billing_customers",
                column: "account_id",
                unique: true);

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
                name: "idx_board_fields_board_position",
                schema: "work",
                table: "board_fields",
                columns: new[] { "board_id", "position" });

            migrationBuilder.CreateIndex(
                name: "idx_board_groups_board_position",
                schema: "work",
                table: "board_groups",
                columns: new[] { "board_id", "position" },
                filter: "deleted_at IS NULL");

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
                name: "idx_board_item_labels_item_label",
                schema: "work",
                table: "board_item_labels",
                columns: new[] { "item_id", "label_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_board_item_labels_label_id",
                schema: "work",
                table: "board_item_labels",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_links_source_item",
                schema: "work",
                table: "board_item_links",
                column: "source_item_id");

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
                name: "idx_board_item_values_item_field",
                schema: "work",
                table: "board_item_values",
                columns: new[] { "item_id", "field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_board_item_values_field_id",
                schema: "work",
                table: "board_item_values",
                column: "field_id");

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
                name: "idx_board_subscribers_board_user",
                schema: "work",
                table: "board_subscribers",
                columns: new[] { "board_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_board_view_filter_rules_preference_id",
                schema: "work",
                table: "board_view_filter_rules",
                column: "preference_id");

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
                name: "ix_board_view_sort_rules_preference_id",
                schema: "work",
                table: "board_view_sort_rules",
                column: "preference_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_view_user_prefs_view_user",
                schema: "work",
                table: "board_view_user_preferences",
                columns: new[] { "view_id", "user_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_views_board_id",
                schema: "work",
                table: "board_views",
                column: "board_id");

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
                name: "idx_calendar_integrations_workspace_id",
                schema: "integration",
                table: "calendar_integrations",
                column: "workspace_id");

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
                name: "idx_comments_resource",
                schema: "collab",
                table: "comments",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ix_comments_parent_id",
                schema: "collab",
                table: "comments",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_custom_role_permissions_role_id",
                schema: "governance",
                table: "custom_role_permissions",
                column: "custom_role_id");

            migrationBuilder.CreateIndex(
                name: "idx_custom_roles_workspace_id",
                schema: "governance",
                table: "custom_roles",
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
                name: "idx_dashboard_widgets_dashboard_id",
                schema: "reporting",
                table: "dashboard_widgets",
                column: "dashboard_id");

            migrationBuilder.CreateIndex(
                name: "idx_dashboards_workspace_id",
                schema: "reporting",
                table: "dashboards",
                column: "workspace_id");

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
                name: "idx_email_verification_tokens_expires",
                schema: "identity",
                table: "email_verification_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ux_email_verification_tokens_one_active_per_user",
                schema: "identity",
                table: "email_verification_tokens",
                column: "user_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "idx_entitlements_account_id",
                schema: "billing",
                table: "entitlements",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "idx_entitlements_target",
                schema: "billing",
                table: "entitlements",
                columns: new[] { "target_scope", "target_workspace_id" });

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_status",
                schema: "ops",
                table: "export_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_workspace_id",
                schema: "ops",
                table: "export_jobs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "ix_feature_usage_daily_feature_code_usage_date",
                schema: "analytics",
                table: "feature_usage_daily",
                columns: new[] { "feature_code", "usage_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_feature_usage_ledger_workspace_id",
                schema: "billing",
                table: "feature_usage_ledger",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_field_options_field_id",
                schema: "work",
                table: "field_options",
                column: "field_id");

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
                name: "ix_form_submissions_board_id",
                schema: "work",
                table: "form_submissions",
                column: "board_id");

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
                name: "ix_idempotency_keys_expires_at",
                schema: "ops",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_scope_key",
                schema: "ops",
                table: "idempotency_keys",
                columns: new[] { "scope", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_status",
                schema: "ops",
                table: "import_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_workspace_id",
                schema: "ops",
                table: "import_jobs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbound_webhook_events_workspace_id",
                schema: "integration",
                table: "inbound_webhook_events",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_integration_connections_workspace_id",
                schema: "integration",
                table: "integration_connections",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_integration_scopes_connection_id",
                schema: "integration",
                table: "integration_scopes",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "idx_integration_secret_versions_connection_id",
                schema: "integration",
                table: "integration_secret_versions",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "idx_integration_sync_cursors_connection_resource",
                schema: "integration",
                table: "integration_sync_cursors",
                columns: new[] { "connection_id", "resource_type" });

            migrationBuilder.CreateIndex(
                name: "idx_invoice_line_items_invoice_id",
                schema: "billing",
                table: "invoice_line_items",
                column: "invoice_id");

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
                name: "idx_item_templates_board_name",
                schema: "work",
                table: "item_templates",
                columns: new[] { "board_id", "name" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_job_locks_lock_key",
                schema: "ops",
                table: "job_locks",
                column: "lock_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_locks_locked_until",
                schema: "ops",
                table: "job_locks",
                column: "locked_until");

            migrationBuilder.CreateIndex(
                name: "idx_labels_board_id",
                schema: "work",
                table: "labels",
                column: "board_id");

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
                name: "ix_notification_counters_user_workspace",
                schema: "notifications",
                table: "notification_counters",
                columns: new[] { "user_id", "workspace_id" });

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
                name: "idx_oauth_accounts_provider",
                schema: "identity",
                table: "oauth_accounts",
                columns: new[] { "provider", "provider_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_oauth_accounts_user_id",
                schema: "identity",
                table: "oauth_accounts",
                column: "user_id");

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
                filter: "workspace_id IS NOT NULL");

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
                name: "idx_password_reset_tokens_expires",
                schema: "identity",
                table: "password_reset_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_password_reset_tokens_user_id",
                schema: "identity",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_payment_methods_workspace_id",
                schema: "billing",
                table: "payment_methods",
                column: "workspace_id");

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
                name: "idx_permission_templates_name",
                schema: "governance",
                table: "permission_templates",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_permission_templates_workspace_id",
                schema: "governance",
                table: "permission_templates",
                column: "workspace_id",
                filter: "workspace_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_plan_limits_plan_id",
                schema: "billing",
                table: "plan_limits",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ux_plan_prices_plan_currency_interval",
                schema: "billing",
                table: "plan_prices",
                columns: new[] { "plan_id", "currency", "billing_interval" },
                unique: true);

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
                name: "ix_processed_events_consumer_name_claimed_at",
                schema: "messaging",
                table: "processed_events",
                columns: new[] { "consumer_name", "claimed_at" },
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
                name: "ix_processed_events_message_name_claimed_at",
                schema: "messaging",
                table: "processed_events",
                columns: new[] { "message_name", "claimed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_workspace_id_claimed_at",
                schema: "messaging",
                table: "processed_events",
                columns: new[] { "workspace_id", "claimed_at" },
                descending: new[] { false, true },
                filter: "\"workspace_id\" IS NOT NULL");

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
                name: "idx_relation_field_configs_field_id",
                schema: "work",
                table: "relation_field_configs",
                column: "field_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_reporting_snapshots_workspace_id",
                schema: "reporting",
                table: "reporting_snapshots",
                column: "workspace_id");

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
                name: "ix_governance_permission_inheritance_cache_lookup",
                schema: "governance",
                table: "resource_permission_inheritance_cache",
                columns: new[] { "workspace_id", "subject_type", "subject_id", "resource_type", "resource_id", "action" });

            migrationBuilder.CreateIndex(
                name: "ux_governance_permission_inheritance_cache",
                schema: "governance",
                table: "resource_permission_inheritance_cache",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "subject_type", "subject_id", "subject_key", "action" },
                unique: true);

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
                name: "ix_collab_resource_read_states_user",
                schema: "collab",
                table: "resource_read_states",
                columns: new[] { "workspace_id", "user_id", "unread_count", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ux_collab_resource_read_states_user_resource",
                schema: "collab",
                table: "resource_read_states",
                columns: new[] { "workspace_id", "user_id", "resource_type", "resource_id" },
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
                name: "ix_saved_filter_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_rules",
                column: "saved_filter_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_filter_sort_rules_saved_filter_id",
                schema: "work",
                table: "saved_filter_sort_rules",
                column: "saved_filter_id");

            migrationBuilder.CreateIndex(
                name: "idx_saved_filters_board_name",
                schema: "work",
                table: "saved_filters",
                columns: new[] { "board_id", "name" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_saved_filters_view_id",
                schema: "work",
                table: "saved_filters",
                column: "view_id");

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
                name: "idx_scim_directories_account_name",
                schema: "account",
                table: "scim_directories",
                columns: new[] { "account_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_search_documents_account_workspace_type",
                schema: "search",
                table: "search_documents",
                columns: new[] { "account_id", "workspace_id", "resource_type" });

            migrationBuilder.CreateIndex(
                name: "ix_search_documents_search_vector",
                schema: "search",
                table: "search_documents",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ux_search_documents_resource",
                schema: "search",
                table: "search_documents",
                columns: new[] { "account_id", "workspace_id", "resource_type", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_search_index_jobs_locks",
                schema: "search",
                table: "search_index_jobs",
                column: "locked_until");

            migrationBuilder.CreateIndex(
                name: "ix_search_index_jobs_pending",
                schema: "search",
                table: "search_index_jobs",
                columns: new[] { "status", "priority", "available_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_search_index_jobs_resource",
                schema: "search",
                table: "search_index_jobs",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "created_at" },
                descending: new[] { false, false, false, true });

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
                name: "idx_share_links_resource_id",
                schema: "governance",
                table: "share_links",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "idx_spaces_workspace_id",
                schema: "workspace",
                table: "spaces",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_subscription_items_plan_price_id",
                schema: "billing",
                table: "subscription_items",
                column: "plan_price_id");

            migrationBuilder.CreateIndex(
                name: "idx_subscription_items_subscription_id",
                schema: "billing",
                table: "subscription_items",
                column: "subscription_id");

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
                name: "idx_time_tracking_item_user",
                schema: "work",
                table: "time_tracking_entries",
                columns: new[] { "item_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "idx_time_tracking_status",
                schema: "work",
                table: "time_tracking_entries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_time_tracking_entries_board_id",
                schema: "work",
                table: "time_tracking_entries",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "idx_usage_metric_history_metric_id",
                schema: "billing",
                table: "usage_metric_history",
                column: "metric_id");

            migrationBuilder.CreateIndex(
                name: "idx_usage_metrics_workspace_id",
                schema: "billing",
                table: "usage_metrics",
                column: "workspace_id");

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
                name: "idx_user_mfa_methods_user_id",
                schema: "identity",
                table: "user_mfa_methods",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_profiles_user_id",
                schema: "identity",
                table: "user_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_security_settings_user_id",
                schema: "identity",
                table: "user_security_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_sessions_expires",
                schema: "identity",
                table: "user_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_sessions_user_id",
                schema: "identity",
                table: "user_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_email",
                schema: "identity",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "deleted_at IS NULL");

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
                name: "idx_webhook_subscriptions_workspace_id",
                schema: "integration",
                table: "webhook_subscriptions",
                column: "workspace_id");

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
                name: "idx_workspace_feature_usages_workspace_id",
                schema: "billing",
                table: "workspace_feature_usages",
                column: "workspace_id");

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
                name: "idx_workspace_policies_workspace_id",
                schema: "governance",
                table: "workspace_policies",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_workspace_routes_account_slug",
                schema: "account",
                table: "workspace_routes",
                columns: new[] { "account_id", "route_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_workspace_routes_is_deleted",
                schema: "account",
                table: "workspace_routes",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_usage_daily_usage_date",
                schema: "analytics",
                table: "workspace_usage_daily",
                column: "usage_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_workspaces_name",
                schema: "workspace",
                table: "workspaces",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_workspaces_personal_per_account",
                schema: "workspace",
                table: "workspaces",
                column: "account_id",
                unique: true,
                filter: "is_personal = true AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_workspaces_account_slug_active",
                schema: "workspace",
                table: "workspaces",
                columns: new[] { "account_id", "slug" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_grants",
                schema: "authz");

            migrationBuilder.DropTable(
                name: "account_domains",
                schema: "account");

            migrationBuilder.DropTable(
                name: "account_identity_providers",
                schema: "account");

            migrationBuilder.DropTable(
                name: "account_invitations",
                schema: "account");

            migrationBuilder.DropTable(
                name: "account_members",
                schema: "account");

            migrationBuilder.DropTable(
                name: "account_regions",
                schema: "account");

            migrationBuilder.DropTable(
                name: "account_settings",
                schema: "account");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "account");

            migrationBuilder.DropTable(
                name: "activity_read_states",
                schema: "activity");

            migrationBuilder.DropTable(
                name: "ai_agent_runs",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "ai_agents",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "api_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "approval_steps",
                schema: "work");

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "automation_execution_steps",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "automation_templates",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "billing_customers",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "billing_events",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "blocks",
                schema: "docs");

            migrationBuilder.DropTable(
                name: "board_item_labels",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_item_links",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_item_members",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_item_values",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_members",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_subscribers",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_templates",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_view_filter_rules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_view_pins",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_view_sort_rules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "calendar_event_links",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "calendar_events",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "checklist_items",
                schema: "work");

            migrationBuilder.DropTable(
                name: "comments",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "custom_role_permissions",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "dashboard_sources",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "dashboard_widgets",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "document_versions",
                schema: "docs");

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
                name: "email_verification_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "entitlements",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "export_jobs",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "feature_usage_daily",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "feature_usage_ledger",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "field_options",
                schema: "work");

            migrationBuilder.DropTable(
                name: "field_permissions",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "form_questions",
                schema: "work");

            migrationBuilder.DropTable(
                name: "form_submissions",
                schema: "work");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "import_jobs",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "inbound_webhook_events",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "integration_scopes",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "integration_secret_versions",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "integration_sync_cursors",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "invoice_line_items",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "item_dependencies",
                schema: "work");

            migrationBuilder.DropTable(
                name: "item_templates",
                schema: "work");

            migrationBuilder.DropTable(
                name: "job_locks",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "member_role_assignments",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "mentions",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "mirror_value_snapshots",
                schema: "work");

            migrationBuilder.DropTable(
                name: "notification_counters",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notification_preferences",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notification_recipients",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "oauth_accounts",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "outbox_delivery_attempts",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "page_templates",
                schema: "docs");

            migrationBuilder.DropTable(
                name: "password_reset_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "permission_rules",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "permission_templates",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "plan_limits",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "plan_prices",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "presence_sessions",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "processed_events",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "reactions",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "relation_field_configs",
                schema: "work");

            migrationBuilder.DropTable(
                name: "reporting_snapshots",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "resource_links",
                schema: "docs");

            migrationBuilder.DropTable(
                name: "resource_permission_inheritance_cache",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "resource_permissions",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "resource_read_states",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "resource_watchers",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "saved_filter_rules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "saved_filter_sort_rules",
                schema: "work");

            migrationBuilder.DropTable(
                name: "scheduled_jobs",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "scim_directories",
                schema: "account");

            migrationBuilder.DropTable(
                name: "scim_sync_runs",
                schema: "account");

            migrationBuilder.DropTable(
                name: "search_documents",
                schema: "search");

            migrationBuilder.DropTable(
                name: "search_index_jobs",
                schema: "search");

            migrationBuilder.DropTable(
                name: "security_events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "share_links",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "spaces",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "subscription_items",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "team_members",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "time_tracking_entries",
                schema: "work");

            migrationBuilder.DropTable(
                name: "usage_metric_history",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "user_login_attempts",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_mfa_methods",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_security_settings",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "webhook_deliveries",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "webhook_subscriptions",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "workload_allocations",
                schema: "work");

            migrationBuilder.DropTable(
                name: "workspace_activity_logs",
                schema: "activity");

            migrationBuilder.DropTable(
                name: "workspace_feature_usages",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "workspace_invitations",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "workspace_members",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "workspace_policies",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "workspace_routes",
                schema: "account");

            migrationBuilder.DropTable(
                name: "workspace_usage_daily",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "workspaces",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "approval_requests",
                schema: "work");

            migrationBuilder.DropTable(
                name: "automation_executions",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "docs");

            migrationBuilder.DropTable(
                name: "labels",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_view_user_preferences",
                schema: "work");

            migrationBuilder.DropTable(
                name: "calendar_integrations",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "checklists",
                schema: "work");

            migrationBuilder.DropTable(
                name: "custom_roles",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "dashboards",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "board_fields",
                schema: "work");

            migrationBuilder.DropTable(
                name: "forms",
                schema: "work");

            migrationBuilder.DropTable(
                name: "integration_connections",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "board_item_connections",
                schema: "work");

            migrationBuilder.DropTable(
                name: "notification_items",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "plans",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "saved_filters",
                schema: "work");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "usage_metrics",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "automation_rules",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "board_items",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_relations",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_views",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_groups",
                schema: "work");

            migrationBuilder.DropTable(
                name: "boards",
                schema: "work");
        }
    }
}

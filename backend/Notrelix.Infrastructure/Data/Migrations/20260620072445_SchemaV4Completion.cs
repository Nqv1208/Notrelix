using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SchemaV4Completion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "search");

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
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    format = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Csv"),
                    row_count = table.Column<int>(type: "integer", nullable: true),
                    options_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    filters_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    result_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    storage_key = table.Column<string>(type: "text", nullable: true),
                    download_url = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_export_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    request_path = table.Column<string>(type: "text", nullable: false),
                    request_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Started"),
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
                    table.PrimaryKey("PK_idempotency_keys", x => x.id);
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
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    total_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    processed_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    succeeded_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    options_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
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
                    table.PrimaryKey("PK_import_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_locks",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lock_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    locked_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    fencing_token = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    acquired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    renewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_locks", x => x.id);
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
                    table.PrimaryKey("PK_resource_permission_inheritance_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "search_documents",
                schema: "search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    search_vector = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_documents", x => x.id);
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
                    table.PrimaryKey("PK_search_index_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unread_counters",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counter_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "Notification"),
                    counter_value = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unread_counters", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ops_export_jobs_workspace_status",
                schema: "ops",
                table: "export_jobs",
                columns: new[] { "workspace_id", "status", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ops_idempotency_keys_expires_at",
                schema: "ops",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_ops_idempotency_keys_workspace_status",
                schema: "ops",
                table: "idempotency_keys",
                columns: new[] { "workspace_id", "status", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_ops_idempotency_keys_scope_key",
                schema: "ops",
                table: "idempotency_keys",
                columns: new[] { "scope", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_import_jobs_workspace_status",
                schema: "ops",
                table: "import_jobs",
                columns: new[] { "workspace_id", "status", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ops_job_locks_lock_key",
                schema: "ops",
                table: "job_locks",
                column: "lock_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_job_locks_locked_until",
                schema: "ops",
                table: "job_locks",
                column: "locked_until");

            migrationBuilder.CreateIndex(
                name: "ix_ops_job_locks_owner",
                schema: "ops",
                table: "job_locks",
                columns: new[] { "locked_by", "locked_until" });

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
                name: "ix_search_documents_workspace_type",
                schema: "search",
                table: "search_documents",
                columns: new[] { "workspace_id", "resource_type" });

            migrationBuilder.CreateIndex(
                name: "ux_search_documents_resource",
                schema: "search",
                table: "search_documents",
                columns: new[] { "workspace_id", "resource_type", "resource_id" },
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
                name: "ux_collab_unread_counters_user_type",
                schema: "collab",
                table: "unread_counters",
                columns: new[] { "workspace_id", "user_id", "counter_type" },
                unique: true);

            // GIN index on search_documents.tags for efficient array lookups
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_search_documents_tags
                ON search.search_documents USING gin(tags);
            """);

            // updated_at triggers for new tables
            // The ops.set_updated_at() function is created by earlier migrations or manual setup;
            // ensure it exists for environments that skip it
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION ops.set_updated_at()
                RETURNS trigger AS $$
                BEGIN
                    NEW.updated_at = now();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            """);

            // updated_at triggers for new tables
            var triggerConfigs = new[] { ("ops", "export_jobs"), ("ops", "import_jobs"), ("ops", "job_locks"), ("search", "search_documents"), ("search", "search_index_jobs") };
            foreach (var (schema, table) in triggerConfigs)
            {
                var triggerName = $"trg_{schema}_{table}_updated_at";
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {triggerName} ON {schema}.{table};");
                migrationBuilder.Sql($"CREATE TRIGGER {triggerName} BEFORE UPDATE ON {schema}.{table} FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_jobs",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "import_jobs",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "job_locks",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "resource_permission_inheritance_cache",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "search_documents",
                schema: "search");

            migrationBuilder.DropTable(
                name: "search_index_jobs",
                schema: "search");

            migrationBuilder.DropTable(
                name: "unread_counters",
                schema: "collab");

            // Drop triggers for new tables
            var dropTriggers = new[] { ("ops", "export_jobs"), ("ops", "import_jobs"), ("ops", "job_locks"), ("search", "search_documents"), ("search", "search_index_jobs") };
            foreach (var (schema, table) in dropTriggers)
            {
                var triggerName = $"trg_{schema}_{table}_updated_at";
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {triggerName} ON {schema}.{table};");
            }
        }
    }
}

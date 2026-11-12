using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRetentionAndExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "export_jobs",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    range_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    range_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    message_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    space_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    excluded_space_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tombstone_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "retention_policies",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: true),
                    mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_policies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_status_created",
                schema: "chat",
                table: "export_jobs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_workspace_requester",
                schema: "chat",
                table: "export_jobs",
                columns: new[] { "workspace_id", "requested_by", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_retention_policies_enabled",
                schema: "chat",
                table: "retention_policies",
                column: "retention_days",
                filter: "retention_days IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_retention_policies_workspace",
                schema: "chat",
                table: "retention_policies",
                column: "workspace_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_jobs",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "retention_policies",
                schema: "chat");
        }
    }
}

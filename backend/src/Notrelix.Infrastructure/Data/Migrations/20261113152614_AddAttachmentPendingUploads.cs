#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentPendingUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attachment_pending_uploads",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    declared_size = table.Column<long>(type: "bigint", nullable: false),
                    ceiling_bytes = table.Column<long>(type: "bigint", nullable: false),
                    declared_content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    voice = table.Column<bool>(type: "boolean", nullable: false),
                    duration_hint_ms = table.Column<int>(type: "integer", nullable: true),
                    waveform = table.Column<byte[]>(type: "bytea", maxLength: 64, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    committed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachment_pending_uploads", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_attachment_pending_uploads_resource",
                schema: "collab",
                table: "attachment_pending_uploads",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ix_attachment_pending_uploads_expiry",
                schema: "collab",
                table: "attachment_pending_uploads",
                column: "expires_at",
                filter: "attachment_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_attachment_pending_uploads_storage_key",
                schema: "collab",
                table: "attachment_pending_uploads",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachment_pending_uploads",
                schema: "collab");
        }
    }
}

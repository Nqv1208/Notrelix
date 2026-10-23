#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentObjectPurge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "purged_at",
                schema: "collab",
                table: "attachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_attachments_purge_pending",
                schema: "collab",
                table: "attachments",
                column: "deleted_at",
                filter: "deleted_at IS NOT NULL AND purged_at IS NULL AND storage_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_attachments_purge_pending",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "purged_at",
                schema: "collab",
                table: "attachments");
        }
    }
}

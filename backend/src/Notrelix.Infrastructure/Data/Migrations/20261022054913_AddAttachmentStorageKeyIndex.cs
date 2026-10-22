#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentStorageKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_attachments_storage_key",
                schema: "collab",
                table: "attachments",
                column: "storage_key",
                unique: true,
                filter: "storage_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_attachments_storage_key",
                schema: "collab",
                table: "attachments");
        }
    }
}

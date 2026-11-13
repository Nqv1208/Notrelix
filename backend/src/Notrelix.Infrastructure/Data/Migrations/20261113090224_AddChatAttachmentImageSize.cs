#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatAttachmentImageSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "image_height",
                schema: "collab",
                table: "attachments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "image_width",
                schema: "collab",
                table: "attachments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_height",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "image_width",
                schema: "collab",
                table: "attachments");
        }
    }
}

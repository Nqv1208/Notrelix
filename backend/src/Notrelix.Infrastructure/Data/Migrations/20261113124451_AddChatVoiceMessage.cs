#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatVoiceMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "audio_duration_ms",
                schema: "collab",
                table: "attachments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "audio_waveform",
                schema: "collab",
                table: "attachments",
                type: "bytea",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "audio_duration_ms",
                schema: "collab",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "audio_waveform",
                schema: "collab",
                table: "attachments");
        }
    }
}

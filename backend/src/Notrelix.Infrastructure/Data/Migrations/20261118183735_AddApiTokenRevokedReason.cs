
#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiTokenRevokedReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                schema: "identity",
                table: "api_tokens",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "revoked_reason",
                schema: "identity",
                table: "api_tokens");
        }
    }
}

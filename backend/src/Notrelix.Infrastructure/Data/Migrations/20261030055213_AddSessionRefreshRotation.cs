#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRefreshRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "previous_refresh_token_hash",
                schema: "identity",
                table: "user_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "rotated_at",
                schema: "identity",
                table: "user_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_sessions_previous_refresh_token_hash",
                schema: "identity",
                table: "user_sessions",
                column: "previous_refresh_token_hash");

            migrationBuilder.CreateIndex(
                name: "idx_user_sessions_refresh_token_hash",
                schema: "identity",
                table: "user_sessions",
                column: "refresh_token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_user_sessions_previous_refresh_token_hash",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropIndex(
                name: "idx_user_sessions_refresh_token_hash",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "previous_refresh_token_hash",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "rotated_at",
                schema: "identity",
                table: "user_sessions");
        }
    }
}

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationConnectionDeliveryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_error_at",
                schema: "integration",
                table: "integration_connections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_success_at",
                schema: "integration",
                table: "integration_connections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_url",
                schema: "integration",
                table: "integration_connections",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_error_at",
                schema: "integration",
                table: "integration_connections");

            migrationBuilder.DropColumn(
                name: "last_success_at",
                schema: "integration",
                table: "integration_connections");

            migrationBuilder.DropColumn(
                name: "target_url",
                schema: "integration",
                table: "integration_connections");
        }
    }
}

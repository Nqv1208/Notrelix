#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class M8IntegrationsSecretBlobsAndWebhookReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_secret_blobs",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encrypted_payload = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_secret_blobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_integration_secret_blobs_revoked",
                schema: "integration",
                table: "integration_secret_blobs",
                column: "revoked");

            migrationBuilder.CreateTable(
                name: "inbound_webhook_receipts",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_event_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbound_webhook_receipts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_inbound_webhook_receipts_provider_external_event_id",
                schema: "integration",
                table: "inbound_webhook_receipts",
                columns: new[] { "provider", "external_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inbound_webhook_receipts_received_at",
                schema: "integration",
                table: "inbound_webhook_receipts",
                column: "received_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbound_webhook_receipts",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "integration_secret_blobs",
                schema: "integration");
        }
    }
}

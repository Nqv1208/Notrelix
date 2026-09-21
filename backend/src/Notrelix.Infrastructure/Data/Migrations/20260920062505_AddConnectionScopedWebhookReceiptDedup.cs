#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectionScopedWebhookReceiptDedup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TAC-AI-FLOW-07 Wave C ConnectionId closure: the former dedup key
            // (provider, external_event_id) was provider-wide, but the provider
            // event id only namespaces a delivery within a provider
            // calendar/connection — BE-API-041 only permits provider-event-id
            // dedup where the provider contract defines that uniqueness, and no
            // repository contract defines provider-wide ExternalEventId
            // uniqueness. A provider-wide index could wrongly classify a
            // distinct calendar's event as a duplicate and drop it. Dedup is
            // now connection-scoped: UNIQUE(connection_id, provider,
            // external_event_id).
            //
            // ConnectionId is the trusted provenance/binding identity resolved
            // from WebhookPath → CalendarIntegration → IntegrationConnection
            // (never from the payload) and is required for every new accepted
            // receipt. Legacy rows carry no reliable connection mapping (the
            // event id alone cannot be deterministically attributed to one
            // connection) and belong to the pre-migration provider-wide dedup
            // domain, so connection_id stays NULL for them; rejected callbacks
            // with no trusted binding also keep NULL. PostgreSQL treats NULL as
            // distinct in unique indexes, so legacy NULL rows never collide
            // with the connection-scoped claims of new accepted receipts.
            migrationBuilder.DropIndex(
                name: "ux_inbound_webhook_receipts_provider_external_event_id",
                schema: "integration",
                table: "inbound_webhook_receipts");

            migrationBuilder.AddColumn<Guid>(
                name: "connection_id",
                schema: "integration",
                table: "inbound_webhook_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_inbound_webhook_receipts_connection_provider_external_event_id",
                schema: "integration",
                table: "inbound_webhook_receipts",
                columns: new[] { "connection_id", "provider", "external_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting restores the provider-wide dedup domain. ConnectionId
            // is dropped with the data — intentional: a down-migration cannot
            // restore the connection provenance columns it never had.
            migrationBuilder.DropIndex(
                name: "ux_inbound_webhook_receipts_connection_provider_external_event_id",
                schema: "integration",
                table: "inbound_webhook_receipts");

            migrationBuilder.DropColumn(
                name: "connection_id",
                schema: "integration",
                table: "inbound_webhook_receipts");

            migrationBuilder.CreateIndex(
                name: "ux_inbound_webhook_receipts_provider_external_event_id",
                schema: "integration",
                table: "inbound_webhook_receipts",
                columns: new[] { "provider", "external_event_id" },
                unique: true);
        }
    }
}

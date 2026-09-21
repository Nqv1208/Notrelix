using Notrelix.Infrastructure.Data.Integrations;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

/// <summary>
/// M8 AI-FLOW-07 — technical webhook receipt persistence. Infrastructure
/// state in the integrations schema; unique (connection_id, provider,
/// external event id) enables delivery-level dedup. ConnectionId is the
/// trusted provenance/binding identity resolved from the WebhookPath
/// bootstrap and is required for new accepted receipts; legacy provider-wide
/// rows and rejected callbacks keep it NULL. Dedup is connection-scoped
/// because the provider event id only namespaces a delivery within a provider
/// calendar/connection (BE-API-041 provider-contract scope).
/// </summary>
public class InboundWebhookReceiptConfiguration : IEntityTypeConfiguration<InboundWebhookReceipt>
{
    public void Configure(EntityTypeBuilder<InboundWebhookReceipt> builder)
    {
        builder.ToTable("inbound_webhook_receipts", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ConnectionId).HasColumnName("connection_id");
        builder.Property(x => x.Provider).HasColumnName("provider").IsRequired().HasMaxLength(50);
        builder.Property(x => x.ExternalEventId).HasColumnName("external_event_id").IsRequired().HasMaxLength(256);
        builder.Property(x => x.PayloadHash).HasColumnName("payload_hash").IsRequired().HasMaxLength(128);
        builder.Property(x => x.ProtectedPayload).HasColumnName("protected_payload");
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at");
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason");

        builder.HasIndex(x => new { x.ConnectionId, x.Provider, x.ExternalEventId })
            .IsUnique()
            .HasDatabaseName("ux_inbound_webhook_receipts_connection_provider_external_event_id");
        builder.HasIndex(x => x.ReceivedAt).HasDatabaseName("idx_inbound_webhook_receipts_received_at");
    }
}

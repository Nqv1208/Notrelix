using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Integrations.Webhooks.Events;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class InboundWebhookEventConfiguration : IEntityTypeConfiguration<InboundWebhookEvent>
{
    public void Configure(EntityTypeBuilder<InboundWebhookEvent> builder)
    {
        builder.ToTable("inbound_webhook_events", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.Provider).HasColumnName("provider").IsRequired().HasMaxLength(50);
        builder.Property(x => x.ExternalEventId).HasColumnName("external_event_id").HasMaxLength(256);
        builder.Property(x => x.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").HasConversion<JsonValueConverter>().IsRequired();
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").IsRequired();

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_inbound_webhook_events_workspace_id");
    }
}

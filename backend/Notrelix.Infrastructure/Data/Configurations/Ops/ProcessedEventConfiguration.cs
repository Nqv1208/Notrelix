using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.Data.Configurations.Ops;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events", DbSchemas.Ops);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.ConsumerName).HasColumnName("consumer_name").IsRequired().HasMaxLength(128);
        builder.Property(x => x.MessageName).HasColumnName("message_name").IsRequired().HasMaxLength(512);
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").IsRequired().HasDefaultValue(1);
        builder.Property(x => x.SourceEventId).HasColumnName("source_event_id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at").IsRequired();

        builder.HasIndex(x => new { x.EventId, x.ConsumerName }).IsUnique().HasDatabaseName("idx_processed_events_event_id_consumer");
        builder.HasIndex(x => x.ProcessedAt).HasDatabaseName("idx_processed_events_processed_at");
    }
}

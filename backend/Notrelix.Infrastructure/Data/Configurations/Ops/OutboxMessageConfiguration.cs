using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.Data.Configurations.Ops;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", DbSchemas.Ops);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(512);
        builder.Property(x => x.EventVersion).HasColumnName("event_version").IsRequired().HasDefaultValue(1);
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        builder.Property(x => x.CausationId).HasColumnName("causation_id").HasMaxLength(128);
        builder.Property(x => x.PayloadJson).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(50).HasDefaultValue(OutboxStatus.Pending);
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MaxRetries).HasColumnName("max_retries").IsRequired().HasDefaultValue(OutboxDefaults.MaxRetries);
        builder.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.Error).HasColumnName("error");

        builder.HasIndex(x => new { x.Status, x.NextAttemptAt }).HasDatabaseName("idx_outbox_messages_pending");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_outbox_messages_created_at");
    }
}

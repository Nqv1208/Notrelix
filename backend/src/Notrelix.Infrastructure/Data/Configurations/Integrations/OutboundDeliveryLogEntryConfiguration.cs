using Notrelix.Domain.Integrations.Webhooks;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class OutboundDeliveryLogEntryConfiguration : IEntityTypeConfiguration<OutboundDeliveryLogEntry>
{
    public void Configure(EntityTypeBuilder<OutboundDeliveryLogEntry> builder)
    {
        builder.ToTable("outbound_delivery_log_entries", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ConnectionId).HasColumnName("connection_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(x => x.ResponseStatusCode).HasColumnName("response_status_code");
        builder.Property(x => x.FailureClass).HasColumnName("failure_class").HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.SkipReason).HasColumnName("skip_reason").HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at");

        builder.HasIndex(x => new { x.WorkspaceId, x.ConnectionId, x.CreatedAt, x.Id })
            .HasDatabaseName("idx_outbound_delivery_log_entries_connection_history");
    }
}

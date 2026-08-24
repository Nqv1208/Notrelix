using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Data.Configurations.Messaging;

public sealed class MessagingOutboxMessageConfiguration : IEntityTypeConfiguration<MessagingOutboxMessage>
{
    public void Configure(EntityTypeBuilder<MessagingOutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", DbSchemas.Messaging);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventId).IsRequired();
        builder.HasIndex(x => x.EventId).IsUnique();

        builder.Property(x => x.SourceEventId);
        builder.Property(x => x.AccountId);
        builder.Property(x => x.SourceContext).IsRequired().HasMaxLength(80);
        builder.Property(x => x.MessageName).IsRequired().HasMaxLength(240);
        builder.Property(x => x.SchemaVersion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Destination).HasMaxLength(240);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(120).HasDefaultValue("application/json");
        builder.Property(x => x.SubjectType).HasMaxLength(160);
        builder.Property(x => x.AggregateType).HasMaxLength(160);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.CausationId).HasMaxLength(100);
        builder.Property(x => x.PartitionKey).HasMaxLength(240);
        builder.Property(x => x.ResourceKind).HasMaxLength(160);
        builder.Property(x => x.StreamKey).HasMaxLength(320);
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").HasConversion<string>().IsRequired();
        builder.Property(x => x.HeadersJson).HasColumnType("jsonb").HasConversion<string>().IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").HasConversion<string>().IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.Status).IsRequired().HasMaxLength(40).HasDefaultValue("Pending");
        builder.Property(x => x.RetryCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MaxRetries).IsRequired().HasDefaultValue(5);
        builder.Property(x => x.NextAttemptAt).IsRequired();
        builder.Property(x => x.LockedBy).HasMaxLength(160);
        builder.Property(x => x.LockId).HasColumnName("lock_id");
        builder.Property(x => x.LastErrorCode).HasMaxLength(120);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => new { x.Status, x.NextAttemptAt, x.CreatedAt })
            .HasFilter("\"status\" IN ('Pending', 'Failed')");
        builder.HasIndex(x => new { x.Status, x.LockedUntil, x.ProcessingStartedAt })
            .HasFilter("\"status\" = 'Processing'");
        builder.HasIndex(x => new { x.SourceContext, x.MessageName, x.CreatedAt }).IsDescending(false, false, true);
        builder.HasIndex(x => new { x.WorkspaceId, x.CreatedAt })
            .HasFilter("\"workspace_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.SubjectType, x.SubjectId, x.CreatedAt })
            .HasFilter("\"subject_type\" IS NOT NULL AND \"subject_id\" IS NOT NULL")
            .IsDescending(false, false, true);
        builder.HasIndex(x => x.CorrelationId)
            .HasFilter("\"correlation_id\" IS NOT NULL");
        builder.HasIndex(x => new { x.PartitionKey, x.CreatedAt })
            .HasFilter("\"partition_key\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.StreamKey, x.StreamVersion })
            .IsUnique()
            .HasFilter("\"stream_key\" IS NOT NULL AND \"stream_version\" IS NOT NULL");
        builder.HasIndex(x => new { x.StreamKey, x.StreamVersion, x.Status })
            .HasFilter("\"stream_key\" IS NOT NULL");
    }
}

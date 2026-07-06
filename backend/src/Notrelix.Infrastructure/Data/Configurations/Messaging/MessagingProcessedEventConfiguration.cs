using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Data.Configurations.Messaging;

public sealed class MessagingProcessedEventConfiguration : IEntityTypeConfiguration<MessagingProcessedEvent>
{
    public void Configure(EntityTypeBuilder<MessagingProcessedEvent> builder)
    {
        builder.ToTable("processed_events", DbSchemas.Messaging);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.ConsumerName).IsRequired().HasMaxLength(240);
        builder.HasIndex(x => new { x.EventId, x.ConsumerName }).IsUnique();

        builder.Property(x => x.SourceContext).HasMaxLength(80);
        builder.Property(x => x.MessageName).IsRequired().HasMaxLength(240);
        builder.Property(x => x.MessageVersion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.SourceEventId);
        builder.Property(x => x.SubjectType).HasMaxLength(160);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.CausationId).HasMaxLength(100);
        builder.Property(x => x.ProcessedAt).IsRequired();
        builder.Property(x => x.Result).IsRequired().HasMaxLength(40).HasDefaultValue("Succeeded");
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").HasConversion<string>().IsRequired().HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(x => new { x.ConsumerName, x.ProcessedAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.WorkspaceId, x.ProcessedAt })
            .HasFilter("\"workspace_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.MessageName, x.ProcessedAt }).IsDescending(false, true);
        builder.HasIndex(x => x.CorrelationId)
            .HasFilter("\"correlation_id\" IS NOT NULL");
    }
}

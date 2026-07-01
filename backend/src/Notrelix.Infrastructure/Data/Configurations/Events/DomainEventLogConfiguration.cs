using Notrelix.Infrastructure.Data.Events;

namespace Notrelix.Infrastructure.Data.Configurations.Events;

public sealed class DomainEventLogConfiguration : IEntityTypeConfiguration<DomainEventLog>
{
    public void Configure(EntityTypeBuilder<DomainEventLog> builder)
    {
        builder.ToTable("domain_event_logs", DbSchemas.Events);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventId).IsRequired();
        builder.HasIndex(x => x.EventId).IsUnique();

        builder.Property(x => x.SourceContext).IsRequired().HasMaxLength(80);
        builder.Property(x => x.EventName).IsRequired().HasMaxLength(240);
        builder.Property(x => x.EventVersion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.AggregateType).HasMaxLength(160);
        builder.Property(x => x.SubjectType).HasMaxLength(160);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.CausationId).HasMaxLength(100);
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.RecordedAt).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.RetentionUntil);

        builder.HasIndex(x => x.RecordedAt).IsDescending();
        builder.HasIndex(x => new { x.SourceContext, x.EventName, x.RecordedAt }).IsDescending(false, false, true);
        builder.HasIndex(x => new { x.SubjectType, x.SubjectId, x.RecordedAt })
            .HasFilter("\"subject_type\" IS NOT NULL AND \"subject_id\" IS NOT NULL")
            .IsDescending(false, false, true);
        builder.HasIndex(x => new { x.SourceContext, x.AggregateType, x.AggregateId, x.RecordedAt })
            .HasFilter("\"aggregate_type\" IS NOT NULL AND \"aggregate_id\" IS NOT NULL")
            .IsDescending(false, false, false, true);
        builder.HasIndex(x => new { x.WorkspaceId, x.RecordedAt })
            .HasFilter("\"workspace_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => x.CorrelationId)
            .HasFilter("\"correlation_id\" IS NOT NULL");
        builder.HasIndex(x => x.PayloadJson).HasMethod("gin");
    }
}

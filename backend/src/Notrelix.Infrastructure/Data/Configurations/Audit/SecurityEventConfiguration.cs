using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.Infrastructure.Data.Configurations.Audit;

public sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("security_events", DbSchemas.Audit);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EventType).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Severity).IsRequired().HasMaxLength(40).HasDefaultValue("Info");
        builder.Property(x => x.Outcome).IsRequired().HasMaxLength(40).HasDefaultValue("Observed");
        builder.Property(x => x.DeviceId).HasMaxLength(160);
        builder.Property(x => x.ResourceType).HasMaxLength(160);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.RecordedAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.OccurredAt })
            .HasFilter("\"user_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.WorkspaceId, x.OccurredAt })
            .HasFilter("\"workspace_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.EventType, x.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.Severity, x.OccurredAt }).IsDescending(false, true);
    }
}

using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.Infrastructure.Data.Configurations.Audit;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", DbSchemas.Audit);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ActorType).IsRequired().HasMaxLength(80).HasDefaultValue("User");
        builder.Property(x => x.Action).IsRequired().HasMaxLength(160);
        builder.Property(x => x.ResourceType).HasMaxLength(160);
        builder.Property(x => x.SubjectType).HasMaxLength(160);
        builder.Property(x => x.Severity).IsRequired().HasMaxLength(40).HasDefaultValue("Info");
        builder.Property(x => x.Outcome).IsRequired().HasMaxLength(40).HasDefaultValue("Succeeded");
        builder.Property(x => x.RequestId).HasMaxLength(120);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.CausationId).HasMaxLength(100);
        builder.Property(x => x.BeforeJson).HasColumnType("jsonb");
        builder.Property(x => x.AfterJson).HasColumnType("jsonb");
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.RecordedAt).IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.OccurredAt })
            .HasFilter("\"workspace_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAt })
            .HasFilter("\"actor_user_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.ResourceType, x.ResourceId, x.OccurredAt })
            .HasFilter("\"resource_type\" IS NOT NULL AND \"resource_id\" IS NOT NULL")
            .IsDescending(false, false, true);
        builder.HasIndex(x => x.CorrelationId)
            .HasFilter("\"correlation_id\" IS NOT NULL");
        builder.HasIndex(x => new { x.Action, x.OccurredAt }).IsDescending(false, true);
    }
}

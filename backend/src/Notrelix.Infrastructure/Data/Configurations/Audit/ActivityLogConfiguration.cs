using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.Infrastructure.Data.Configurations.Audit;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs", DbSchemas.Audit);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.ActorDisplayName).HasColumnName("actor_display_name").HasMaxLength(240);
        builder.Property(x => x.ActivityType).HasColumnName("activity_type").IsRequired().HasMaxLength(160);
        builder.Property(x => x.Verb).HasColumnName("verb").HasMaxLength(120);
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(160);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.ResourceName).HasColumnName("resource_name").HasMaxLength(300);
        builder.Property(x => x.TargetResourceType).HasColumnName("target_resource_type").HasMaxLength(160);
        builder.Property(x => x.TargetResourceId).HasColumnName("target_resource_id");
        builder.Property(x => x.TargetResourceName).HasColumnName("target_resource_name").HasMaxLength(300);
        builder.Property(x => x.Summary).HasColumnName("summary");
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at").IsRequired();
        builder.Property(x => x.IsVisible).HasColumnName("is_visible").IsRequired().HasDefaultValue(true);
        builder.Property(x => x.HiddenAt).HasColumnName("hidden_at");
        builder.Property(x => x.HiddenBy).HasColumnName("hidden_by");
        builder.Property(x => x.HideReason).HasColumnName("hide_reason");

        builder.HasIndex(x => new { x.WorkspaceId, x.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAt })
            .HasFilter("\"actor_user_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.ResourceType, x.ResourceId, x.OccurredAt })
            .HasFilter("\"resource_type\" IS NOT NULL AND \"resource_id\" IS NOT NULL")
            .IsDescending(false, false, true);
        builder.HasIndex(x => new { x.WorkspaceId, x.IsVisible, x.OccurredAt })
            .HasFilter("\"is_visible\" = true")
            .IsDescending(false, false, true);
    }
}

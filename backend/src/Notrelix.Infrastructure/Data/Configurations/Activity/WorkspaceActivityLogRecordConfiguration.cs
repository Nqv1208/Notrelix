using Notrelix.Infrastructure.Data.Projections.Activity;

namespace Notrelix.Infrastructure.Data.Configurations.Activity;

public sealed class WorkspaceActivityLogRecordConfiguration : IEntityTypeConfiguration<WorkspaceActivityLogRecord>
{
    public void Configure(EntityTypeBuilder<WorkspaceActivityLogRecord> builder)
    {
        builder.ToTable("workspace_activity_logs", DbSchemas.Activity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SourceContext).HasColumnName("source_context").IsRequired().HasMaxLength(80);
        builder.Property(x => x.SourceEventId).HasColumnName("source_event_id");
        builder.Property(x => x.SourceMessageId).HasColumnName("source_message_id");

        builder.Property(x => x.ActivityType).HasColumnName("activity_type").IsRequired().HasMaxLength(160);

        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.ActorDisplayName).HasColumnName("actor_display_name").HasMaxLength(200);
        builder.Property(x => x.ActorAvatarUrl).HasColumnName("actor_avatar_url");

        builder.Property(x => x.SubjectType).HasColumnName("subject_type").IsRequired().HasMaxLength(160);
        builder.Property(x => x.SubjectId).HasColumnName("subject_id").IsRequired();
        builder.Property(x => x.SubjectDisplayName).HasColumnName("subject_display_name").HasMaxLength(300);

        builder.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(160);
        builder.Property(x => x.TargetId).HasColumnName("target_id");
        builder.Property(x => x.TargetDisplayName).HasColumnName("target_display_name").HasMaxLength(300);

        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(160);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.ResourceDisplayName).HasColumnName("resource_display_name").HasMaxLength(300);

        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(300);
        builder.Property(x => x.Body).HasColumnName("body");
        builder.Property(x => x.DataJson).HasColumnName("data_json").HasColumnType("jsonb").HasConversion<string>().IsRequired().HasDefaultValueSql("'{}'::jsonb");

        builder.Property(x => x.Visibility).HasColumnName("visibility").IsRequired().HasMaxLength(40).HasDefaultValue("Workspace");
        builder.Property(x => x.Importance).HasColumnName("importance").IsRequired().HasMaxLength(40).HasDefaultValue("Normal");

        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(x => new { x.WorkspaceId, x.OccurredAt }).HasDatabaseName("ix_activity_workspace_logs_workspace_time")
            .IsDescending(false, true).HasFilter("\"deleted_at\" IS NULL");
        builder.HasIndex(x => new { x.SubjectType, x.SubjectId, x.OccurredAt }).HasDatabaseName("ix_activity_workspace_logs_subject")
            .IsDescending(false, false, true).HasFilter("\"deleted_at\" IS NULL");
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAt }).HasDatabaseName("ix_activity_workspace_logs_actor")
            .IsDescending(false, true).HasFilter("\"actor_user_id\" IS NOT NULL AND \"deleted_at\" IS NULL");
        builder.HasIndex(x => new { x.ActivityType, x.OccurredAt }).HasDatabaseName("ix_activity_workspace_logs_type")
            .IsDescending(false, true).HasFilter("\"deleted_at\" IS NULL");
        builder.HasIndex(x => new { x.ResourceType, x.ResourceId, x.OccurredAt }).HasDatabaseName("ix_activity_workspace_logs_resource")
            .IsDescending(false, false, true).HasFilter("\"resource_type\" IS NOT NULL AND \"resource_id\" IS NOT NULL AND \"deleted_at\" IS NULL");
        builder.HasIndex(x => x.SourceEventId).HasDatabaseName("ix_activity_workspace_logs_source_event")
            .HasFilter("\"source_event_id\" IS NOT NULL");
        builder.HasIndex(x => x.SourceMessageId).HasDatabaseName("ix_activity_workspace_logs_source_message")
            .HasFilter("\"source_message_id\" IS NOT NULL");
        builder.HasIndex(x => x.DataJson).HasDatabaseName("ix_activity_workspace_logs_data_gin").HasMethod("gin");
    }
}

using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

public sealed class NotificationItemConfiguration : IEntityTypeConfiguration<NotificationItemRecord>
{
    public void Configure(EntityTypeBuilder<NotificationItemRecord> builder)
    {
        builder.ToTable("notification_items", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.DeduplicationKey).HasColumnName("deduplication_key").HasMaxLength(320);
        builder.HasIndex(x => x.DeduplicationKey).IsUnique().HasDatabaseName("ux_notifications_items_dedup");

        builder.Property(x => x.SourceContext).HasColumnName("source_context").IsRequired().HasMaxLength(80);
        builder.Property(x => x.SourceEventId).HasColumnName("source_event_id");
        builder.Property(x => x.SourceMessageId).HasColumnName("source_message_id");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");

        builder.Property(x => x.NotificationType).HasColumnName("notification_type").IsRequired().HasMaxLength(160);
        builder.Property(x => x.Severity).HasColumnName("severity").HasConversion<string>().IsRequired().HasMaxLength(40).HasDefaultValue(NotificationSeverity.Info);

        builder.Property(x => x.SubjectType).HasColumnName("subject_type").HasMaxLength(160);
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
        builder.Property(x => x.ResourceKind).HasColumnName("resource_type").HasMaxLength(160);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");

        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(320);
        builder.Property(x => x.Body).HasColumnName("body");
        builder.Property(x => x.ActionUrl).HasColumnName("action_url");
        builder.Property(x => x.DataJson).HasColumnName("data_json").HasColumnType("jsonb").HasConversion<string>().IsRequired();

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(40).HasDefaultValue(NotificationItemStatus.Active);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");

        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().HasDefaultValue(1L);

        builder.HasIndex(x => new { x.WorkspaceId, x.CreatedAt }).HasDatabaseName("ix_notifications_items_workspace_time")
            .IsDescending(false, true).HasFilter("\"workspace_id\" IS NOT NULL");
        builder.HasIndex(x => x.SourceMessageId).HasDatabaseName("ix_notifications_items_source_message")
            .HasFilter("\"source_message_id\" IS NOT NULL");
        builder.HasIndex(x => x.SourceEventId).HasDatabaseName("ix_notifications_items_source_event")
            .HasFilter("\"source_event_id\" IS NOT NULL");
        builder.HasIndex(x => new { x.SubjectType, x.SubjectId, x.CreatedAt }).HasDatabaseName("ix_notifications_items_subject")
            .IsDescending(false, false, true).HasFilter("\"subject_type\" IS NOT NULL AND \"subject_id\" IS NOT NULL");
        builder.HasIndex(x => new { x.ResourceKind, x.ResourceId, x.CreatedAt }).HasDatabaseName("ix_notifications_items_resource")
            .IsDescending(false, false, true).HasFilter("\"resource_type\" IS NOT NULL AND \"resource_id\" IS NOT NULL");
    }
}

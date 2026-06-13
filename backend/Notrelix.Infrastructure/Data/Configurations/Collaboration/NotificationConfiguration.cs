using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Content).HasColumnName("content").IsRequired().HasMaxLength(2048);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.ResourceType).HasColumnName("target_type").HasMaxLength(50);
            target.Property(t => t.ResourceId).HasColumnName("target_id");
        });

        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_notifications_user_id");
        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_notifications_workspace_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_notifications_status");
    }
}

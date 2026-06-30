using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Notifications.NotificationItems;
using Notrelix.Domain.Notifications.NotificationRecipients;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

public sealed class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("notification_recipients", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.NotificationId).HasColumnName("notification_id").IsRequired();
        builder.HasOne<NotificationItem>()
            .WithMany()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        builder.Property(x => x.RecipientEmail).HasColumnName("recipient_email");
        builder.Property(x => x.RecipientName).HasColumnName("recipient_name").HasMaxLength(240);

        builder.Property(x => x.DeliveryPolicyJson).HasColumnName("delivery_policy_json").HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(40).HasDefaultValue(RecipientStatus.Unread);
        builder.Property(x => x.SeenAt).HasColumnName("seen_at");
        builder.Property(x => x.ReadAt).HasColumnName("read_at");
        builder.Property(x => x.ArchivedAt).HasColumnName("archived_at");
        builder.Property(x => x.DismissedAt).HasColumnName("dismissed_at");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.NotificationId, x.RecipientUserId }).IsUnique().HasDatabaseName("ux_notifications_recipients_notification_user");
        builder.HasIndex(x => new { x.RecipientUserId, x.Status, x.CreatedAt }).HasDatabaseName("ix_notifications_recipients_user_status_time").IsDescending(false, false, true);
        builder.HasIndex(x => new { x.WorkspaceId, x.RecipientUserId, x.Status, x.CreatedAt }).HasDatabaseName("ix_notifications_recipients_workspace_user_status")
            .IsDescending(false, false, false, true).HasFilter("\"workspace_id\" IS NOT NULL");
        builder.HasIndex(x => x.NotificationId).HasDatabaseName("ix_notifications_recipients_notification");
    }
}

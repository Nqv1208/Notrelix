using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreferenceRecord>
{
    public void Configure(EntityTypeBuilder<NotificationPreferenceRecord> builder)
    {
        builder.ToTable("notification_preferences", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(x => x.NotificationType).HasColumnName("notification_type").IsRequired().HasMaxLength(160);
        builder.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().IsRequired().HasMaxLength(40);

        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired().HasDefaultValue(true);
        builder.Property(x => x.DeliveryMode).HasColumnName("delivery_mode").HasConversion<string>().IsRequired().HasMaxLength(40).HasDefaultValue(DeliveryMode.Immediate);
        builder.Property(x => x.DigestIntervalMinutes).HasColumnName("digest_interval_minutes");
        builder.Property(x => x.QuietHoursJson).HasColumnName("quiet_hours_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Timezone).HasColumnName("timezone").HasMaxLength(80);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.UserId, x.NotificationType, x.Channel }).IsUnique().HasDatabaseName("ux_notifications_preferences_global")
            .HasFilter("\"workspace_id\" IS NULL");
        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.NotificationType, x.Channel }).IsUnique().HasDatabaseName("ux_notifications_preferences_workspace")
            .HasFilter("\"workspace_id\" IS NOT NULL");
        builder.HasIndex(x => new { x.UserId, x.WorkspaceId }).HasDatabaseName("ix_notifications_preferences_user");
    }
}

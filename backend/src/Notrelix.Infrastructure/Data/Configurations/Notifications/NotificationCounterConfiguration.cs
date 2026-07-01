using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Notifications.NotificationCounters;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

public sealed class NotificationCounterConfiguration : IEntityTypeConfiguration<NotificationCounter>
{
    public void Configure(EntityTypeBuilder<NotificationCounter> builder)
    {
        builder.ToTable("notification_counters", DbSchemas.Notifications);

        builder.HasKey(x => new { x.WorkspaceId, x.UserId, x.CounterType });
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.CounterType).HasColumnName("counter_type").HasMaxLength(80).HasDefaultValue("Notification");
        builder.Property(x => x.CounterValue).HasColumnName("counter_value").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").IsRequired().HasDefaultValue(1);

        builder.HasIndex(x => new { x.UserId, x.WorkspaceId }).HasDatabaseName("ix_notification_counters_user_workspace");
    }
}

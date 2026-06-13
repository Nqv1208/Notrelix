using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.NotificationId).HasColumnName("notification_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        builder.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(256);
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(1024);
        builder.Property(x => x.SentAt).HasColumnName("sent_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => x.NotificationId).HasDatabaseName("idx_notification_deliveries_notification_id");
        builder.HasIndex(x => x.RecipientUserId).HasDatabaseName("idx_notification_deliveries_recipient");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_notification_deliveries_status");
    }
}

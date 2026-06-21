using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Enabled).HasColumnName("enabled");

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_notification_preferences_user_id");
        builder.HasIndex(x => new { x.UserId, x.WorkspaceId }).HasDatabaseName("idx_notification_preferences_user_workspace");
    }
}

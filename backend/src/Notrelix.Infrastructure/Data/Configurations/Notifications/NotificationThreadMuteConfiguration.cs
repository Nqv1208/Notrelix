using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

public sealed class NotificationThreadMuteConfiguration : IEntityTypeConfiguration<NotificationThreadMuteRecord>
{
    public void Configure(EntityTypeBuilder<NotificationThreadMuteRecord> builder)
    {
        builder.ToTable("notification_thread_mutes", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ResourceKind).HasColumnName("resource_type").IsRequired().HasMaxLength(160);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ResourceKind, x.ResourceId }).IsUnique().HasDatabaseName("ux_notification_thread_mutes_member_thread");
        builder.HasIndex(x => new { x.ResourceKind, x.ResourceId }).HasDatabaseName("ix_notification_thread_mutes_thread");
    }
}

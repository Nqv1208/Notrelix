using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.ActorId).HasColumnName("actor_id");
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(100);
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false);
        builder.Property(x => x.ReadAt).HasColumnName("read_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("idx_notifications_user_unread")
            .HasFilter("is_read = false");

        builder.Ignore(x => x.DomainEvents);
    }
}

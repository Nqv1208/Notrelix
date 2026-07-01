using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardSubscriberConfiguration : IEntityTypeConfiguration<BoardSubscriber>
{
    public void Configure(EntityTypeBuilder<BoardSubscriber> builder)
    {
        builder.ToTable("board_subscribers", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.SubscriberRole).HasColumnName("subscriber_role").IsRequired();
        builder.Property(x => x.NotificationJson).HasColumnName("notification_json").IsRequired();
        builder.Property(x => x.SubscribedAt).HasColumnName("subscribed_at").IsRequired();
        builder.Property(x => x.SubscribedBy).HasColumnName("subscribed_by");
        builder.Property(x => x.Version).HasColumnName("version");

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BoardId, x.UserId }).IsUnique().HasDatabaseName("idx_board_subscribers_board_user");
    }
}

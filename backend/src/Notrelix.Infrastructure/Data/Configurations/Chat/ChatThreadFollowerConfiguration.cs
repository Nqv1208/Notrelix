using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>A person's follow state on a thread (UC-CHT-F04). Keyed by (root message, user).</summary>
public class ChatThreadFollowerConfiguration : IEntityTypeConfiguration<ThreadFollower>
{
    public void Configure(EntityTypeBuilder<ThreadFollower> builder)
    {
        builder.ToTable("thread_followers", DbSchemas.Chat);

        builder.HasKey(x => new { x.RootMessageId, x.UserId });
        builder.Property(x => x.RootMessageId).HasColumnName("root_message_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Muted).HasColumnName("muted").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ReadReplySequence).HasColumnName("read_reply_sequence").IsRequired().HasDefaultValue(0L);

        builder.HasIndex(x => new { x.UserId, x.WorkspaceId })
            .HasDatabaseName("ix_thread_followers_user_workspace");

        builder.HasOne<MessageThread>()
            .WithMany()
            .HasForeignKey(x => x.RootMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

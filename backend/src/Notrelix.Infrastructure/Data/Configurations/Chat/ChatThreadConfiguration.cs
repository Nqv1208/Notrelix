using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>The thread of a root message (CHT-013). Keyed by the root message, so a root has one thread.</summary>
public class ChatThreadConfiguration : IEntityTypeConfiguration<MessageThread>
{
    public void Configure(EntityTypeBuilder<MessageThread> builder)
    {
        builder.ToTable("threads", DbSchemas.Chat);

        builder.HasKey(x => x.RootMessageId);
        builder.Property(x => x.RootMessageId).HasColumnName("root_message_id").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.ReplyCount).HasColumnName("reply_count").IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.LastReplyAt).HasColumnName("last_reply_at").IsRequired();
        builder.Property(x => x.ArchivedAt).HasColumnName("archived_at");

        builder.HasIndex(x => new { x.WorkspaceId, x.LastReplyAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_threads_workspace_last_reply");

        builder.HasOne<Notrelix.Domain.Chat.Spaces.Space>()
            .WithMany()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(x => x.RootMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

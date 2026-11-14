using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

public class ChatMessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.Sequence).HasColumnName("sequence").IsRequired();
        builder.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion(v => v.ToWire(), s => MessageKindWire.MessageKindFromWire(s))
            .IsRequired()
            .HasMaxLength(16);
        builder.Property(x => x.Content).HasColumnName("content").HasMaxLength(ChatLimits.MessageMaxLength);
        builder.Property(x => x.ClientMessageId).HasColumnName("client_message_id").HasMaxLength(ChatLimits.ClientMessageIdMaxLength);
        builder.Property(x => x.ReplyToId).HasColumnName("reply_to_id");
        builder.Property(x => x.ThreadRootId).HasColumnName("thread_root_id");
        builder.Property(x => x.ThreadSequence).HasColumnName("thread_sequence");
        builder.Property(x => x.AlsoSentToChannel).HasColumnName("also_sent_to_channel").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.EditedAt).HasColumnName("edited_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");

        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.IsEdited);
        builder.Ignore(x => x.IsSystem);
        builder.Ignore(x => x.IsRemovedByModerator);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // One sequence per space, gap-free and never reused (CHT-005). Stored descending, so the newest-first history
        // read (spec section 19, index messages (space_id, sequence desc)) uses the same index as the unique rule.
        builder.HasIndex(x => new { x.SpaceId, x.Sequence })
            .IsUnique()
            .IsDescending(false, true)
            .HasDatabaseName("ux_messages_space_sequence");

        // A client retry with the same key finds the stored message instead of storing a second one (CHT-006).
        builder.HasIndex(x => new { x.SpaceId, x.AuthorId, x.ClientMessageId })
            .IsUnique()
            .HasDatabaseName("ux_messages_space_author_client_message");

        // One sequence per thread, gap-free and never reused (UC-CHT-F01). Null outside a thread, so the rule is per root.
        builder.HasIndex(x => new { x.ThreadRootId, x.ThreadSequence })
            .IsUnique()
            .HasDatabaseName("ux_messages_thread_sequence");

        builder.HasOne<Notrelix.Domain.Chat.Spaces.Space>()
            .WithMany()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

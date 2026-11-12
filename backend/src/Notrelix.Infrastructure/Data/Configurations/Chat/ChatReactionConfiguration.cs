using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>
/// One person's reaction of one kind to a message (CHT-011, UC-CHT-G06, G07). The unique index on (message_id, user_id, kind)
/// settles a race between two requests for the same reaction; the handler also checks under the space lock. The reactors of a
/// message are listed in (message_id, created_at, user_id) order, and the summary reads by (message_id, kind).
/// </summary>
public class ChatReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("reactions", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(16).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.MessageId, x.UserId, x.Kind })
            .IsUnique()
            .HasDatabaseName("ux_reactions_message_user_kind");

        builder.HasIndex(x => new { x.MessageId, x.CreatedAt, x.UserId })
            .HasDatabaseName("ix_reactions_message_created_at");

        builder.HasOne<Notrelix.Domain.Chat.Spaces.Space>()
            .WithMany()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

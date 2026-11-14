using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>
/// The mentions of a message (CHT-010, UC-CHT-G01, G03, G04): one row per span of the text. A message's rows are read by
/// message_id; the mention counters read the rows of a person by (target_id) and the broadcasts of a space by (space_id, kind).
/// The kind is stored as its wire text.
/// </summary>
public class ChatMessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
{
    public void Configure(EntityTypeBuilder<MessageMention> builder)
    {
        builder.ToTable("message_mentions", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion(k => k.ToWire(), v => MentionKindWire.MentionKindFromWire(v)!.Value)
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(x => x.TargetId).HasColumnName("target_id");
        builder.Property(x => x.Start).HasColumnName("start").IsRequired();
        builder.Property(x => x.Length).HasColumnName("length").IsRequired();

        builder.HasIndex(x => x.MessageId).HasDatabaseName("ix_message_mentions_message");
        builder.HasIndex(x => new { x.TargetId, x.Kind }).HasDatabaseName("ix_message_mentions_target");
        builder.HasIndex(x => new { x.SpaceId, x.Kind }).HasDatabaseName("ix_message_mentions_space_kind");

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

using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>
/// A message pinned in its space (UC-CHT-F06, F07). A message has at most one pin (unique on message_id); the list of a space
/// reads its pins newest first by (space_id, pinned_at, id).
/// </summary>
public class ChatPinConfiguration : IEntityTypeConfiguration<Pin>
{
    public void Configure(EntityTypeBuilder<Pin> builder)
    {
        builder.ToTable("pins", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
        builder.Property(x => x.PinnedBy).HasColumnName("pinned_by").IsRequired();
        builder.Property(x => x.PinnedAt).HasColumnName("pinned_at").IsRequired();

        builder.HasIndex(x => x.MessageId)
            .IsUnique()
            .HasDatabaseName("ux_pins_message");

        builder.HasIndex(x => new { x.SpaceId, x.PinnedAt, x.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("ix_pins_space_pinned_at");

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

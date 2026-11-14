using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

public class ChatSpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.ToTable("spaces", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion(v => v.ToWire(), s => SpaceWire.SpaceKindFromWire(s))
            .IsRequired()
            .HasMaxLength(32);
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(ChatLimitsMax.Name);
        builder.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(ChatLimitsMax.Topic);
        builder.Property(x => x.GroupId).HasColumnName("group_id");
        builder.Property(x => x.Position).HasColumnName("position").IsRequired();
        builder.Property(x => x.ArchivedAt).HasColumnName("archived_at");
        builder.Property(x => x.LastSequence).HasColumnName("last_sequence").IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.SlowModeSeconds).HasColumnName("slow_mode_seconds").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LinkKind).HasColumnName("link_kind").HasMaxLength(40);
        builder.Property(x => x.LinkId).HasColumnName("link_id");

        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.IsArchived);
        builder.Ignore(x => x.HasLink);
        builder.Ignore(x => x.IsPublic);
        builder.Ignore(x => x.IsPrivate);
        builder.Ignore(x => x.IsConversation);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasMany(x => x.Memberships)
            .WithOne()
            .HasForeignKey(m => m.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Memberships).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.PermissionOverrides)
            .WithOne()
            .HasForeignKey(o => o.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.PermissionOverrides).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<ChannelGroup>()
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.WorkspaceId, x.Kind }).HasDatabaseName("idx_spaces_workspace_kind");
        builder.HasIndex(x => new { x.WorkspaceId, x.LinkKind, x.LinkId })
            .HasDatabaseName("idx_spaces_workspace_link")
            .HasFilter("link_id IS NOT NULL AND deleted_at IS NULL");

        // A name is unique among the channels that are live: not archived and not deleted. Conversations carry no channel name
        // (a direct one has none, a group one may share a channel's name), so only the channel kinds take part.
        builder.HasIndex(x => new { x.WorkspaceId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_spaces_workspace_name_live")
            .HasFilter("archived_at IS NULL AND deleted_at IS NULL AND kind IN ('channel_public', 'channel_private')");
    }
}

/// <summary>Column widths of the Chat tables, taken from the domain limits.</summary>
internal static class ChatLimitsMax
{
    public const int Name = Notrelix.Domain.Chat.Rules.ChatLimits.ChannelNameMaxLength;
    public const int Topic = Notrelix.Domain.Chat.Rules.ChatLimits.ChannelTopicMaxLength;
}

using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

public class ChatMembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships", DbSchemas.Chat);

        builder.HasKey(x => new { x.SpaceId, x.UserId });
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion(v => v.ToWire(), s => SpaceWire.MembershipRoleFromWire(s))
            .IsRequired()
            .HasMaxLength(16);
        builder.Property(x => x.NotificationLevel)
            .HasColumnName("notification_level")
            .HasConversion(v => v.ToWire(), s => SpaceWire.NotificationLevelFromWire(s))
            .IsRequired()
            .HasMaxLength(16);
        builder.Property(x => x.JoinedAt).HasColumnName("joined_at").IsRequired();
        builder.Property(x => x.LeftAt).HasColumnName("left_at");
        builder.Property(x => x.ReadSequence).HasColumnName("read_sequence").IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.MentionCount).HasColumnName("mention_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MutedUntil).HasColumnName("muted_until");
        builder.Property(x => x.HiddenAt).HasColumnName("hidden_at");

        builder.Ignore(x => x.IsActive);
        builder.Ignore(x => x.IsManager);
        builder.Ignore(x => x.IsHidden);

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_memberships_user");

        // The unread summary reads one person's active memberships in a Workspace, with their read boundary (UC-CHT-D01).
        builder.HasIndex(x => new { x.UserId, x.WorkspaceId })
            .HasDatabaseName("idx_memberships_user_workspace_active")
            .IncludeProperties(x => new { x.SpaceId, x.ReadSequence })
            .HasFilter("left_at IS NULL");
    }
}

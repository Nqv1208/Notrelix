using Notrelix.Domain.Chat.Permissions;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>
/// The overrides of a channel's default permission set (UC-CHT-H01): one row per (space, subject, action), allow or deny. The rows of
/// a space are read together by space_id (the primary key leads with it). Subject, action and effect are stored as their wire text.
/// </summary>
public class ChatPermissionOverrideConfiguration : IEntityTypeConfiguration<ChannelPermissionOverride>
{
    public void Configure(EntityTypeBuilder<ChannelPermissionOverride> builder)
    {
        builder.ToTable("permission_overrides", DbSchemas.Chat);

        builder.HasKey(x => new { x.SpaceId, x.Subject, x.Action });
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.Subject)
            .HasColumnName("subject")
            .IsRequired();
        builder.Property(x => x.Action)
            .HasColumnName("action")
            .IsRequired();
        builder.Property(x => x.Effect)
            .HasColumnName("effect")
            .IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
    }
}

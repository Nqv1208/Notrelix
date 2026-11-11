using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

public class ChatChannelGroupConfiguration : IEntityTypeConfiguration<ChannelGroup>
{
    public void Configure(EntityTypeBuilder<ChannelGroup> builder)
    {
        builder.ToTable("channel_groups", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(Notrelix.Domain.Chat.Rules.ChatLimits.ChannelGroupNameMaxLength);
        builder.Property(x => x.Position).HasColumnName("position").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => new { x.WorkspaceId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_channel_groups_workspace_name");
    }
}

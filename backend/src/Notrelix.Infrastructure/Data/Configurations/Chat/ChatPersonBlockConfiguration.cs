using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>One block per direction in a Workspace: (workspace, blocker, blocked) is the key (UC-CHT-E06).</summary>
public class ChatPersonBlockConfiguration : IEntityTypeConfiguration<PersonBlock>
{
    public void Configure(EntityTypeBuilder<PersonBlock> builder)
    {
        builder.ToTable("blocks", DbSchemas.Chat);

        builder.HasKey(x => new { x.WorkspaceId, x.BlockerId, x.BlockedId });
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BlockerId).HasColumnName("blocker_id").IsRequired();
        builder.Property(x => x.BlockedId).HasColumnName("blocked_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.BlockedId }).HasDatabaseName("ix_blocks_workspace_blocked");
    }
}

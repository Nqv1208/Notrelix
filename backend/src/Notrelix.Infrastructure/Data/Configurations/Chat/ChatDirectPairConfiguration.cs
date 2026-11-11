using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>
/// The unordered pair of a direct conversation (CHT-014). The key (workspace, low, high) makes a second conversation of the same pair
/// impossible; the start command takes a pair lock first, so the database key is only the last line of defence.
/// </summary>
public class ChatDirectPairConfiguration : IEntityTypeConfiguration<DirectPair>
{
    public void Configure(EntityTypeBuilder<DirectPair> builder)
    {
        builder.ToTable("direct_pairs", DbSchemas.Chat);

        builder.HasKey(x => new { x.WorkspaceId, x.UserLow, x.UserHigh });
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserLow).HasColumnName("user_low").IsRequired();
        builder.Property(x => x.UserHigh).HasColumnName("user_high").IsRequired();
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();

        builder.HasIndex(x => x.SpaceId).HasDatabaseName("ix_direct_pairs_space_id");

        builder.HasOne<Notrelix.Domain.Chat.Spaces.Space>()
            .WithMany()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

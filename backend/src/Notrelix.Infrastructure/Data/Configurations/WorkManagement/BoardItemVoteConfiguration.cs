using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemVoteConfiguration : IEntityTypeConfiguration<BoardItemVote>
{
    public void Configure(EntityTypeBuilder<BoardItemVote> builder)
    {
        builder.ToTable("board_item_votes", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.VotedAt).HasColumnName("voted_at").IsRequired();

        builder.HasOne<BoardItem>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<BoardField>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Cascade);

        // One vote per person, Item and field: the row is the vote.
        builder.HasIndex(x => new { x.ItemId, x.FieldId, x.UserId }).IsUnique().HasDatabaseName("idx_board_item_votes_item_field_user");
        builder.HasIndex(x => x.FieldId).HasDatabaseName("idx_board_item_votes_field_id");
    }
}

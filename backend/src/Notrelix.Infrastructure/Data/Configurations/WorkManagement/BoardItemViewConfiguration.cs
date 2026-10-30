using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemViewConfiguration : IEntityTypeConfiguration<BoardItemView>
{
    public void Configure(EntityTypeBuilder<BoardItemView> builder)
    {
        builder.ToTable("board_item_views", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.LastViewedAt).HasColumnName("last_viewed_at").IsRequired();

        builder.HasOne<BoardItem>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ItemId, x.UserId }).IsUnique().HasDatabaseName("idx_board_item_views_item_user");
    }
}

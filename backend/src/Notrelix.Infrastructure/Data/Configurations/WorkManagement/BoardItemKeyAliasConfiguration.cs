using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemKeyAliasConfiguration : IEntityTypeConfiguration<BoardItemKeyAlias>
{
    public void Configure(EntityTypeBuilder<BoardItemKeyAlias> builder)
    {
        builder.ToTable("board_item_key_aliases", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.ItemKey).HasColumnName("item_key").HasMaxLength(64).IsRequired();

        builder.HasOne<BoardItem>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // A key is issued once per board, so it resolves to one Item.
        builder.HasIndex(x => new { x.BoardId, x.ItemKey }).IsUnique().HasDatabaseName("ux_work_board_item_key_aliases_board_key");
        builder.HasIndex(x => x.ItemId).HasDatabaseName("idx_board_item_key_aliases_item");
    }
}

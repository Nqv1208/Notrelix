using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemConfiguration : IEntityTypeConfiguration<BoardItem>
{
    public void Configure(EntityTypeBuilder<BoardItem> builder)
    {
        builder.ToTable("board_items", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(1024);
        builder.Property(x => x.DescriptionMd).HasColumnName("description_md").HasMaxLength(BoardItem.DescriptionMaxLength);

        builder.Property(x => x.Position).HasColumnName("position").HasColumnType("text").UseCollation(WorkOrderingConstraints.OrdinalCollation).IsRequired();

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasMany(x => x.FieldValues)
            .WithOne()
            .HasForeignKey(x => x.ItemId);

        builder.HasOne<BoardGroup>()
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.GroupId, x.Position }).IsUnique().HasFilter("deleted_at IS NULL AND parent_item_id IS NULL").HasDatabaseName(WorkOrderingConstraints.GroupItems);
        builder.HasIndex(x => new { x.ParentItemId, x.Position }).IsUnique().HasFilter("deleted_at IS NULL AND parent_item_id IS NOT NULL").HasDatabaseName(WorkOrderingConstraints.ChildItems);
        builder.HasIndex(x => x.BoardId).HasFilter("deleted_at IS NULL").HasDatabaseName("idx_board_items_board_id");
        // An Item key is unique within its Board across every Item, deleted ones
        // included, so a key is never reused.
        builder.HasIndex(x => new { x.BoardId, x.ItemKey }).IsUnique().HasDatabaseName("ux_work_board_items_board_item_key");
        // Child Items of a set of parents are read together (board progress, item detail).
        builder.HasIndex(x => x.ParentItemId).HasFilter("parent_item_id IS NOT NULL").HasDatabaseName("idx_board_items_parent_item_id");
    }
}

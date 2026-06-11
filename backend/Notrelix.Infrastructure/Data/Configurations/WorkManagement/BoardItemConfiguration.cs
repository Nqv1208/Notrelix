using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemConfiguration : IEntityTypeConfiguration<BoardItem>
{
    public void Configure(EntityTypeBuilder<BoardItem> builder)
    {
        builder.ToTable("board_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(1024);

        builder.OwnsOne(x => x.Position, pos =>
        {
            pos.Property(p => p.Value).HasColumnName("position").HasColumnType("float8").IsRequired();
        });

        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
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

        builder.HasIndex(x => new { x.GroupId, x.Position }).HasFilter("is_deleted = false").HasDatabaseName("idx_board_items_group_position");
        builder.HasIndex(x => x.BoardId).HasFilter("is_deleted = false").HasDatabaseName("idx_board_items_board_id");
    }
}

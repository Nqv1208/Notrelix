using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class ItemDependencyConfiguration : IEntityTypeConfiguration<ItemDependency>
{
    public void Configure(EntityTypeBuilder<ItemDependency> builder)
    {
        builder.ToTable("item_dependencies", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.PredecessorItemId).HasColumnName("predecessor_item_id").IsRequired();
        builder.Property(x => x.SuccessorItemId).HasColumnName("successor_item_id").IsRequired();
        builder.Property(x => x.DependencyType).HasColumnName("dependency_type").IsRequired();
        builder.Property(x => x.LagMinutes).HasColumnName("lag_minutes");
        builder.Property(x => x.Version).HasColumnName("version");

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne<BoardItem>()
            .WithMany()
            .HasForeignKey(x => x.PredecessorItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BoardItem>()
            .WithMany()
            .HasForeignKey(x => x.SuccessorItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PredecessorItemId, x.SuccessorItemId }).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("idx_item_dependencies_pair");
        builder.HasIndex(x => x.SuccessorItemId).HasFilter("deleted_at IS NULL").HasDatabaseName("idx_item_dependencies_successor");
    }
}

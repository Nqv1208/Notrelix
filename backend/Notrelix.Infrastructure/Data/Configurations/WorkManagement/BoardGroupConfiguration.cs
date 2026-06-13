using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.BoardGroups;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardGroupConfiguration : IEntityTypeConfiguration<BoardGroup>
{
    public void Configure(EntityTypeBuilder<BoardGroup> builder)
    {
        builder.ToTable("board_groups", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(256);
        builder.Property(x => x.IsCollapsed).HasColumnName("is_collapsed");

        builder.OwnsOne(x => x.Color, color =>
        {
            color.Property(c => c.Value).HasColumnName("color").IsRequired().HasMaxLength(50);
        });

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

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BoardId, x.Position }).HasFilter("is_deleted = false").HasDatabaseName("idx_board_groups_board_position");
    }
}

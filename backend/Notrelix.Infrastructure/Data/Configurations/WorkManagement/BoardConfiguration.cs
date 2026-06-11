using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("boards");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1024);
        builder.Property(x => x.Background).HasColumnName("background").HasColumnType("jsonb").IsRequired().HasDefaultValue("{\"type\":\"color\",\"value\":\"#0079BF\"}");
        builder.Property(x => x.Visibility).HasColumnName("visibility").HasConversion<string>().IsRequired().HasMaxLength(50).HasDefaultValue(BoardVisibility.Workspace);
        builder.Property(x => x.IsArchived).HasColumnName("is_archived");

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

        builder.HasIndex(x => x.WorkspaceId).HasFilter("is_deleted = false AND is_archived = false").HasDatabaseName("idx_boards_workspace_id");
        builder.HasIndex(x => x.Title).HasDatabaseName("idx_boards_title");
    }
}

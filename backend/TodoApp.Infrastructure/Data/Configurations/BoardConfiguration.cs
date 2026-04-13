using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data.Configurations;

public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("boards");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(500);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Background).HasColumnName("background").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.Visibility).HasColumnName("visibility").HasMaxLength(50).HasDefaultValue("workspace");
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.WorkspaceId, x.IsArchived }).HasDatabaseName("idx_boards_workspace");

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DomainEvents);
    }
}
